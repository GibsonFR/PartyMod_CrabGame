using BepInEx.IL2CPP.Utils.Collections;
using static PartyMod.ModUserDiscoveryUtility;
using static PartyMod.ModUserDiscovery;
using System.Windows.Forms;

namespace PartyMod
{
    public class ModUserDiscoveryUtility
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
    }
    public class ModUserDiscovery : MonoBehaviour
    {
        public static bool createdLobby, lobbyParamSet, leftCreatedLobby;

        float elapsedTime = 0f;
        float elapsedTimeLobbyLifetime = 0f;
        float LOBBY_SCAN_INTERVAL = 2f;
        float LOBBY_LIFETIME = 5f;

        void Update()
        {
            elapsedTimeLobbyLifetime += Time.deltaTime; 
            elapsedTime += Time.deltaTime;

            if (elapsedTime > LOBBY_SCAN_INTERVAL)
            {
                elapsedTime = 0f;
                RequestLobbyScan();
            }

            if (elapsedTimeLobbyLifetime > LOBBY_LIFETIME && !leftCreatedLobby)
            {
                leftCreatedLobby = true;
                SteamManager.Instance.LeaveLobby();
            }
        }
    }

    public class ModUserDiscoveryPatches
    {
        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
        [HarmonyPostfix]
        public static void OnSteamManagerAwakePost()
        {
            if (!createdLobby)
            {
                createdLobby = true;
                SteamManager.Instance.CreateLobby(0);
            }
        }
      

        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Update))]
        [HarmonyPostfix]
        public static void OnSteamManagerUpdatePost(SteamManager __instance)
        {
            if (__instance.currentLobby.m_SteamID != 0 && !lobbyParamSet)
            {
                SetupLobbyParam();
                lobbyParamSet = true;
            }

        }

        [HarmonyPatch(typeof(SteamMatchmaking), nameof(SteamMatchmaking.RequestLobbyList))]
        [HarmonyPostfix]
        public static void OnSteamMatchmakingRequestLobbyListPost()
        {
            PartyModCoroutineRunner.Instance.StartCoroutine(ProcessLobbyListAfterDelay().WrapToIl2Cpp());
        }

        private static System.Collections.IEnumerator ProcessLobbyListAfterDelay()
        {
            yield return new WaitForSeconds(1f);

            int count = 10;
            for (int i = 0; i < count; i++)
            {
                try
                {
                    var lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
                    string modTag = SteamMatchmaking.GetLobbyData(lobbyId, "PartyMod");
                    string ownerTag = SteamMatchmaking.GetLobbyData(lobbyId, "clientId");

                    if (modTag == "true" && ulong.TryParse(ownerTag, out ulong ownerId))
                    {
                        if (ownerId != clientId && !modUsers.ContainsKey(ownerId))
                        {
                            ModUserSyncManager.cleanupTimer = 0f;
                            modUsers.Add(ownerId, 0f);

                            string pseudo = SteamFriends.GetFriendPersonaName(new CSteamID(ownerId));
                            pseudo = Regex.Replace(pseudo, "<.*?>", string.Empty);
                            PartyChatManager.Instance.AppendSecret($"trying to connect to {pseudo}.");

                            Plugin.__instance.Log.LogInfo($"Found mod user: {ownerId}");
                        }
                    }
                }
                catch { }
            }
        }
    }
}
