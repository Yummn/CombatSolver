using CombatSolver.Engine.Common;

namespace CombatSolver;

// A branch-local copy of BetterDefect's private per-hit charge batches.
// Power.Amount alone cannot distinguish charges created by the current card
// from those already eligible for that card's multiple damage instances.
internal sealed class BetterDefectStormChargePredictionState(
    List<(int EligibleSerial, int Bonus)> batches) : IPredictionStateForkable
{
    public List<(int EligibleSerial, int Bonus)> Batches { get; } = batches;

    public object Fork(PredictionForkContext context)
        => new BetterDefectStormChargePredictionState([.. Batches]);
}
