using System;
using System.Collections.Generic;
using System.Reflection;
using Pigeon;
using UnityEngine.Events;

public static class ModifierEnabler
{
    private const string CharacterMethod = "RandomizeCharacterOnRevive";
    private const string GearMethod = "RandomizeGearOnRevive";

    private static readonly string[] CharacterApiFallbacks =
        { "m_split", "m_personality", "m_char", "split_personality" };

    private static readonly string[] GearApiFallbacks = { "m_butter", "m_fingers", "m_gear", "butter_fingers" };

    private static bool _applied;


    public static void TryEnable(string reason, bool force = false)
    {
        if (_applied && !force)
            return;

        if (Global.Instance == null || Global.Instance.MissionModifiers == null)
        {
            RandomizerModifiersPlugin.Logger.LogDebug(
                $"[RandomizerModifiers] Global not ready yet ({reason}).");
            return;
        }

        try
        {
            EnableRandomizerModifiers();
            _applied = true;
        }
        catch (Exception ex)
        {
            RandomizerModifiersPlugin.Logger.LogError(
                $"[RandomizerModifiers] Failed to enable modifiers ({reason}): {ex}");
        }
    }

    private static void EnableRandomizerModifiers()
    {
        var pool = Global.Instance.MissionModifiers;
        var length = pool.Length;

        if (ConfigManager.LogModifiersOnLoad.Value)
            LogAllModifiers(pool, length);

        var characterIndex = -1;
        var gearIndex = -1;

        for (var i = 0; i < length; i++)
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

        if (characterIndex < 0)
            characterIndex = FindByApiName(pool, length, CharacterApiFallbacks);

        if (gearIndex < 0)
            gearIndex = FindByApiName(pool, length, GearApiFallbacks);

        var changed = false;

        if (ConfigManager.EnableSplitPersonality.Value)
        {
            if (characterIndex >= 0)
                changed |= SetWeight(pool, characterIndex, ConfigManager.SplitPersonalityWeight.Value,
                    "Split Personality");
            else
                RandomizerModifiersPlugin.Logger.LogWarning(
                    "[RandomizerModifiers] Could not find Split Personality (RandomizeCharacterOnRevive). " +
                    "Enable Debug.LogModifiersOnLoad to inspect the pool.");
        }
        else
        {
            RandomizerModifiersPlugin.Logger.LogInfo(
                "[RandomizerModifiers] Split Personality left unchanged (disabled in config).");
        }

        if (ConfigManager.EnableButterFingers.Value)
        {
            if (gearIndex >= 0)
                changed |= SetWeight(pool, gearIndex, ConfigManager.ButterFingersWeight.Value, "Butter Fingers");
            else
                RandomizerModifiersPlugin.Logger.LogWarning(
                    "[RandomizerModifiers] Could not find Butter Fingers (RandomizeGearOnRevive). " +
                    "Enable Debug.LogModifiersOnLoad to inspect the pool.");
        }
        else
        {
            RandomizerModifiersPlugin.Logger.LogInfo(
                "[RandomizerModifiers] Butter Fingers left unchanged (disabled in config).");
        }

        if (changed)
        {
            pool.SetupWeightSum();
            RandomizerModifiersPlugin.Logger.LogInfo(
                "[RandomizerModifiers] MissionModifiers weight sum refreshed.");
        }
        else
        {
            RandomizerModifiersPlugin.Logger.LogInfo(
                "[RandomizerModifiers] No weight changes were required.");
        }
    }

    private static bool SetWeight(WeightedArray<MissionModifier> pool, int index, int weight, string label)
    {
        var previous = pool.GetWeight(index);
        var modifier = pool[index];
        var api = modifier != null ? modifier.APIName : "?";

        if (previous == weight)
        {
            RandomizerModifiersPlugin.Logger.LogInfo(
                $"[RandomizerModifiers] {label} ('{api}') already has weight {weight} (index {index}).");
            return false;
        }

        pool.SetWeight(index, weight);
        RandomizerModifiersPlugin.Logger.LogInfo(
            $"[RandomizerModifiers] {label} ('{api}') weight {previous} -> {weight} (index {index}).");
        return true;
    }

    private static int FindByApiName(WeightedArray<MissionModifier> pool, int length, string[] candidates)
    {
        for (var i = 0; i < length; i++)
        {
            var modifier = pool[i];
            if (modifier == null || string.IsNullOrEmpty(modifier.APIName))
                continue;

            var api = modifier.APIName;
            for (var c = 0; c < candidates.Length; c++)
                if (api.IndexOf(candidates[c], StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
        }

        return -1;
    }

    private static bool TryGetActionMethodNames(MissionModifierGeneric generic, out HashSet<string> methods)
    {
        methods = new HashSet<string>(StringComparer.Ordinal);

        var actionField = typeof(MissionModifierGeneric).GetField(
            "action",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (actionField == null)
            return false;

        if (actionField.GetValue(generic) is not UnityEventBase unityEvent)
            return false;

        var count = unityEvent.GetPersistentEventCount();
        for (var i = 0; i < count; i++)
        {
            var methodName = unityEvent.GetPersistentMethodName(i);
            if (!string.IsNullOrEmpty(methodName))
                methods.Add(methodName);
        }

        return methods.Count > 0;
    }

    private static void LogAllModifiers(WeightedArray<MissionModifier> pool, int length)
    {
        RandomizerModifiersPlugin.Logger.LogInfo(
            $"[RandomizerModifiers] Dumping {length} mission modifiers:");
        for (var i = 0; i < length; i++)
        {
            var modifier = pool[i];
            if (modifier == null)
            {
                RandomizerModifiersPlugin.Logger.LogInfo(
                    $"  [{i}] <null> weight={pool.GetWeight(i)}");
                continue;
            }

            var methods = "";
            if (modifier is MissionModifierGeneric generic && TryGetActionMethodNames(generic, out var names))
                methods = " methods=[" + string.Join(", ", names) + "]";

            RandomizerModifiersPlugin.Logger.LogInfo(
                $"  [{i}] api='{modifier.APIName}' type={modifier.GetType().Name} " +
                $"weight={pool.GetWeight(i)} flags={modifier.Flags}{methods}");
        }
    }
}