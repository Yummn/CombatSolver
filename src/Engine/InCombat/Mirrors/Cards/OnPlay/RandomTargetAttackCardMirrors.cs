using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Extensions;
using CombatSolver.Engine.InCombat.Simulation;

namespace CombatSolver.Engine.InCombat.Mirrors.Cards.OnPlay;

internal static class RandomTargetAttackCardMirrors
{
    public static void FlakCannonOnPlay(FlakCannon card, CardOnPlayMirrorContext context)
    {
        var statuses = context.OwnerState.AllCards
            .Where(predictedCard =>
                predictedCard.Preview.Type is CardType.Status &&
                !context.OwnerState.ExhaustPile.Cards.Contains(predictedCard))
            .ToList();

        foreach (var status in statuses)
        {
            context.Simulator.Exhaust(status);
            if (context.Simulator.HasPendingChoice)
                return;
        }

        if (BetterDefectMobileCompatibility.IsTransformed<FlakCannon>())
        {
            // The transformed card targets one enemy and counts the *entire*
            // exhaust pile after exhausting statuses, not just those statuses.
            int hitCount = context.OwnerState.ExhaustPile.Cards.Count;
            if (hitCount > 0)
                context.AttackSingle(hitCount);
        }
        else
        {
            context.AttackRandomOpponents(statuses.Count);
        }
    }

    public static void RicochetOnPlay(Ricochet card, CardOnPlayMirrorContext context)
    {
        context.AttackRandomOpponents(card.DynamicVars.Repeat.IntValue);
    }

    public static void RipAndTearOnPlay(RipAndTear card, CardOnPlayMirrorContext context)
    {
        if (BetterDefectMobileCompatibility.IsTransformed<RipAndTear>())
        {
            Dictionary<MegaCrit.Sts2.Core.Entities.Creatures.Creature, int> counts = [];
            for (int i = 0; i < 3 && !context.Simulator.HasPendingChoice; i++)
            {
                var enemies = context.State.HittableEnemies.ToList();
                if (enemies.Count == 0)
                    break;
                var target = context.Rng.CombatTargets.NextItem(enemies)
                    ?? throw new InvalidOperationException("RipAndTear has no random target.");
                counts[target] = counts.GetValueOrDefault(target) + 1;
                // BetterDefect uses CreatureCmd.Damage with the card as source,
                // not DamageCmd.Attack: no attack-start/card-play hook per hit.
                context.Simulator.Damage([target], card.DynamicVars.Damage.BaseValue,
                    card.DynamicVars.Damage.Props, card.Owner.Creature, context.Card, null);
            }
            foreach (var target in counts.Where(pair => pair.Value >= 2).Select(pair => pair.Key))
            {
                if (context.Simulator.HasPendingChoice)
                    return;
                if (!context.State.IsHittable(target))
                    continue;
                context.Simulator.Damage([target], card.DynamicVars.Damage.BaseValue,
                    card.DynamicVars.Damage.Props, card.Owner.Creature, context.Card, null);
            }
            return;
        }
        context.AttackRandomOpponents(hitCount: 2);
    }

    public static void StardustOnPlay(Stardust card, CardOnPlayMirrorContext context)
    {
        context.AttackRandomOpponents(context.Card.ResolveStarXValue(context.State));
    }

    public static void SweepingGazeOnPlay(SweepingGaze card, CardOnPlayMirrorContext context)
    {
        if (context.State.GetOsty(card.Owner) is { } osty && context.State.GetCreature(osty).IsAlive)
        {
            DamageCmd.Attack(card.DynamicVars.OstyDamage.BaseValue)
                .FromOsty(osty, card, context.CardPlay)
                .TargetingRandomOpponents(context.CombatState)
                .Simulate(context.Simulator);
        }
    }

    public static void SwordBoomerangOnPlay(SwordBoomerang card, CardOnPlayMirrorContext context)
    {
        context.AttackRandomOpponents(card.DynamicVars.Repeat.IntValue);
    }

    public static void VolleyOnPlay(Volley card, CardOnPlayMirrorContext context)
    {
        context.AttackRandomOpponents(context.Card.ResolveEnergyXValue(context.State));
    }
}
