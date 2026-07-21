using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Events;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
[MycoMod(null, ModFlags.IsClientSide)]
public class SparrohPlugin : BaseUnityPlugin
{
    public const string PluginGUID = "sparroh.randomizermodifiers";
    public const string PluginName = "RandomizerModifiers";
    public const string PluginVersion = "1.0.0";

    private const string CharacterMethod = "RandomizeCharacterOnRevive";
    private const string GearMethod = "RandomizeGearOnRevive";

    // Fallback API names if UnityEvent inspection fails (best-effort).
    private static readonly string[] CharacterApiFallbacks = { "m_split", "m_personality", "m_char", "split_personality" };
    private static readonly string[] GearApiFallbacks = { "m_butter", "m_fingers", "m_gear", "butter_fingers" };

    internal static new ManualLogSource Logger;
    internal static SparrohPlugin Instance;

    private Harmony _harmony;
    private bool _applied;

    private ConfigEntry<bool> _enableSplitPersonality;
    private ConfigEntry<bool> _enableButterFingers;
    private ConfigEntry<int> _splitPersonalityWeight;
    private ConfigEntry<int> _butterFingersWeight;
    private ConfigEntry<bool> _logModifiersOnLoad;

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;

        _enableSplitPersonality = Config.Bind(
            "General",
            "EnableSplitPersonality",
            true,
            "Re-enable the Split Personality modifier (randomize employee/character on revive).");

        _enableButterFingers = Config.Bind(
            "General",
            "EnableButterFingers",
            true,
            "Re-enable the Butter Fingers modifier (randomize weapons on revive).");

        _splitPersonalityWeight = Config.Bind(
            "Weights",
            "SplitPersonalityWeight",
            1,
            new ConfigDescription(
                "Spawn weight for Split Personality. Vanilla disabled it with 0; 1 matches a normal modifier.",
                new AcceptableValueRange<int>(0, 100)));

        _butterFingersWeight = Config.Bind(
            "Weights",
            "ButterFingersWeight",
            1,
            new ConfigDescription(
                "Spawn weight for Butter Fingers. Vanilla disabled it with 0; 1 matches a normal modifier.",
                new AcceptableValueRange<int>(0, 100)));

        _logModifiersOnLoad = Config.Bind(
            "Debug",
            "LogModifiersOnLoad",
            false,
            "Log every mission modifier API name and weight when Global loads.");

        _harmony = new Harmony(PluginGUID);
        _harmony.PatchAll(typeof(GlobalLoadHook));

