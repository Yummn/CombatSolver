using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Monsters;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.ValueProps;
using CombatSolver.Engine.InCombat.Simulation;

namespace CombatSolver;

internal sealed partial class UnattendedTestRunner
{
    private async Task AssertSurpriseDeathSlotsAsync(CombatState combat, Player player)
    {
        Creature source = combat.Enemies.Single();
        await CreatureCmd.SetCurrentHp(source, 1);
        await SetBlockAsync(source, 0);
        foreach (PowerModel power in source.Powers.ToArray())
            await PowerCmd.Remove(power);
        await PowerCmd.Apply<SurprisePower>(
            new BlockingPlayerChoiceContext(), source, 1, source, null);
        await RunManager.Instance.ActionExecutor.FinishedExecutingActions();

        CombatPredictionSimulator simulator = CombatRootSnapshot.Capture(combat).ForkSimulator();
        SimulatedCombatState shadow = (SimulatedCombatState)simulator.State.CombatState;
        Creature predictedSource = shadow.Enemies.Single(enemy => enemy.CombatId == source.CombatId);
        simulator.Damage(predictedSource, 1, ValueProp.Unpowered, player.Creature);
        if (!CorePowerSupport.ApplyEnemyDeathPowers(
                simulator, shadow, shadow.KnownEnemies, new HashSet<uint>()))
            throw new InvalidOperationException("SurprisePower 槽位夹具意外请求了选牌。");

        Dictionary<string, string?> expected = shadow.Enemies
            .Where(enemy => enemy.Monster is FatGremlin or SneakyGremlin)
            .ToDictionary(
                enemy => enemy.Monster!.GetType().Name,
                enemy => enemy.SlotName,
                StringComparer.Ordinal);
        if (expected.Count != 2 || expected.Values.Any(string.IsNullOrWhiteSpace)
            || expected.Values.Distinct(StringComparer.Ordinal).Count() != 2)
            throw new InvalidOperationException(
                $"预测 SurprisePower 召唤槽位无效：{string.Join(",", expected.Select(pair => $"{pair.Key}={pair.Value}"))}");

        await CreatureCmd.Damage(
            new BlockingPlayerChoiceContext(), source, 1, ValueProp.Unpowered, player.Creature);
        await RunManager.Instance.ActionExecutor.FinishedExecutingActions();
        Dictionary<string, string?> actual = combat.Enemies
            .Where(enemy => enemy.Monster is FatGremlin or SneakyGremlin)
            .ToDictionary(
                enemy => enemy.Monster!.GetType().Name,
                enemy => enemy.SlotName,
                StringComparer.Ordinal);
        if (!expected.OrderBy(pair => pair.Key).SequenceEqual(actual.OrderBy(pair => pair.Key)))
            throw new InvalidOperationException(
                $"SurprisePower 预测/原生槽位不一致：expected={FormatSlots(expected)} actual={FormatSlots(actual)}");
        _completedChecks.Add(
            $"SurprisePower:Fat={expected[nameof(FatGremlin)]}:Sneaky={expected[nameof(SneakyGremlin)]}");
    }

    private static string FormatSlots(IReadOnlyDictionary<string, string?> slots)
        => string.Join(",", slots.OrderBy(pair => pair.Key)
            .Select(pair => $"{pair.Key}={pair.Value ?? "-"}"));
}
