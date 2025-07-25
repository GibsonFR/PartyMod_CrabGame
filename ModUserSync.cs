namespace PartyMod
{
    public class ModUserSyncManager : MonoBehaviour
    {
        private float pingInterval = 1f;
        private float cleanupInterval = 20f;
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

            foreach (var user in modUsers.Keys.ToList())
            {
                SendGMFPacket(user, packets);
            }
        }

        public static void HandlePingPacket(ulong senderId)
        {
            string pseudo = SteamFriends.GetFriendPersonaName(new CSteamID(senderId));
            pseudo = Regex.Replace(pseudo, "<.*?>", string.Empty);

            if (!modUsers.ContainsKey(senderId)) modUsers.Add(senderId, 0f);

            if (modUsers[senderId] == 0f) PartyChatManager.Instance.AppendSecret($"connected to {pseudo}.");

            modUsers[senderId] = Time.realtimeSinceStartup;
        }

        private static void CleanupInactive()
        {
            float now = Time.realtimeSinceStartup;
            var inactive = modUsers.Where(x => now - x.Value > 5f).Select(x => x.Key).ToList();
            foreach (var id in inactive)
            {
                string pseudo = SteamFriends.GetFriendPersonaName(new CSteamID(id));
                pseudo = Regex.Replace(pseudo, "<.*?>", string.Empty);

                modUsers.Remove(id);
                PartyChatManager.Instance.AppendSecret($"failed to establish stable connection with {pseudo}.");
            }
        }
    }
}
