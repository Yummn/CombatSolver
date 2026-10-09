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
    private const string ReviewedHooks = "AfterPreventingDeath,ShouldDieLate";

    // NodeRewind renamed the same reviewed, detour-free death interceptor.
    // Keep exact identity/version pairs: a matching hook name alone must not
    // admit an unrelated assembly or a future unreviewed gameplay change.
    private static readonly (string ModId, string HookTypeName, string Version)[] ReviewedHooksByMod =
    [
        ("LoserEatDust", "LoserEatDust.LoserEatDustDeathHook", "0.3.1"),
        ("NodeRewind", "NodeRewind.NodeRewindDeathHook", "0.4.2"),
    ];

    internal static bool IsReviewedDeathHook(AbstractModel subscriber)
    {
        if (!MobilePortPolicy.IsMobile || subscriber.GetType() is not { } type
            || type.BaseType != typeof(AbstractModel))
            return false;

        var mod = AssemblyInfo.ModForType(type, out bool isBaseGame);
        if (isBaseGame || mod?.manifest is not { } manifest
            || !mod.assemblies.Contains(type.Assembly))
            return false;

        if (!ReviewedHooksByMod.Any(reviewed =>
                string.Equals(manifest.id, reviewed.ModId, StringComparison.OrdinalIgnoreCase)
                && string.Equals(type.FullName, reviewed.HookTypeName, StringComparison.Ordinal)
                && string.Equals(manifest.version?.ToString().TrimStart('v'),
                    reviewed.Version, StringComparison.Ordinal)))
            return false;

        PredictionModHookSubscriberInertness.IsCombatInert(type, out string hooks);
        return string.Equals(hooks, ReviewedHooks, StringComparison.Ordinal);
    }
}
