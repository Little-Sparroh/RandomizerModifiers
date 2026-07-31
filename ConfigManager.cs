using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

public static class ConfigManager
{
    private const float DebounceSeconds = 0.25f;

    private static ConfigFile config;
    private static ManualLogSource logger;
    private static FileSystemWatcher configWatcher;
    private static volatile bool pendingRefresh;
    private static volatile bool reloadPending;
    private static float lastReloadTime;
    public static ConfigEntry<bool> EnableSplitPersonality { get; private set; }
    public static ConfigEntry<bool> EnableButterFingers { get; private set; }
    public static ConfigEntry<int> SplitPersonalityWeight { get; private set; }
    public static ConfigEntry<int> ButterFingersWeight { get; private set; }
    public static ConfigEntry<bool> LogModifiersOnLoad { get; private set; }

    public static void Initialize(ConfigFile configFile, ManualLogSource log)
    {
        config = configFile;
        logger = log;

        EnableSplitPersonality = config.Bind(
            "General",
            "Enable Split Personality",
            true,
            "Re-enable the Split Personality modifier (randomize employee/character on revive).");

        EnableButterFingers = config.Bind(
            "General",
            "Enable Butter Fingers",
            true,
            "Re-enable the Butter Fingers modifier (randomize weapons on revive).");

        SplitPersonalityWeight = config.Bind(
            "Weights",
            "Split Personality Weight",
            1,
            new ConfigDescription(
                "Spawn weight for Split Personality. Vanilla disabled it with 0; 1 matches a normal modifier.",
                new AcceptableValueRange<int>(0, 100)));

        ButterFingersWeight = config.Bind(
            "Weights",
            "Butter Fingers Weight",
            1,
            new ConfigDescription(
                "Spawn weight for Butter Fingers. Vanilla disabled it with 0; 1 matches a normal modifier.",
                new AcceptableValueRange<int>(0, 100)));

        LogModifiersOnLoad = config.Bind(
            "Debug",
            "Log Modifiers On Load",
            false,
            "Log every mission modifier API name and weight when Global loads.");

        EnableSplitPersonality.SettingChanged += OnSettingChanged;
        EnableButterFingers.SettingChanged += OnSettingChanged;
        SplitPersonalityWeight.SettingChanged += OnSettingChanged;
        ButterFingersWeight.SettingChanged += OnSettingChanged;
        LogModifiersOnLoad.SettingChanged += OnSettingChanged;

        try
        {
            SetupFileWatcher();
        }
        catch (Exception ex)
        {
            logger.LogError($"Error setting up config file watcher: {ex.Message}");
        }
    }


    public static void Tick()
    {
        if (!reloadPending)
            return;

        if (Time.unscaledTime - lastReloadTime < DebounceSeconds)
            return;

        reloadPending = false;
        lastReloadTime = Time.unscaledTime;

        try
        {
            config.Reload();
            pendingRefresh = true;
            logger.LogInfo("Config reloaded from disk.");
        }
        catch (Exception ex)
        {
            logger.LogError($"Error reloading config: {ex.Message}");
        }
    }

    public static bool ConsumePendingRefresh()
    {
        if (!pendingRefresh)
            return false;

        pendingRefresh = false;
        return true;
    }

    public static void Dispose()
    {
        if (EnableSplitPersonality != null)
            EnableSplitPersonality.SettingChanged -= OnSettingChanged;
        if (EnableButterFingers != null)
            EnableButterFingers.SettingChanged -= OnSettingChanged;
        if (SplitPersonalityWeight != null)
            SplitPersonalityWeight.SettingChanged -= OnSettingChanged;
        if (ButterFingersWeight != null)
            ButterFingersWeight.SettingChanged -= OnSettingChanged;
        if (LogModifiersOnLoad != null)
            LogModifiersOnLoad.SettingChanged -= OnSettingChanged;

        if (configWatcher != null)
        {
            configWatcher.EnableRaisingEvents = false;
            configWatcher.Changed -= OnConfigFileChanged;
            configWatcher.Created -= OnConfigFileChanged;
            configWatcher.Renamed -= OnConfigFileChanged;
            configWatcher.Dispose();
            configWatcher = null;
        }
    }

    private static void SetupFileWatcher()
    {
        configWatcher = new FileSystemWatcher(Paths.ConfigPath, $"{RandomizerModifiersPlugin.PluginGUID}.cfg");
        configWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName;
        configWatcher.Changed += OnConfigFileChanged;
        configWatcher.Created += OnConfigFileChanged;
        configWatcher.Renamed += OnConfigFileChanged;
        configWatcher.EnableRaisingEvents = true;
    }

    private static void OnConfigFileChanged(object sender, FileSystemEventArgs e)
    {
        reloadPending = true;
    }

    private static void OnSettingChanged(object sender, EventArgs e)
    {
        pendingRefresh = true;
    }
}