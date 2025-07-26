namespace PartyMod
{
    public class ModUserSyncManager : MonoBehaviour
    {
        private float pingInterval = 1f;
        private float cleanupInterval = 10f;
        private float pingTimer = 0f;
        public static float cleanupTimer = 0f;

        void Update()
        {
            pingTimer += Time.deltaTime;
            cleanupTimer += Time.deltaTime;

            if (pingTimer >= pingInterval)
            {
                pingTimer = 0f;
                BroadcastPing();
            }

            if (cleanupTimer >= cleanupInterval)
            {
                cleanupTimer = 0f;
                CleanupInactive();
            }
        }

        public static void BroadcastPing()
        {
            string msg = $"[GMF] ping {clientId}";
            var packets = CreateGMFPacket(msg);

            foreach (var user in connectedModUsers.Keys.ToList())
            {
                SendGMFPacket(user, packets);
            }
        }

        public static void ForceBroadcastPing()
        {
            foreach (var user in detectedModUsers) connectedModUsers[user] = 0f;
            BroadcastPing();

            CustomChatboxUtility.AppendCustomMessage("Forced network sync...");
        }


        public static void HandlePingPacket(ulong senderId)
        {
            string pseudo = SteamFriends.GetFriendPersonaName(new CSteamID(senderId));
            pseudo = Regex.Replace(pseudo, "<.*?>", string.Empty);

            if (!connectedModUsers.ContainsKey(senderId)) connectedModUsers.Add(senderId, 0f);
            if (!detectedModUsers.Contains(senderId)) detectedModUsers.Add(senderId);

            if (connectedModUsers[senderId] == 0f) CustomChatboxUtility.AppendCustomMessage($"connected to {pseudo}.");

            connectedModUsers[senderId] = Time.realtimeSinceStartup;
        }

        private static void CleanupInactive()
        {
            float now = Time.realtimeSinceStartup;
            var inactive = connectedModUsers.Where(x => now - x.Value > 20f).Select(x => x.Key).ToList();
            foreach (var id in inactive)
            {
                string pseudo = SteamFriends.GetFriendPersonaName(new CSteamID(id));
                pseudo = Regex.Replace(pseudo, "<.*?>", string.Empty);

                connectedModUsers.Remove(id);
                CustomChatboxUtility.AppendCustomMessage($"Lost connection with {pseudo}.");
            }
        }
    }
}
