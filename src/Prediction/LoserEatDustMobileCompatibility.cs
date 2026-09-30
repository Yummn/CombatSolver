using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Modding;

namespace CombatSolver;

/// <summary>
/// The reviewed Android death hook only changes what happens after a player
/// would die: it opens a retry dialog and restores a room-entry checkpoint.
/// Winning routes never exercise that hook. Prediction treats a lethal branch
/// as a terminal loss instead of simulating a new run inside the old combat.
/// </summary>
internal static class LoserEatDustMobileCompatibility
{
    private const string ModId = "LoserEatDust";
    private const string HookTypeName = "LoserEatDust.LoserEatDustDeathHook";
    private const string ReviewedVersion = "0.3.1";
    private const string ReviewedHooks = "AfterPreventingDeath,ShouldDieLate";

    internal static bool IsReviewedDeathHook(AbstractModel subscriber)
    {
        if (!MobilePortPolicy.IsMobile || subscriber.GetType() is not { } type
            || !string.Equals(type.FullName, HookTypeName, StringComparison.Ordinal)
            || type.BaseType != typeof(AbstractModel))
            return false;

        var mod = AssemblyInfo.ModForType(type, out bool isBaseGame);
        if (isBaseGame || mod?.manifest is not { } manifest
            || !string.Equals(manifest.id, ModId, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(manifest.version?.ToString().TrimStart('v'),
                ReviewedVersion, StringComparison.Ordinal)
            || !mod.assemblies.Contains(type.Assembly))
            return false;

        PredictionModHookSubscriberInertness.IsCombatInert(type, out string hooks);
        return string.Equals(hooks, ReviewedHooks, StringComparison.Ordinal);
    }
}
