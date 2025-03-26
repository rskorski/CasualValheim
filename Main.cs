using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;


namespace CasualValheim   
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public partial class Mod : BaseUnityPlugin
    {
        const string pluginGUID = "raelik.CasualValheim";
        const string pluginName = "CasualValheim";
        const string pluginVersion = "0.0.1";


        public static ConfigEntry<bool> DbgLogEnabled;

        private readonly Harmony HarmonyInstance = new Harmony(pluginGUID);

        public static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        partial void DeathClemencyConfig();
        partial void OdinsEnduranceConfig();

        public void Awake()
        {
            CVUtil.Log("Loading START", debug:false);
            Config.SaveOnConfigSet = false;

            DbgLogEnabled = Config.Bind("CasualValheim", "DbgLogEnabled", true, "enable debug log output");

            DeathClemencyConfig();
            OdinsEnduranceConfig();

            Config.Save();
            Config.SaveOnConfigSet = true;

            Assembly assembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(assembly);

            CVUtil.Log("Loading END", debug:false);
        }
    }
}