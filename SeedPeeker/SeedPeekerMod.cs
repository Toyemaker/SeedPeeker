using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SeedPeeker
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class SeedPeekerMod : BaseUnityPlugin
    {
        public static ManualLogSource DebugLogger;
        public const string pluginGuid = "toyemaker.plateup.seedpeeker";
        public const string pluginName = "Seed Peeker";
        public const string pluginVersion = "1.0";

        public void Awake()
        {
            DebugLogger = Logger;
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), pluginGuid);
        }
    }
}
