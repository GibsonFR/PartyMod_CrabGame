﻿namespace PartyMod
{
    public static class Managers
    {
        public static readonly Type[] ManagerTypes =
        [
            typeof(PartySyncManager),
            typeof(ModUserSyncManager),
            typeof(ModUserDiscovery),
            typeof(PartyModCoroutineRunner),
            typeof(CustomChatBoxManager),
        ];

        public static void RegisterIl2CppMonoBehaviours()
        {
            foreach (var type in ManagerTypes)
                ClassInjector.RegisterTypeInIl2Cpp(type);
        }
    }
}
