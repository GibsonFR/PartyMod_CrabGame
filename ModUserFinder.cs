using static PartyMod.ModUserFinderUtility;
using static PartyMod.ModUserFinder;

namespace PartyMod
{
    public static class ModUserFinderUtility
    {
        public static void RequestLobbyScan()
        {
            SteamMatchmaking.AddRequestLobbyListStringFilter("PartyMod", "true", ELobbyComparison.k_ELobbyComparisonEqual);
            SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
            SteamMatchmaking.RequestLobbyList();
        }

        public static void SetupLobbyParam()
        {
            var lobby = SteamManager.Instance.currentLobby;
            SteamMatchmaking.SetLobbyData(lobby, "PartyMod", "true");
            SteamMatchmaking.SetLobbyData(lobby, "clientId", $"{clientId}");
        }

        public static Packet BuildConnectionPacket(int packetType, byte[] payload, ulong senderId)
        {
            int size = 16 + payload.Length;
            var buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(size - 4));
            buffer.AddRange(BitConverter.GetBytes(packetType));
            buffer.AddRange(BitConverter.GetBytes(senderId));
            buffer.AddRange(payload);
            var packet = new Packet();
            packet.field_Private_List_1_Byte_0.Clear();
            foreach (var b in buffer)
                packet.field_Private_List_1_Byte_0.Add(b);
            return packet;
        }

        public static Packet CreateConnectionPacket(string content)
        {
            byte[] msgBytes = Encoding.UTF8.GetBytes(content);
            var payload = new List<byte>();


            payload.AddRange(BitConverter.GetBytes(msgBytes.Length));

            payload.AddRange(msgBytes);

            return BuildConnectionPacket((int)ClientSendType.ping, payload.ToArray(), clientId);


        }
        public static void SendConnectionPacket(ulong receiverId)
        {
            Packet packet = CreateConnectionPacket("connect");
            SteamPacketManager.SendPacket(new CSteamID(receiverId), packet, 8, SteamPacketDestination.ToServer);
        }

        public static System.Collections.IEnumerator ProcessLobbyListAfterDelay()
        {
            yield return new WaitForSeconds(1f);

            int count = 10;
            for (int i = 0; i < count; i++)
            {
                var lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
                string modTag = SteamMatchmaking.GetLobbyData(lobbyId, "PartyMod");
                string ownerTag = SteamMatchmaking.GetLobbyData(lobbyId, "clientId");

                if (modTag == "true" && ulong.TryParse(ownerTag, out ulong modUserId) && modUserId != clientId)
                {
                    bool isNew = !detectedModUsers.Contains(modUserId);
                    if (isNew)
                    {
                        detectedModUsers.Add(modUserId);

                        SteamNetworking.AcceptP2PSessionWithUser(new CSteamID(modUserId));

                        SendConnectionPacket(modUserId);

                        ModUserChatUtility.AppendCustomMessage($"Found mod user: {modUserId}, sending connection packet...");
                    }
                }
            }
        }

        public static string ExtractPayload(Packet packet)
        {
            try
            {
                var list = packet.field_Private_List_1_Byte_0;
                if (list.Count <= 20) return string.Empty;

                int payloadSize = BitConverter.ToInt32(list.ToArray(), 16);
                if (payloadSize <= 0) return string.Empty;

                byte[] payload = new byte[payloadSize];
                for (int i = 0; i < payloadSize; i++)
                {
                    payload[i] = list[20 + i];
                }

                return Encoding.UTF8.GetString(payload);
            }
            catch
            {
                return string.Empty;
            }
        }


    }

    public class ModUserFinder : MonoBehaviour
    {
        private const float LobbyScanInterval = 3f;
        private const float LobbyLifetime = 7f;

        public static bool createdLobby, leftCreatedLobby;
        private float scanTimer, lifetimeTimer;

        void Update()
        {
            scanTimer += Time.deltaTime;
            lifetimeTimer += Time.deltaTime;

            if (scanTimer >= LobbyScanInterval)
            {
                scanTimer = 0f;
                RequestLobbyScan();
            }

            if (lifetimeTimer >= LobbyLifetime && !leftCreatedLobby)
            {
                leftCreatedLobby = true;
                SteamManager.Instance.LeaveLobby();
            }
        }
    }
    public static class ModUserFinderPatches
    {
        [HarmonyPatch(typeof(ServerHandle), nameof(ServerHandle.Ping))]
        [HarmonyPrefix]
        public static bool OnServerHandlePing(ulong __0, Packet __1)
        {
            ulong senderId = __0;
            if (senderId == 0 || senderId == clientId) return true;

            string message = ExtractPayload(__1);

            if (message.Contains("connect")) 
            {
                detectedModUsers.Add(senderId);
                GibsonNetworkManager.SendPacket(senderId, "pong", true);
                GibsonNetworkManager.SendPacket(senderId, "ping", true);
            }

            return true;
        }

        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
        [HarmonyPostfix]
        public static void OnSteamManagerAwake()
        {
            if (!createdLobby)
            {
                createdLobby = true;
                SteamManager.Instance.CreateLobby(0);
            }
        }

        [HarmonyPatch(typeof(PlayerMovement), nameof(PlayerMovement.Awake))]
        [HarmonyPostfix]
        public static void OnPlayerMovementAwake()
        {
            if (SteamManager.Instance?.currentLobby.m_SteamID != 0) SetupLobbyParam();
            
        }

        [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.RequestLobbyList))]
        [HarmonyPostfix]
        public static void OnLobbyListRequested()
        {
            PartyModCoroutineRunner.Instance.StartCoroutine(ProcessLobbyListAfterDelay().WrapToIl2Cpp());
        }
    }
}
