namespace PartyMod
{
    public class PartySyncManager : MonoBehaviour
    {
        
        private const float SyncInterval = 5f;
        private const float PartyListTimeout = 0.5f;

        private float syncTimer = 0f;

        public static string pendingPartyCreateName = null;
        public static ulong pendingPartyCreateSelfId = 0;
        public static bool pendingPartyNameExists = false;
        public static float pendingPartyCreateTime = 0f;

        void Update()
        {
            syncTimer += Time.deltaTime;

            if (syncTimer >= SyncInterval)
            {
                syncTimer = 0f;
                PartyManager.SyncPartyLobby();
            }

            if (PartyManager.pendingPartyList && Time.realtimeSinceStartup - PartyManager.pendingPartyListTime > PartyListTimeout)
            {
                if (PartyManager.PublicPartyList.Count == 0)
                {
                    ModUserChatUtility.AppendCustomMessage("No public parties found.");
                }
                else
                {
                    ModUserChatUtility.AppendCustomMessage($"Parties: {string.Join(", ", PartyManager.PublicPartyList)}");
                }

                PartyManager.pendingPartyList = false;
                PartyManager.PublicPartyList.Clear();
            }

            if (pendingPartyCreateName != null && Time.realtimeSinceStartup - pendingPartyCreateTime > 0.5f)
            {
                if (pendingPartyNameExists)
                {
                    ModUserChatUtility.AppendCustomMessage($"Party name '{pendingPartyCreateName}' already exists.");
                }
                else
                {
                    PartyManager.CreateParty(pendingPartyCreateSelfId, pendingPartyCreateName, false);
                    ModUserChatUtility.AppendCustomMessage($"Party '{pendingPartyCreateName}' created successfully.");
                }

                // Reset pending
                pendingPartyCreateName = null;
                pendingPartyCreateSelfId = 0;
                pendingPartyNameExists = false;
            }

        }

    }
}
