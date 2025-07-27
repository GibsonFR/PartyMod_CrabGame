using static PartyMod.PartySyncManager;
namespace PartyMod
{
    public class PartyManager : MonoBehaviour
    {
        private static readonly Dictionary<string, HashSet<ulong>> Parties = new();
        private static readonly Dictionary<ulong, string> PlayerToParty = new();
        private static readonly Dictionary<string, bool> PartyPrivacy = new();
        private static readonly Dictionary<string, HashSet<ulong>> PartyBlacklist = new();

        public static HashSet<string> PublicPartyList = new();
        public static bool pendingPartyList = false;
        public static float pendingPartyListTime = 0f;

        private const float PartyListTimeout = 3f;

        void Awake() => GibsonNetworkManager.OnPacketReceived += HandlePartyPacket;


        // ---------------------------
        // PARTY COMMANDS (/party)
        // ---------------------------
        public static void HandlePartyCommand(string input, ulong selfId)
        {
            var args = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (args.Length < 2)
            {
                AppendMsg("Usage: /party <create|join|leave|delete|kick|list|member>");
                return;
            }

            string cmd = args[1].ToLowerInvariant();
            string partyName = args.Length > 2 ? args[2] : null;
            string myParty = GetPartyOf(selfId);

            switch (cmd)
            {
                case "create":
                    if (string.IsNullOrWhiteSpace(partyName))
                    {
                        AppendMsg("Usage: /party create <name>");
                        break;
                    }

                    pendingPartyCreateName = partyName;
                    pendingPartyCreateSelfId = selfId;
                    pendingPartyCreateTime = Time.realtimeSinceStartup;
                    pendingPartyNameExists = false;

                    Broadcast($"party|check_exists|{partyName}|{selfId}");
                    AppendMsg($"Checking availability for '{partyName}'...");
                    break;


                case "join":
                    if (string.IsNullOrWhiteSpace(partyName))
                    {
                        AppendMsg("Usage: /party join <name>");
                        break;
                    }
                    Broadcast($"party|join_request|{partyName}|{selfId}");
                    AppendMsg($"Join request sent for '{partyName}'.");
                    break;

                case "leave":
                    if (string.IsNullOrEmpty(myParty))
                    {
                        AppendMsg("You are not in a party.");
                        break;
                    }
                    HandleLeave(selfId, myParty);
                    break;

                case "delete":
                    if (string.IsNullOrEmpty(myParty))
                    {
                        AppendMsg("You are not in a party.");
                        break;
                    }
                    if (!IsOwner(myParty, selfId))
                    {
                        AppendMsg("Only the party owner can delete.");
                        break;
                    }
                    Broadcast($"party|deleted|{myParty}|{selfId}");
                    DeleteParty(myParty);
                    AppendMsg($"Party '{myParty}' deleted.");
                    break;



                case "kick":
                    if (myParty == null)
                    {
                        AppendMsg("You are not in a party.");
                        break;
                    }
                    if (!IsOwner(myParty, selfId))
                    {
                        AppendMsg("Only the owner can kick.");
                        break;
                    }
                    if (args.Length < 3)
                    {
                        AppendMsg("Usage: /party kick <playerId>");
                        break;
                    }
                    if (ulong.TryParse(args[2], out ulong toKick))
                    {
                        if (GetPartyMembers(myParty).Contains(toKick))
                        {
                            Broadcast($"party|kick|{myParty}|{toKick}");
                            LeaveParty(toKick);
                            AppendMsg($"Kicked {GetName(toKick)} from '{myParty}'.");
                        }
                        else AppendMsg("Player not in your party.");
                    }
                    break;

                case "list":
                    PublicPartyList.Clear();
                    pendingPartyList = true;
                    pendingPartyListTime = Time.realtimeSinceStartup;
                    Broadcast("party|ping||");
                    AppendMsg("Requesting public party list...");
                    break;

                case "member":
                    AppendMsg(ListMembers(selfId));
                    break;

                default:
                    AppendMsg("Unknown /party command.");
                    break;
            }
        }

