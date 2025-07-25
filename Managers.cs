namespace PartyMod
{
    public static class Managers
    {
        public static readonly Type[] ManagerTypes =
        {
            typeof(PartySyncManager),
            typeof(PartyChatManager),
            typeof(ModUserSyncManager),
            typeof(ModUserDiscovery),
            typeof(PartyModCoroutineRunner),
            typeof(CustomChatBox),
        };

        public static void RegisterIl2CppMonoBehaviours()
        {
            foreach (var type in ManagerTypes)
                ClassInjector.RegisterTypeInIl2Cpp(type);
        }
    }
}
