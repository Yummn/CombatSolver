namespace CombatSolver;

/// <summary>
/// The first Android build only computes advice. Deployment remains disabled until
/// live-state differential tests cover the target game and installed gameplay mods.
/// Keep this compile-time gate independent of persisted desktop preferences.
/// </summary>
internal static class MobilePortPolicy
{
#if COMBAT_SOLVER_MOBILE
    internal static readonly bool AdviceOnly = true;
#else
    internal static readonly bool AdviceOnly = false;
#endif

    internal static SolverSettingsData Constrain(SolverSettingsData data)
    {
        if (!AdviceOnly)
            return data;

        return data with
        {
            AutomaticCalculationEnabled = false,
            AutoEnableFullAuto = false,
            OnlineStatisticsEnabled = false,
            SearchCompletionNotificationsEnabled = false,
            AutoConfigureServerGc = false,
            EnableNoGcRegion = false,
            PerformancePreset = SolverPerformancePreset.Custom,
            SearchMaxDegreeOfParallelism = 1,
            SearchTimeLimitSeconds = 3,
            SearchBeamWidth = 12,
            SearchMaxExpandedNodes = 2_000,
            SearchMaxCardBranchesPerNode = 8,
            SearchMaxPileChoiceBranchesPerAction = 4,
            SearchMaxHandChoiceBranchesPerAction = 4,
            UseBeamWidthPortfolio = false,
            UseNoveltyPortfolio = false,
            UseEarlyTurnExploration = false,
            OverlayOpacity = 0.92f,
        };
    }
}
