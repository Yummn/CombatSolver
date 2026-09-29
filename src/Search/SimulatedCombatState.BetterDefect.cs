using CombatSolver.Engine.InCombat.Simulation;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models;
using CombatSolver.Engine.InCombat.Mirrors.Hooks.Card;

namespace CombatSolver;

internal sealed partial class SimulatedCombatState
{
    public int GetTotalCardPlayStartSerial(CombatPredictionSimulator simulator)
        => _rootHistory.CardPlaysStarted.Length
            + simulator.History.Entries.Count(entry => entry is CombatPredictionCardPlayStartedEntry);

    private static void AddBetterDefectHiddenStates(
        ref StateFingerprintBuilder fingerprint,
        CombatPredictionSimulator simulator,
        IReadOnlyList<PowerModel> effectivePowers)
    {
        if (!BetterDefectMobileCompatibility.HasReviewedMobileMod)
            return;

        // These counters alter later card/draw/damage effects but are absent
        // from Power.Amount. Keep search branches with distinct counters apart.
        foreach (PowerModel power in effectivePowers)
        {
            if (power.Amount <= 0)
                continue;
            if (power is SubroutinePower
                && BetterDefectMobileCompatibility.IsTransformed<Subroutine>())
            {
                var state = simulator.StateStore.Peek(power, () =>
                {
                    var initial = BetterDefectMobileCompatibility.ReadOncePerRoundDrawState(
                        power, "BetterDefect.BdCustomSubroutinePowerPatch");
                    return new BetterDefectOncePerRoundDrawPredictionState(initial.Round, initial.Drew);
                });
                fingerprint.Add('b');
                fingerprint.Add(power.Owner.CombatId ?? uint.MaxValue);
                fingerprint.Add(state.Round);
                fingerprint.Add(state.Drew);
            }
            else if (power is SmokestackPower smokestack
                && BetterDefectMobileCompatibility.IsTransformed<Smokestack>())
            {
                var state = simulator.StateStore.Peek(power, () =>
                {
                    var initial = BetterDefectMobileCompatibility.ReadOncePerRoundDrawState(
                        power, "BetterDefect.BdCustomSmokestackPowerPatch");
                    return new BetterDefectOncePerRoundDrawPredictionState(initial.Round, initial.Drew);
                });
                int stacks = simulator.StateStore.Peek(power, () =>
                    new BetterDefectSmokestackPredictionState(
                        BetterDefectMobileCompatibility.ReadSmokestackStackCount(smokestack))).StackCount;
                fingerprint.Add('m');
                fingerprint.Add(power.Owner.CombatId ?? uint.MaxValue);
                fingerprint.Add(state.Round);
                fingerprint.Add(state.Drew);
                fingerprint.Add(stacks);
            }
            else if (power.GetType().FullName == "BetterDefect.Cards.BdStaticDischargeChargePower")
            {
                var state = simulator.StateStore.Peek(power, () =>
                    new BetterDefectStormChargePredictionState(
                        BetterDefectMobileCompatibility.ReadStormChargeBatches(power)));
                fingerprint.Add('c');
                fingerprint.Add(power.Owner.CombatId ?? uint.MaxValue);
                fingerprint.Add(state.Batches.Count);
                foreach (var batch in state.Batches)
                {
                    fingerprint.Add(batch.EligibleSerial);
                    fingerprint.Add(batch.Bonus);
                }
            }
        }
    }
}
