using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsSandbox)]
public class RandomizerModifiersPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.randomizermodifiers";
    public const string PluginName = "RandomizerModifiers";
    public const string PluginVersion = "1.0.1";

    internal new static ManualLogSource Logger;
    internal static RandomizerModifiersPlugin Instance;

    private Harmony _harmony;

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        ConfigManager.Initialize(Config, Logger);

        _harmony = new Harmony(PluginGUID);

        try
        {
            _harmony.PatchAll(typeof(GlobalLoadHook));
            Logger.LogInfo("Harmony patches applied.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error applying patches: {ex.Message}");
        }


        ModifierEnabler.TryEnable("Awake");

        Logger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
    }

    private void Update()
    {
        ConfigManager.Tick();

        if (ConfigManager.ConsumePendingRefresh())
            ModifierEnabler.TryEnable("ConfigChanged", true);
    }

    private void OnDestroy()
    {
        ConfigManager.Dispose();
        _harmony?.UnpatchSelf();
        _harmony = null;
        Instance = null;
    }
}