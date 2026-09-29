using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using CombatSolver.Engine.InCombat.Simulation;
using CombatSolver.Engine.InCombat.Mirrors.Hooks.Card;

namespace CombatSolver;

internal static partial class CardPowerOnPlaySupport
{
    private static void ApplyLate(
        CombatPredictionSimulator simulator,
        SimulatedCombatState combat,
        CardModel card,
        Creature owner)
    {
        switch (card)
        {
            case SeekingEdge:
                combat.Apply<SeekingEdgePower>(owner, 1, owner);
                break;
            case SentryMode:
                combat.Apply<SentryModePower>(owner, card.DynamicVars["SentryModePower"].IntValue, owner);
                break;
            case SerpentForm:
                combat.Apply<SerpentFormPower>(owner, card.DynamicVars["SerpentFormPower"].IntValue, owner);
                break;
            case Shadowmeld:
                combat.Apply<ShadowmeldPower>(owner, card.DynamicVars["Power"].IntValue, owner);
                break;
            case Shroud:
                combat.Apply<ShroudPower>(owner, card.DynamicVars.Block.IntValue, owner);
                break;
            case SignalBoost:
                combat.Apply<SignalBoostPower>(owner, card.DynamicVars["SignalBoostPower"].IntValue, owner);
                break;
            case SleightOfFlesh:
                combat.Apply<SleightOfFleshPower>(owner, card.DynamicVars["SleightOfFleshPower"].IntValue, owner);
                break;
            case Smokestack:
                if (BetterDefectMobileCompatibility.IsTransformed<Smokestack>())
                {
                    SmokestackPower? prior = combat.EffectivePowers().OfType<SmokestackPower>()
                        .FirstOrDefault(power => power.Owner == owner && power.Amount > 0);
                    if (prior is not null)
                    {
                        _ = simulator.StateStore.Get(prior, () =>
                            new BetterDefectSmokestackPredictionState(
                                BetterDefectMobileCompatibility.ReadSmokestackStackCount(prior)));
                        _ = simulator.StateStore.Get(prior, () =>
                        {
                            var initial = BetterDefectMobileCompatibility.ReadOncePerRoundDrawState(
                                prior, "BetterDefect.BdCustomSmokestackPowerPatch");
                            return new BetterDefectOncePerRoundDrawPredictionState(initial.Round, initial.Drew);
                        });
                    }
                }
                combat.Apply<SmokestackPower>(owner, card.DynamicVars["SmokestackPower"].IntValue, owner);
                if (BetterDefectMobileCompatibility.IsTransformed<Smokestack>())
                {
                    SmokestackPower current = combat.EffectivePowers().OfType<SmokestackPower>()
                        .Single(power => power.Owner == owner && power.Amount > 0);
                    var state = simulator.StateStore.Get(current, () =>
                        new BetterDefectSmokestackPredictionState(0));
                    state.StackCount++;
                }
                break;
            case SpectrumShift:
                combat.Apply<SpectrumShiftPower>(owner, card.DynamicVars.Cards.IntValue, owner);
                break;
            case Speedster:
                combat.Apply<SpeedsterPower>(owner, card.DynamicVars["SpeedsterPower"].IntValue, owner);
                break;
            case Spinner:
                if (BetterDefectMobileCompatibility.IsTransformed<Spinner>())
                    combat.ApplyPower(BetterDefectMobileCompatibility.ReviewedPowerType(
                        "BetterDefect.Cards.BdSpinnerNoDecayPower"), owner,
                        card.DynamicVars["SpinnerPower"].IntValue, owner);
                else
                    combat.Apply<SpinnerPower>(owner, card.DynamicVars["SpinnerPower"].IntValue, owner);
                break;
            case SpiritOfAsh:
                combat.Apply<SpiritOfAshPower>(owner, card.DynamicVars["BlockOnExhaust"].IntValue, owner);
                break;
            case Stampede:
                combat.Apply<StampedePower>(owner, card.DynamicVars["Power"].IntValue, owner);
                break;
            case StoneArmor:
                combat.Apply<PlatingPower>(owner, card.DynamicVars["PlatingPower"].IntValue, owner);
                break;
            case Storm:
                if (BetterDefectMobileCompatibility.IsTransformed<Storm>())
                    combat.ApplyPower(BetterDefectMobileCompatibility.ReviewedPowerType(
                        "BetterDefect.Cards.BdStormChargePower"), owner,
                        card.DynamicVars["StormPower"].IntValue, owner);
                else
                    combat.Apply<StormPower>(owner, card.DynamicVars["StormPower"].IntValue, owner);
                break;
            case Stratagem:
                combat.Apply<StratagemPower>(owner, 1, owner);
                break;
            case Subroutine:
                combat.Apply<SubroutinePower>(owner, 1, owner);
                break;
            case SwordSage:
                combat.Apply<SwordSagePower>(owner, card.DynamicVars["SwordSagePower"].IntValue, owner);
                break;
            case Terraforming:
                combat.Apply<VigorPower>(owner, card.DynamicVars["VigorPower"].IntValue, owner);
                break;
            case TheSealedThrone:
                combat.Apply<TheSealedThronePower>(owner, 1, owner);
                break;
            case Thunder:
                combat.Apply<ThunderPower>(owner, card.DynamicVars["ThunderPower"].IntValue, owner);
                break;
            case ToolsOfTheTrade:
                combat.Apply<ToolsOfTheTradePower>(owner, 1, owner);
                break;
            case TrashToTreasure:
                combat.Apply<TrashToTreasurePower>(owner, 1, owner);
                break;
            case Tyranny:
                combat.Apply<TyrannyPower>(owner, 1, owner);
                break;
            case Unmovable:
                combat.Apply<UnmovablePower>(owner, 1, owner);
                break;
            case Vicious:
                combat.Apply<ViciousPower>(owner, card.DynamicVars.Cards.IntValue, owner);
                break;
            case WellLaidPlans:
                combat.Apply<WellLaidPlansPower>(owner, 1, owner);
                break;
        }
    }
}
