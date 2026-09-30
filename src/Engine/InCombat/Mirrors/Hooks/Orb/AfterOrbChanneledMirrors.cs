using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Orbs;
using CombatSolver.Engine.Common;
using CombatSolver.Engine.Common.Mirrors;

namespace CombatSolver.Engine.InCombat.Mirrors.Hooks.Orb;

using Registry = MethodMirrorRegistry<AbstractModel, AfterOrbChanneledMirrorContext>;

internal static class AfterOrbChanneledMirrors
{
    private static readonly MirrorMethodSpec Method = MirrorMethodSpec.Hook(
        nameof(AbstractModel.AfterOrbChanneled),
        [typeof(PlayerChoiceContext), typeof(Player), typeof(OrbModel)]);

    private static readonly Registry Registry = CreateRegistry();

    public static void Invoke(AbstractModel listener, AfterOrbChanneledMirrorContext context)
    {
        // These powers only install native orb-event subscriptions. Their
        // effects are resolved directly by the lightning/glass orb mirrors;
        // retaining native subscriptions in a branch would mutate live state.
        if (listener is PowerModel orbSubscription
            && BetterDefectMobileCompatibility.IsMirroredPower(orbSubscription)
            && orbSubscription.GetType().FullName is
                "BetterDefect.Cards.BdElectrodynamicsPower" or
                "BetterDefect.Cards.BdSpinnerNoDecayPower")
            return;
        if (listener is PowerModel power
            && BetterDefectMobileCompatibility.IsMirroredPower(power)
            && power.GetType().FullName == "BetterDefect.Cards.BdStormChargePower")
        {
            if (context.Player.Creature == power.Owner && context.Orb is LightningOrb
                && context.CombatState is SimulatedCombatState combat)
            {
                Type chargeType = BetterDefectMobileCompatibility.ReviewedPowerType(
                    "BetterDefect.Cards.BdStaticDischargeChargePower");
                PowerModel? prior = combat.EffectivePowers().FirstOrDefault(candidate =>
                    candidate.Owner == power.Owner && candidate.GetType() == chargeType
                    && candidate.Amount > 0);
                if (prior is not null)
                    _ = context.StateStore.Get(prior, () =>
                        new BetterDefectStormChargePredictionState(
                            BetterDefectMobileCompatibility.ReadStormChargeBatches(prior)));
                combat.ApplyPower(chargeType, power.Owner, power.Amount, power.Owner);
                PowerModel current = combat.EffectivePowers().Single(candidate =>
                    candidate.Owner == power.Owner && candidate.GetType() == chargeType
                    && candidate.Amount > 0);
                var state = context.StateStore.Get(current, () =>
                    new BetterDefectStormChargePredictionState([]));
                state.Batches.Add((combat.GetTotalCardPlayStartSerial(context.Simulator) + 1,
                    power.Amount));
            }
            return;
        }
        Registry.Invoke(listener, context);
    }

    private static Registry CreateRegistry()
    {
        var registry = new Registry(Method);
        registry.Register<Metronome>(HandleMetronome);
        return registry;
    }

    private static void HandleMetronome(Metronome relic, AfterOrbChanneledMirrorContext context)
    {
        if (context.Player != relic.Owner)
        {
            return;
        }

        var state = context.StateStore.Get(relic, () => new MetronomePredictionState(relic));
        state.OrbsChanneled++;

        if (state.OrbsChanneled == relic.DynamicVars[Metronome._orbCountKey].IntValue)
        {
            context.Simulator.Damage(
                context.State.HittableEnemies,
                relic.DynamicVars.Damage,
                relic.Owner.Creature);
        }
    }
}

internal sealed class AfterOrbChanneledMirrorContext : CombatMirrorContext
{
    public required Player Player { get; init; }

    public required OrbModel Orb { get; init; }
}

internal sealed class MetronomePredictionState(Metronome relic) : IPredictionStateForkable
{
    public int OrbsChanneled { get; set; } = relic._orbsChanneled;

    public object Fork(PredictionForkContext context) => MemberwiseClone();
}
