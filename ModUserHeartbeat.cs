namespace PartyMod
{
    public class ModUserHeartbeatManager : MonoBehaviour
    {
        private const float PingInterval = 1f;      
        private const float CleanupInterval = 5f;  
        private const float Timeout = 10f;          

        private float pingTimer = 0f;
        private float cleanupTimer = 0f;

        public static event Action<ulong> OnUserConnected;
        public static event Action<ulong> OnUserDisconnected;

        void Awake()
        {
            GibsonNetworkManager.OnPacketReceived += HandlePacket;

            OnUserConnected += userId => ModUserChatUtility.AppendCustomMessage($"Connected to {userId}");
  
            OnUserDisconnected += userId => ModUserChatUtility.AppendCustomMessage($"Lost connection with {userId}");
            
        }

        void Update()
        {
            pingTimer += Time.deltaTime;
            cleanupTimer += Time.deltaTime;

            if (pingTimer >= PingInterval)
            {
                pingTimer = 0f;
                BroadcastPing();
            }

            if (cleanupTimer >= CleanupInterval)
            {
                cleanupTimer = 0f;
                CleanupInactive();
            }
        }
        private static void BroadcastPing()
        {
            foreach (var user in connectedModUsers.Keys.ToList())
            {
                GibsonNetworkManager.SendPacket(user, "ping", true);
            }
        }
        private static void HandlePacket(ulong senderId, string message)
        {
            switch (message)
            {
                case "ping":
                    GibsonNetworkManager.SendPacket(senderId, "pong", true);
                    break;

                case "pong":
                    RegisterOrUpdate(senderId);
                    break;
            }
        }

        public static void RegisterOrUpdate(ulong userId)
        {
            bool isNew = !connectedModUsers.ContainsKey(userId);

            if (isNew)
            {
                connectedModUsers.Add(userId, 0f);
                OnUserConnected?.Invoke(userId);
            }
            connectedModUsers[userId] = Time.realtimeSinceStartup;
        }
        private static void CleanupInactive()
        {
            float now = Time.realtimeSinceStartup;
            var inactive = connectedModUsers
                .Where(x => now - x.Value > Timeout)
                .Select(x => x.Key)
                .ToList();

            foreach (var userId in inactive)
            {
                connectedModUsers.Remove(userId);
                detectedModUsers.Remove(userId);
                OnUserDisconnected?.Invoke(userId);
            }
        }
    }
}
