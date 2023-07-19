using BepInEx;
using BepInEx.Configuration;


namespace mp18_plugin
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> force_failure_to_extract;
        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            force_failure_to_extract = Config.Bind("MP-18 settings", "Force failure to extract", false, "Make the extractor fail each time");
        }
    }
}
