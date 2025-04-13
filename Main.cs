using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;


namespace CasualValheim   
{
    [BepInPlugin(pluginGUID, pluginName, pluginVersion)]
    public partial class Mod : BaseUnityPlugin
    {
        const string pluginGUID = "raelik.CasualValheim";
        const string pluginName = "CasualValheim";
        const string pluginVersion = "0.1.0";


        public static ConfigEntry<bool> VerboseLogEnabled;

        private readonly Harmony HarmonyInstance = new Harmony(pluginGUID);

        public static ManualLogSource logger = BepInEx.Logging.Logger.CreateLogSource(pluginName);

        partial void DeathClemencyInit();
        partial void OdinsEnduranceInit();

        public void Awake()
        {
            CVUtil.LogDebug("Loading START");
            Config.SaveOnConfigSet = false;

            CVUtil.Log("CasualValheim Initializing");

            VerboseLogEnabled = Config.Bind("CasualValheim", "VerboseLogEnabled", false, "enable verbose logging, which is noisy and/or expensive.  Requires BepInEx Debug level logging.");
            CVUtil.LoggingInit(VerboseLogEnabled.Value);

            DeathClemencyInit();
            OdinsEnduranceInit();

            Config.Save();
            Config.SaveOnConfigSet = true;

            Assembly assembly = Assembly.GetExecutingAssembly();
            HarmonyInstance.PatchAll(assembly);

            CVUtil.LogDebug("Loading END");
        }
    }
}