        TryEnableModifiers("Awake");
        Logger.LogInfo($"{PluginName} v{PluginVersion} loaded.");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
        _harmony = null;
        Instance = null;
    }

    internal void TryEnableModifiers(string reason)
    {
        if (_applied)
            return;

        if (Global.Instance == null || Global.Instance.MissionModifiers == null)
        {
            Logger.LogDebug($"[RandomizerModifiers] Global not ready yet ({reason}).");
            return;
        }

        try
        {
            EnableRandomizerModifiers();
            _applied = true;
        }
        catch (Exception ex)
        {
            Logger.LogError($"[RandomizerModifiers] Failed to enable modifiers ({reason}): {ex}");
        }
    }

    private void EnableRandomizerModifiers()
    {
        var pool = Global.Instance.MissionModifiers;
        int length = pool.Length;

        if (_logModifiersOnLoad.Value)
            LogAllModifiers(pool, length);

        int characterIndex = -1;
        int gearIndex = -1;

        // Primary: identify MissionModifierGeneric entries by UnityEvent persistent method names.
        for (int i = 0; i < length; i++)
        {
            if (pool[i] is not MissionModifierGeneric generic)
                continue;

            if (!TryGetActionMethodNames(generic, out var methods))
                continue;

            if (characterIndex < 0 && methods.Contains(CharacterMethod))
                characterIndex = i;

            if (gearIndex < 0 && methods.Contains(GearMethod))
                gearIndex = i;
        }

        // Fallback: API name heuristics if event inspection missed them.
        if (characterIndex < 0)
            characterIndex = FindByApiName(pool, length, CharacterApiFallbacks);

        if (gearIndex < 0)
            gearIndex = FindByApiName(pool, length, GearApiFallbacks);

        bool changed = false;

        if (_enableSplitPersonality.Value)
        {
            if (characterIndex >= 0)
            {
                changed |= SetWeight(pool, characterIndex, _splitPersonalityWeight.Value, "Split Personality");
            }
            else
            {
                Logger.LogWarning(
                    "[RandomizerModifiers] Could not find Split Personality (RandomizeCharacterOnRevive). " +
                    "Enable Debug.LogModifiersOnLoad to inspect the pool.");
            }
        }
        else
        {
            Logger.LogInfo("[RandomizerModifiers] Split Personality left unchanged (disabled in config).");
        }

        if (_enableButterFingers.Value)
        {
            if (gearIndex >= 0)
            {
                changed |= SetWeight(pool, gearIndex, _butterFingersWeight.Value, "Butter Fingers");
            }
            else
            {
                Logger.LogWarning(
                    "[RandomizerModifiers] Could not find Butter Fingers (RandomizeGearOnRevive). " +
                    "Enable Debug.LogModifiersOnLoad to inspect the pool.");
            }
        }
        else
        {
            Logger.LogInfo("[RandomizerModifiers] Butter Fingers left unchanged (disabled in config).");
        }

        if (changed)
        {
            // Rebuild cached weight sum / sort order used by Mission.GetModifiers.
            pool.SetupWeightSum();
            Logger.LogInfo("[RandomizerModifiers] MissionModifiers weight sum refreshed.");
        }
        else
        {
            Logger.LogInfo("[RandomizerModifiers] No weight changes were required.");
        }
    }

    private static bool SetWeight(Pigeon.WeightedArray<MissionModifier> pool, int index, int weight, string label)
    {
        int previous = pool.GetWeight(index);
        MissionModifier modifier = pool[index];
        string api = modifier != null ? modifier.APIName : "?";

        if (previous == weight)
        {
            Logger.LogInfo($"[RandomizerModifiers] {label} ('{api}') already has weight {weight} (index {index}).");
            return false;
        }

        pool.SetWeight(index, weight);
        Logger.LogInfo(
            $"[RandomizerModifiers] {label} ('{api}') weight {previous} -> {weight} (index {index}).");
        return true;
    }

    private static int FindByApiName(Pigeon.WeightedArray<MissionModifier> pool, int length, string[] candidates)
    {
        for (int i = 0; i < length; i++)
        {
            MissionModifier modifier = pool[i];
            if (modifier == null || string.IsNullOrEmpty(modifier.APIName))
                continue;

            string api = modifier.APIName;
            for (int c = 0; c < candidates.Length; c++)
            {
                if (api.IndexOf(candidates[c], StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }
        }

        return -1;
    }

    private static bool TryGetActionMethodNames(MissionModifierGeneric generic, out HashSet<string> methods)
    {
        methods = new HashSet<string>(StringComparer.Ordinal);

        FieldInfo actionField = typeof(MissionModifierGeneric).GetField(
            "action",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (actionField == null)
            return false;

        if (actionField.GetValue(generic) is not UnityEventBase unityEvent)
            return false;

        int count = unityEvent.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
        {
            string methodName = unityEvent.GetPersistentMethodName(i);
            if (!string.IsNullOrEmpty(methodName))
                methods.Add(methodName);
        }

        return methods.Count > 0;
    }

    private static void LogAllModifiers(Pigeon.WeightedArray<MissionModifier> pool, int length)
    {
        Logger.LogInfo($"[RandomizerModifiers] Dumping {length} mission modifiers:");
        for (int i = 0; i < length; i++)
        {
            MissionModifier modifier = pool[i];
            if (modifier == null)
            {
                Logger.LogInfo($"  [{i}] <null> weight={pool.GetWeight(i)}");
                continue;
            }

            string methods = "";
            if (modifier is MissionModifierGeneric generic && TryGetActionMethodNames(generic, out var names))
                methods = " methods=[" + string.Join(", ", names) + "]";

            Logger.LogInfo(
                $"  [{i}] api='{modifier.APIName}' type={modifier.GetType().Name} " +
                $"weight={pool.GetWeight(i)} flags={modifier.Flags}{methods}");
        }
    }
}

[HarmonyPatch(typeof(Global), nameof(Global.LoadInstance))]
internal static class GlobalLoadHook
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        SparrohPlugin.Instance?.TryEnableModifiers("Global.LoadInstance");
    }
}
