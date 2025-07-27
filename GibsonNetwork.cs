using Il2CppSystem.Linq;

namespace PartyMod
{
    public class GibsonNetworkManager : MonoBehaviour
    {
        public static event Action<ulong, string> OnPacketReceived; 

        private const int Channel = 1;
        private static readonly byte[] Buffer = new byte[4096];

        void Awake() => SteamNetworking.AllowP2PPacketRelay(true);

        void Update() => HandlePackets();

        public static void SendPacket(ulong receiverId, string message, bool reliable = true)
        {
            var steamId = new CSteamID(receiverId);
            SteamNetworking.AcceptP2PSessionWithUser(steamId); 
            byte[] data = Encoding.UTF8.GetBytes(message);

            bool sent = SteamNetworking.SendP2PPacket(
                steamId,
                data,
                (uint)data.Length,
                reliable ? EP2PSend.k_EP2PSendReliable : EP2PSend.k_EP2PSendUnreliable,
                Channel
            );

            Plugin.__instance.Log.LogInfo($"[P2P] SendPacket à {receiverId} ({message}) => {(sent ? "OK" : "FAIL")}");
        }


        public static void ForceTryReconnect()
        {
            foreach (var user in detectedModUsers)
            {
                if (!connectedModUsers.ContainsKey(user)) connectedModUsers.Add(user, 0f);
            }
        }

        private static void HandlePackets()
        {
            while (SteamNetworking.IsP2PPacketAvailable(out uint msgSize, Channel))
            {
                if (msgSize > Buffer.Length) return;

                var il2cppBuffer = new Il2CppStructArray<byte>((int)msgSize);
                if (SteamNetworking.ReadP2PPacket(il2cppBuffer, (uint)il2cppBuffer.Length, out uint bytesRead, out CSteamID remoteId, Channel))
                {
                    byte[] managed = il2cppBuffer.ToArray();
                    string message = Encoding.UTF8.GetString(managed, 0, (int)bytesRead).TrimEnd('\0', ' ');

                    SteamNetworking.AcceptP2PSessionWithUser(remoteId);

                    ModUserHeartbeatManager.RegisterOrUpdate(remoteId.m_SteamID);

                    Plugin.__instance.Log.LogInfo($"[P2P] Packet reçu de {remoteId.m_SteamID} : {message}");

                    OnPacketReceived?.Invoke(remoteId.m_SteamID, message);
                }
            }
        }


    }
}
