using HarmonyLib;

/// <summary>
///     Applies randomizer modifier weights immediately after vanilla Global resources initialize.
/// </summary>
[HarmonyPatch(typeof(Global), nameof(Global.LoadInstance))]
internal static class GlobalLoadHook
{
    [HarmonyPostfix]
    private static void Postfix()
    {
        ModifierEnabler.TryEnable("Global.LoadInstance");
    }
}
