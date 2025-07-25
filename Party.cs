namespace PartyMod
{
    public class Party
    {
        public string Name { get; }
        public ulong OwnerId { get; private set; }
        public bool IsPrivate { get; }
        public HashSet<ulong> Members { get; }
        public HashSet<ulong> Blacklist { get; }

        public Party(string name, ulong ownerId, bool isPrivate)
        {
            Name = name;
            OwnerId = ownerId;
            IsPrivate = isPrivate;
            Members = new HashSet<ulong> { ownerId };
            Blacklist = new HashSet<ulong>();
        }

        public bool AddMember(ulong playerId)
        {
            if (Blacklist.Contains(playerId) || Members.Contains(playerId))
                return false;
            Members.Add(playerId);
            return true;
        }

        public void RemoveMember(ulong playerId)
        {
            Members.Remove(playerId);
        }
    }

    public static class PartyUtility
    {
        private static readonly Dictionary<string, HashSet<ulong>> Parties = [];
        private static readonly Dictionary<string, bool> PartyIsPrivate = [];

        private static readonly Dictionary<ulong, string> PlayerToParty = [];
        private static readonly Dictionary<string, HashSet<ulong>> PartyBlacklist = [];

        public static string pendingPartyCreateName = null;
        public static ulong pendingPartyCreateSelfId = 0;
        public static bool pendingPartyNameExists = false;
        public static float pendingPartyCreateTime = 0;
        public const float PendingTimeout = 0.5f;

        public static bool pendingPartyLeave = false;
        public static float pendingPartyLeaveTime = 0;
        public static string pendingPartyLeaveName = null;


        public static HashSet<string> collectedPartyNames = [];
        public static float pendingPartyListTime = 0;
        public static bool pendingPartyList = false;
        public static bool pendingPartyIsPrivate = false;

        public static string GetPersonaName(ulong steamId) => Regex.Replace(SteamFriends.GetFriendPersonaName(new CSteamID(steamId)), "<.*?>", string.Empty);

        public static bool CreateParty(ulong ownerId, string name, bool isPrivate = false)
        {
            if (string.IsNullOrWhiteSpace(name) || Parties.ContainsKey(name))
                return false;
            Parties[name] = new HashSet<ulong> { ownerId };
            PlayerToParty[ownerId] = name;
            PartyIsPrivate[name] = isPrivate;
            return true;
        }

        public static bool JoinParty(ulong playerId, string name)
        {
            if (!Parties.TryGetValue(name, out var party)) return false;

            if (PartyBlacklist.TryGetValue(name, out var bl) && bl.Contains(playerId)) return false;
            if (party.Contains(playerId)) return false;
            party.Add(playerId);
            PlayerToParty[playerId] = name;
            return true;
        }


        public static void LeaveParty(ulong playerId, bool notify = true)
        {
            if (PlayerToParty.TryGetValue(playerId, out var name))
            {
                if (Parties.TryGetValue(name, out var party) && party.Contains(playerId))
                {
                    party.Remove(playerId);
                    PlayerToParty.Remove(playerId);

                    if (party.Count == 0)
                    {
                        Parties.Remove(name);
                        PartyIsPrivate.Remove(name);
                        PartyBlacklist.Remove(name);
                    }
                    else if (notify)
                    {
                        foreach (var member in party.ToList())
                        {
                            if (member != playerId)
                                SendPartyPacket("party_member_left", name, member, playerId);
                        }
                    }
                }
            }
        }




        public static void DeleteParty(string partyName)
        {
            if (!Parties.ContainsKey(partyName)) return;

            foreach (var member in Parties[partyName].ToList())
            {
                PlayerToParty.Remove(member);
            }

            Parties.Remove(partyName);
            PartyBlacklist.Remove(partyName);
            PartyIsPrivate.Remove(partyName);
        }



        public static string GetPartyOf(ulong playerId)
            => PlayerToParty.TryGetValue(playerId, out var name) ? name : null;

        public static IEnumerable<ulong> GetPartyMembers(string name)
            => Parties.TryGetValue(name, out var members) ? members : Enumerable.Empty<ulong>();

        public static string ListPartyMembers(ulong playerId)
        {
            var partyName = GetPartyOf(playerId);
            if (partyName == null) return "You are not in a party.";
            var members = GetPartyMembers(partyName)
                .Select(id => $"{GetPersonaName(id)} ({id})");
            return $"Party '{partyName}' members: {string.Join(", ", members)}";
        }
        public static void SendPartyPacket(string type, string partyName, ulong to, ulong who = 0)
        {
            string content = $"[GMF] party {type} {partyName} {who}";
            var packets = CreateGMFPacket(content);
            SendGMFPacket(to, packets);
        }

        public static void BroadcastPartyPacket(string type, string partyName, ulong who = 0)
        {
            string content = $"[GMF] party {type} {partyName} {who}";
            var packets = CreateGMFPacket(content);
            foreach (var id in modUsers)
                SendGMFPacket(id.Key, packets);
        }

        public static void SendPartyDeletedPacket(ulong to, string partyName, ulong ownerId)
        {
            string content = $"[GMF] party party_deleted {partyName} {ownerId}";
            var packets = CreateGMFPacket(content);
            SendGMFPacket(to, packets);
        }

        public static void HandleGMFPartyPacket(string[] arg, ulong senderId)
        {
            string cmd = arg.Length > 0 ? arg[0] : null;
            string partyName = arg.Length > 1 ? arg[1] : null;
            ulong targetId = arg.Length > 2 && ulong.TryParse(arg[2], out var fid) ? fid : 0;

            switch (cmd)
            {
                case "party_check_exists":
                    if (GetPartyMembers(partyName).FirstOrDefault() == clientId)
                        SendPartyPacket("party_exists", partyName, senderId);
                    break;

                case "party_exists":
                    if (pendingPartyCreateName != null && pendingPartyCreateName == partyName)
                        pendingPartyNameExists = true;
                    break;

                case "party_join_request":
                    {
                        var ownerId = GetPartyMembers(partyName).FirstOrDefault();
                        if (clientId == ownerId)
                        {
                            var joinerPersona = GetPersonaName(targetId);

                            if (PartyBlacklist.TryGetValue(partyName, out var bl) && bl.Contains(targetId))
                            {
                                SendPartyPacket("party_kick_blocked", partyName, targetId, clientId);
                                return;
                            }

                            JoinParty(targetId, partyName);

                            SendPartyPacket("party_join_confirm", partyName, targetId, ownerId);

                            ForceMessage($"{joinerPersona} joined your party '{partyName}'.");

                            foreach (var member in GetPartyMembers(partyName))
                            {
                                if (member != ownerId && member != targetId)
                                {
                                    SendPartyPacket("party_member_added", partyName, member, targetId);
                                    ForceMessage($"{joinerPersona} joined your party '{partyName}'.");
                                }
                            }
                        }
                        break;
                    }
                case "party_member_added":
                    {
                        if (GetPartyOf(clientId) == partyName)
                        {
                            JoinParty(targetId, partyName);
                            ForceMessage($"{GetPersonaName(targetId)} joined your party '{partyName}'.");
                        }
                        break;
                    }
                case "party_member_left":
                    {
                        if (GetPartyOf(targetId) == partyName)
                        {
                            LeaveParty(targetId, false);
                            ForceMessage($"{GetPersonaName(targetId)} left your party '{partyName}'.");
                        }
                        break;
                    }

                case "party_join_confirm":
                    {
                        ulong ownerId = targetId;

                        if (GetPartyOf(clientId) == partyName)
                            break;

                        if (!Parties.ContainsKey(partyName))
                        {
                            Parties[partyName] = new HashSet<ulong> { ownerId };
                            PartyIsPrivate[partyName] = false;
                            PlayerToParty[ownerId] = partyName;
                        }
                        else if (!GetPartyMembers(partyName).Contains(ownerId))
                        {
                            Parties[partyName].Add(ownerId);
                            PlayerToParty[ownerId] = partyName;
                        }

                        JoinParty(clientId, partyName);

                        ForceMessage($"You successfully joined party '{partyName}'.");
                        break;
                    }





                case "party_leave_request":
                    {
                        var ownerId = GetPartyMembers(partyName).FirstOrDefault();
                        if (clientId == ownerId)
                        {
                            if (!Parties.ContainsKey(partyName))
                            {
                                SendPartyPacket("party_leave_denied", partyName, targetId, clientId);
                            }
                            else if (GetPartyMembers(partyName).Contains(targetId))
                            {
                                LeaveParty(targetId);

                                ForceMessage($"{GetPersonaName(targetId)} left your party '{partyName}'.");

                                SendPartyPacket("party_leave_confirm", partyName, targetId, clientId);
                            }
                            else
                            {
                                SendPartyPacket("party_leave_denied", partyName, targetId, clientId);
                            }
                        }
                        break;
                    }




                case "party_leave_confirm":
                    {
                        if (GetPartyOf(clientId) == partyName)
                        {
                            LeaveParty(clientId);
                            ForceMessage("You left the party successfully.");
                        }
                        pendingPartyLeave = false;
                        pendingPartyLeaveName = null;
                        break;
                    }

                case "party_leave_denied":
                    {
                        ForceMessage($"Leave request denied for party '{partyName}'. You were not in that party.");
                        pendingPartyLeave = false;
                        pendingPartyLeaveName = null;
                        break;
                    }


                case "party_deleted":
                    if (!Parties.ContainsKey(partyName))
                        break;

                    if (clientId != senderId)
                    {
                        var ownerName = GetPersonaName(senderId);
                        ForceMessage($"Party '{partyName}' was deleted by {ownerName}.");
                    }

                    LeaveParty(clientId);
                    Parties.Remove(partyName);
                    break;



                case "party_kick":
                    if (clientId == targetId)
                    {
                        LeaveParty(clientId);
                        ForceMessage("You have been kicked from the party.");
                    }
                    break;

                case "party_sync_lobby":
                    {
                        ulong lobbyId = targetId;
                        if (SteamManager.Instance.currentLobby.m_SteamID == 0 || SteamManager.Instance.currentLobby.m_SteamID != lobbyId)
                        {
                            ForceMessage("Joining your party owner's lobby...");
                            SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
                        }
                        break;
                    }

                case "party_ping":
                    foreach (var p in Parties.Keys)
                        if (GetPartyMembers(p).FirstOrDefault() == clientId && (!PartyIsPrivate.TryGetValue(p, out var priv) || !priv))
                            SendPartyPacket("party_pong", p, senderId);
                    break;

                case "party_pong":
                    if (pendingPartyList && !string.IsNullOrEmpty(partyName))
                        collectedPartyNames.Add(partyName);
                    break;
            }
        }

        public static void HandlePartyCommand(string input, ulong selfId)
        {
            var arg = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (arg.Length < 2)
            {
                ForceMessage("Usage: /party <create|join|leave|delete|list> [name] [private]");
                return;
            }

            string command = arg[1].ToLowerInvariant();
            string partyName = arg.Length > 2 ? arg[2] : null;
            bool isPrivate = arg.Length > 3 && arg[3].Equals("private", StringComparison.OrdinalIgnoreCase);
            string myParty = GetPartyOf(selfId);

            switch (command)
            {
                case "create":
                    if (string.IsNullOrWhiteSpace(partyName))
                    {
                        ForceMessage("Usage: /party create <name> [private]");
                        break;
                    }
                    pendingPartyCreateName = partyName;
                    pendingPartyCreateSelfId = selfId;
                    pendingPartyCreateTime = Time.realtimeSinceStartup;
                    pendingPartyNameExists = false;
                    pendingPartyIsPrivate = isPrivate;
                    BroadcastPartyPacket("party_check_exists", partyName, selfId);
                    break;

                case "join":
                    if (string.IsNullOrWhiteSpace(partyName))
                    {
                        ForceMessage("Usage: /party join <name>");
                        break;
                    }
                    BroadcastPartyPacket("party_join_request", partyName, selfId);
                    ForceMessage($"Join request sent for party '{partyName}'.");
                    break;

                case "leave":
                    {
                        myParty = GetPartyOf(selfId);
                        if (string.IsNullOrEmpty(myParty))
                        {
                            ForceMessage("You are not in a party.");
                            break;
                        }

                        ulong ownerId = GetPartyMembers(myParty).FirstOrDefault();
                        if (ownerId == 0)
                        {
                            ForceMessage("Party owner not found, cannot leave.");
                            break;
                        }

                        if (ownerId == selfId)
                        {
                            DeleteParty(myParty);
                            BroadcastPartyPacket("party_deleted", myParty, selfId);
                            ForceMessage($"You deleted your party '{myParty}'.");
                        }
                        else
                        {
                            SendPartyPacket("party_leave_request", myParty, ownerId, selfId);
                            pendingPartyLeave = true;
                            pendingPartyLeaveTime = Time.realtimeSinceStartup;
                            pendingPartyLeaveName = myParty;

                            ForceMessage("Leave request sent... waiting for host confirmation.");
                        }
                        break;
                    }



                case "delete":
                    var partyToDelete = GetPartyOf(selfId);
                    if (partyToDelete == null)
                    {
                        ForceMessage("You are not in a party.");
                        break;
                    }
                    if (GetPartyMembers(partyToDelete).FirstOrDefault() != selfId)
                    {
                        ForceMessage("Only the party owner can delete the party.");
                        break;
                    }

                    foreach (var member in GetPartyMembers(partyToDelete))
                    {
                        if (member != selfId)
                            SendPartyDeletedPacket(member, partyToDelete, selfId);
                    }

                    DeleteParty(partyToDelete);
                    ForceMessage($"You deleted your party '{partyToDelete}'.");
                    break;


                case "list":
                    collectedPartyNames.Clear();
                    foreach (var p in Parties.Keys)
                        if (!PartyIsPrivate.TryGetValue(p, out var priv) || !priv)
                            collectedPartyNames.Add(p);
                    pendingPartyList = true;
                    pendingPartyListTime = UnityEngine.Time.realtimeSinceStartup;
                    BroadcastPartyPacket("party_ping", "", selfId);
                    break;

                case "kick":
                    if (myParty == null)
                    {
                        ForceMessage("You are not in a party.");
                        break;
                    }
                    if (GetPartyMembers(myParty).FirstOrDefault() != selfId)
                    {
                        ForceMessage("Only the party owner can kick.");
                        break;
                    }
                    if (arg.Length < 3)
                    {
                        ForceMessage("Usage: /party kick <playerName|steamIdPart>");
                        break;
                    }
                    string filter = arg[2].ToLowerInvariant();
                    ulong toKick = 0;
                    foreach (var id in GetPartyMembers(myParty))
                    {
                        if (id == selfId) continue;
                        var pname = GetPersonaName(id).ToLowerInvariant();
                        if (pname.Contains(filter) || id.ToString().Contains(filter))
                        {
                            toKick = id;
                            break;
                        }
                    }
                    if (toKick == 0)
                    {
                        ForceMessage("No such party member found.");
                        break;
                    }
                    if (!PartyBlacklist.TryGetValue(myParty, out var bl))
                    {
                        bl = new HashSet<ulong>();
                        PartyBlacklist[myParty] = bl;
                    }
                    LeaveParty(toKick);
                    bl.Add(toKick);
                    SendPartyPacket("party_kick", myParty, toKick, selfId);
                    ForceMessage($"Kicked {GetPersonaName(toKick)} from the party.");
                    foreach (var member in GetPartyMembers(myParty))
                    {
                        if (member != selfId)
                            SendPartyPacket("party_member_left", myParty, member, toKick);
                    }
                    break;

                case "member":
                    ForceMessage(ListPartyMembers(selfId));
                    break;

                default:
                    ForceMessage("Unknown /party command.");
                    break;
            }
        }



        public static void SyncPartyLobby()
        {
            string partyName = GetPartyOf(clientId);
            if (string.IsNullOrEmpty(partyName)) return;
            ulong ownerId = GetPartyMembers(partyName).FirstOrDefault();
            if (ownerId != clientId) return;

            ulong lobbyId = SteamManager.Instance.currentLobby.m_SteamID;
            if (lobbyId == 0) return;

            foreach (var member in GetPartyMembers(partyName))
            {
                if (member == clientId) continue;
                SendPartyPacket("party_sync_lobby", partyName, member, lobbyId);
            }
        }

    }
}
