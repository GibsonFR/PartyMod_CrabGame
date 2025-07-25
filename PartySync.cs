namespace PartyMod
{
    public class PartySyncManager : MonoBehaviour
    {
        private float syncInterval = 1f;
        private float timer = 0f;


        void Update()
        {
            timer += Time.deltaTime;
            if (timer >= syncInterval)
            {
                timer = 0f;
                SyncPartyLobby();
            }

            if (pendingPartyCreateName != null)
            {
                if (Time.realtimeSinceStartup - pendingPartyCreateTime > 0.5f)
                {
                    if (pendingPartyNameExists)
                    {
                        ForceMessage($"Party name '{pendingPartyCreateName}' already exists.");
                    }
                    else if (CreateParty(pendingPartyCreateSelfId, pendingPartyCreateName, pendingPartyIsPrivate))
                    {
                        JoinParty(pendingPartyCreateSelfId, pendingPartyCreateName); 
                        ForceMessage($"Party '{pendingPartyCreateName}' created.");
                    }
                    else
                    {
                        ForceMessage($"Party name '{pendingPartyCreateName}' already exists.");
                    }

                    pendingPartyCreateName = null;
                    pendingPartyCreateSelfId = 0;
                    pendingPartyNameExists = false;
                    pendingPartyCreateTime = 0;
                }
            }

            if (pendingPartyLeave && Time.realtimeSinceStartup - pendingPartyLeaveTime > 0.5f)
            {
                ForceMessage($"Leave request failed for party '{pendingPartyLeaveName}'. Host did not respond.");
                pendingPartyLeave = false;
                pendingPartyLeaveName = null;
            }

            if (pendingPartyList)
            {
                if (UnityEngine.Time.realtimeSinceStartup - pendingPartyListTime > 0.3f)
                {
                    if (collectedPartyNames.Count == 0) ForceMessage("No parties found.");
                    else ForceMessage("Parties: " + string.Join(", ", collectedPartyNames));

                    pendingPartyList = false;
                    pendingPartyListTime = 0;
                    collectedPartyNames.Clear();
                }
            }


        }
    }
}
