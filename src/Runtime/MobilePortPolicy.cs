namespace CombatSolver;

/// <summary>
/// Android uses the normal search/deployment controller, but not desktop process
/// helpers or runtime GC configuration. Search bounds remain mobile-sized.
/// </summary>
internal static class MobilePortPolicy
{
#if COMBAT_SOLVER_MOBILE
    internal static readonly bool IsMobile = true;
#else
    internal static readonly bool IsMobile = false;
#endif

    internal static SolverSettingsData InitialSettings() => !IsMobile
        ? new SolverSettingsData()
        : new SolverSettingsData
        {
            PerformanceMigrationVersion = SolverSettings.CurrentPerformanceMigrationVersion,
            AutomaticCalculationEnabled = false,
            AutoEnableFullAuto = false,
            PerformancePreset = SolverPerformancePreset.Custom,
            SearchMaxDegreeOfParallelism = 1,
            SearchTimeLimitSeconds = 6,
            SearchBeamWidth = 16,
            SearchMaxExpandedNodes = 4_000,
            SearchMaxCardBranchesPerNode = 10,
            SearchMaxPileChoiceBranchesPerAction = 5,
            SearchMaxHandChoiceBranchesPerAction = 5,
            UseBeamWidthPortfolio = false,
            UseNoveltyPortfolio = false,
            UseEarlyTurnExploration = false,
            OnlineStatisticsEnabled = false,
            SearchCompletionNotificationsEnabled = false,
            AutoConfigureServerGc = false,
            EnableNoGcRegion = false,
            OverlayOpacity = 0.92f,
        };

    internal static SolverSettingsData Constrain(SolverSettingsData data)
    {
        if (!IsMobile)
            return data;

        // Desktop presets may request hundreds of thousands of expansions.
        // Resolve once and keep smaller player values from the advice preview.
        SolverSearchProfile profile = SolverSettings.ResolvePerformanceValues(data).Profile;
        return data with
        {
            SolverDisabled = false,
            AutomaticCalculationEnabled = data.AutoEnableFullAuto,
            StopFullAutoOnCombatEnd = false,
            OverlayTheme = SolverOverlayTheme.Dark,
            PerformancePreset = SolverPerformancePreset.Custom,
            SearchMaxDegreeOfParallelism = 1,
            SearchTimeLimitSeconds = Math.Clamp(profile.SoftTimeBudgetMilliseconds / 1000d, 1d, 30d),
            SearchBeamWidth = Math.Clamp(profile.BeamWidth, 1, 32),
            SearchMaxExpandedNodes = Math.Clamp(profile.MaxExpandedNodes, 100, 20_000),
            SearchMaxCardBranchesPerNode = Math.Clamp(profile.MaxCardBranchesPerNode, 1, 16),
            SearchMaxPileChoiceBranchesPerAction = Math.Clamp(profile.MaxPileChoiceBranchesPerAction, 1, 8),
            SearchMaxHandChoiceBranchesPerAction = Math.Clamp(profile.MaxHandChoiceBranchesPerAction, 1, 8),
            UseBeamWidthPortfolio = false,
            UseNoveltyPortfolio = false,
            UseEarlyTurnExploration = false,
            OnlineStatisticsEnabled = false,
            SearchCompletionNotificationsEnabled = false,
            AutoConfigureServerGc = false,
            EnableNoGcRegion = false,
        };
    }
}