        // ---------------------------
        // PACKET HANDLER
        // ---------------------------
        private static void HandlePartyPacket(ulong senderId, string message)
        {
            if (!message.StartsWith("party|")) return;

            var parts = message.Split('|');
            if (parts.Length < 2) return;

            string cmd = parts[1];
            string partyName = parts.Length > 2 ? parts[2] : null;
            ulong paramId = parts.Length > 3 && ulong.TryParse(parts[3], out var pid) ? pid : 0;

            switch (cmd)
            {
                case "created":
                    AppendMsg($"Party '{partyName}' created by {GetName(senderId)}.");
                    break;

                case "check_exists":
                    if (Parties.ContainsKey(partyName))
                    {
                        GibsonNetworkManager.SendPacket(senderId, $"party|exists|{partyName}");
                    }
                    break;

                case "exists":
                    if (pendingPartyCreateName == partyName)
                        pendingPartyNameExists = true;
                    break;


                case "join_request":
                    if (IsOwner(partyName, clientId))
                    {
                        JoinParty(paramId, partyName);
                        Broadcast($"party|member_added|{partyName}|{paramId}");
                        AppendMsg($"{GetName(paramId)} joined your party.");
                    }
                    break;

                case "member_added":
                    if (GetPartyOf(paramId) == null)
                    {
                        JoinParty(paramId, partyName);
                        AppendMsg($"{GetName(paramId)} joined '{partyName}'.");
                    }
                    break;

                case "kick":
                    if (paramId == clientId)
                    {
                        LeaveParty(clientId);
                        AppendMsg("You have been kicked from the party.");
                    }
                    break;

                case "deleted":
                    if (Parties.ContainsKey(partyName))
                    {
                        if (IsOwner(partyName, senderId)) 
                        {
                            LeaveParty(clientId);
                            DeleteParty(partyName);
                            AppendMsg($"Party '{partyName}' deleted by {GetName(senderId)}.");
                        }
                        else
                        {
                            AppendMsg($"[SECURITY] {GetName(senderId)} tried to delete '{partyName}' but is not the owner!");
                        }
                    }
                    break;


                case "ping":
                    foreach (var p in Parties.Keys)
                        if (!PartyPrivacy[p])
                            GibsonNetworkManager.SendPacket(senderId, $"party|pong|{p}");
                    break;

                case "pong":
                    if (pendingPartyList && !string.IsNullOrEmpty(partyName))
                        PublicPartyList.Add(partyName);
                    break;

                case "sync_lobby":
                    if (paramId != 0)
                    {
                        ulong lobbyId = paramId;
                        if (SteamManager.Instance.currentLobby.m_SteamID != lobbyId)
                        {
                            AppendMsg("Joining your party host's lobby...");
                            SteamManager.Instance.LeaveLobby();
                            SteamManager.Instance.JoinLobby(new CSteamID(lobbyId));
                        }
                    }
                    break;
            }
        }


        // ---------------------------
        // PARTY LOGIC
        // ---------------------------
        public static void SyncPartyLobby()
        {
            string partyName = GetPartyOf(clientId);
            if (string.IsNullOrEmpty(partyName)) return;

            ulong ownerId = GetPartyMembers(partyName).FirstOrDefault();
            if (ownerId != clientId) return; // seul l'owner sync

            ulong lobbyId = SteamManager.Instance.currentLobby.m_SteamID;
            if (lobbyId == 0) return;

            foreach (var member in GetPartyMembers(partyName))
            {
                if (member == clientId) continue;
                string message = $"party|sync_lobby|{partyName}|{lobbyId}";
                GibsonNetworkManager.SendPacket(member, message);
            }
        }

        public static bool CreateParty(ulong ownerId, string name, bool isPrivate)
        {
            if (Parties.ContainsKey(name)) return false;
            Parties[name] = new HashSet<ulong> { ownerId };
            PlayerToParty[ownerId] = name;
            PartyPrivacy[name] = isPrivate;
            return true;
        }

        public static bool JoinParty(ulong playerId, string name)
        {
            if (!Parties.ContainsKey(name)) return false;
            Parties[name].Add(playerId);
            PlayerToParty[playerId] = name;
            return true;
        }

        public static void LeaveParty(ulong playerId)
        {
            if (!PlayerToParty.TryGetValue(playerId, out var name)) return;
            if (Parties.TryGetValue(name, out var members))
            {
                members.Remove(playerId);
                PlayerToParty.Remove(playerId);
                if (members.Count == 0) DeleteParty(name);
            }
        }

        private static void HandleLeave(ulong selfId, string myParty)
        {
            if (IsOwner(myParty, selfId))
            {
                Broadcast($"party|deleted|{myParty}|{selfId}");
                DeleteParty(myParty);
                AppendMsg($"You deleted '{myParty}'.");
            }
            else
            {
                Broadcast($"party|member_left|{myParty}|{selfId}");
                LeaveParty(selfId);
                AppendMsg("You left the party.");
            }
        }

        public static void DeleteParty(string name)
        {
            if (!Parties.ContainsKey(name)) return;
            foreach (var m in Parties[name]) PlayerToParty.Remove(m);
            Parties.Remove(name);
            PartyPrivacy.Remove(name);
            PartyBlacklist.Remove(name);
        }

        public static bool IsOwner(string partyName, ulong playerId)
            => Parties.TryGetValue(partyName, out var members) && members.FirstOrDefault() == playerId;

        public static string GetPartyOf(ulong playerId)
            => PlayerToParty.TryGetValue(playerId, out var name) ? name : null;

        public static IEnumerable<ulong> GetPartyMembers(string name)
            => Parties.TryGetValue(name, out var members) ? members : Enumerable.Empty<ulong>();

        public static string ListMembers(ulong playerId)
        {
            var party = GetPartyOf(playerId);
            if (party == null) return "You are not in a party.";
            var members = GetPartyMembers(party).Select(id => GetName(id));
            return $"Party '{party}' members: {string.Join(", ", members)}";
        }

        private static void Broadcast(string packet)
        {
            foreach (var user in connectedModUsers.Keys)
                if (user != clientId)
                    GibsonNetworkManager.SendPacket(user, packet);
        }

        private static string GetName(ulong steamId)
            => Regex.Replace(SteamFriends.GetFriendPersonaName(new CSteamID(steamId)), "<.*?>", string.Empty);

        private static void AppendMsg(string text) => ModUserChatUtility.AppendCustomMessage(text);
        
    }
}
