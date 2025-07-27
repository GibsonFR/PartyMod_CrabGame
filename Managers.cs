namespace PartyMod
{
    public static class Managers
    {
        public static readonly Type[] ManagerTypes =
        [
            typeof(PartyManager),
            typeof(PartySyncManager),
            typeof(ModUserHeartbeatManager),
            typeof(ModUserFinder),
            typeof(PartyModCoroutineRunner),
            typeof(ModUserChatManager),
            typeof(GibsonNetworkManager),
        ];

        public static void RegisterIl2CppMonoBehaviours()
        {
            foreach (var type in ManagerTypes)
                ClassInjector.RegisterTypeInIl2Cpp(type);
        }
    }
}
