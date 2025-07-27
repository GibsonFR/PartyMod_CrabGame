global using BepInEx;
global using BepInEx.IL2CPP;
global using HarmonyLib;
global using SteamworksNative;
global using System;
global using System.Collections.Generic;
global using System.Linq;
global using System.Text;
global using UnityEngine;
global using System.Text.RegularExpressions;
global using UnityEngine.UI;
global using UnhollowerRuntimeLib;
global using UnhollowerBaseLib;
global using BepInEx.IL2CPP.Utils.Collections;

global using static PartyMod.Utility;
global using static PartyMod.Variables;


namespace PartyMod
{
    [BepInPlugin("party.mod", "PartyMod", "1.0.0")]
    public class Plugin : BasePlugin
    {
        public static Plugin __instance = null;

        public override void Load()
        {
            __instance = this;

            Harmony.CreateAndPatchAll(typeof(Plugin));
            Harmony.CreateAndPatchAll(typeof(ModUserFinderPatches));
            Log.LogInfo("Mod created by Gibson, discord : gib_son");

            Managers.RegisterIl2CppMonoBehaviours();
        }


        [HarmonyPatch(typeof(SteamManager), nameof(SteamManager.Awake))]
        [HarmonyPostfix]
        public static void OnSteamManagerAwakePost(SteamManager __instance)
        {
            if (clientId != 0) return;

            clientId = (ulong)__instance.field_Private_CSteamID_0;

            if (GameObject.Find("PartyMod") != null) return;

            GameObject pluginObj = new("PartyMod");

            foreach (var type in Managers.ManagerTypes)
            {
                typeof(GameObject)
                    .GetMethod(nameof(GameObject.AddComponent), Type.EmptyTypes)
                    .MakeGenericMethod(type)
                    .Invoke(pluginObj, null);
            }

            pluginObj.transform.SetParent(null);
            UnityEngine.Object.DontDestroyOnLoad(pluginObj);
        }

        //Anticheat Bypass 
        [HarmonyPatch(typeof(EffectManager), "Method_Private_Void_GameObject_Boolean_Vector3_Quaternion_0")]
        [HarmonyPatch(typeof(LobbyManager), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(MonoBehaviourPublicVesnUnique), "Method_Private_Void_0")]
        [HarmonyPatch(typeof(LobbySettings), "Method_Public_Void_PDM_2")]
        [HarmonyPatch(typeof(MonoBehaviourPublicTeplUnique), "Method_Private_Void_PDM_32")]
        [HarmonyPrefix]
        public static bool Prefix(System.Reflection.MethodBase __originalMethod)
        {
            return false;
        }
    }
}