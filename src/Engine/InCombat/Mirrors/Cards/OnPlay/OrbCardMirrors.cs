using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using CombatSolver.Engine.Common;
using MegaCrit.Sts2.Core.Models.Orbs;
using CombatSolver.Engine.InCombat.Simulation;
using System.Reflection;
using MegaCrit.Sts2.Core.Extensions;

namespace CombatSolver.Engine.InCombat.Mirrors.Cards.OnPlay;

internal static class OrbCardMirrors
{
    public static void BallLightningOnPlay(BallLightning card, CardOnPlayMirrorContext context)
    {
        context.AttackSingle();
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.OrbChannel<LightningOrb>(card.Owner);
    }

    public static void ChaosOnPlay(Chaos card, CardOnPlayMirrorContext context)
    {
        for (var i = 0; i < card.DynamicVars.Repeat.IntValue; i++)
        {
            OrbModel orb;
            if (BetterDefectMobileCompatibility.IsTransformed<Chaos>())
            {
                Type[] present = context.OwnerState.OrbQueue.Orbs.Select(item => item.GetType()).ToArray();
                OrbModel[] canonical =
                [
                    ModelDb.Orb<LightningOrb>(), ModelDb.Orb<FrostOrb>(),
                    ModelDb.Orb<DarkOrb>(), ModelDb.Orb<PlasmaOrb>(),
                    ModelDb.Orb<GlassOrb>(),
                ];
                OrbModel[] missing = canonical.Where(item => !present.Contains(item.GetType())).ToArray();
                orb = (missing.Length > 0 ? missing : canonical).ToList()
                    .StableShuffle(context.Rng.CombatOrbGeneration).First().ToMutable();
            }
            else
            {
                orb = OrbModel.GetRandomOrb(context.Rng.CombatOrbGeneration).ToMutable();
            }
            context.Simulator.OrbChannel(card.Owner, orb);
            if (context.Simulator.HasPendingChoice)
                return;
        }
    }

    public static void ChillOnPlay(Chill card, CardOnPlayMirrorContext context)
    {
        context.Simulator.OrbChannel<FrostOrb>(card.Owner, context.State.HittableEnemies.Count);
    }

    public static void ColdSnapOnPlay(ColdSnap card, CardOnPlayMirrorContext context)
    {
        context.AttackSingle();
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.OrbChannel<FrostOrb>(card.Owner);
        if (BetterDefectMobileCompatibility.ColdSnapTransformed && !context.Simulator.HasPendingChoice)
            context.Simulator.OrbChannel<FrostOrb>(card.Owner);
    }

    public static void ConsumingShadowOnPlay(ConsumingShadow card, CardOnPlayMirrorContext context)
    {
        context.Simulator.OrbChannel<DarkOrb>(card.Owner, card.DynamicVars.Repeat.IntValue);
        if (context.Simulator.HasPendingChoice)
            return;
        if (context.State.CombatState is not ICombatPredictionEffectSink effects)
            throw new InvalidOperationException("吞噬暗影结算缺少可写的预测状态。");
        effects.ApplyPower(
            typeof(ConsumingShadowPower),
            card.Owner.Creature,
            card.DynamicVars["ConsumingShadowPower"].IntValue,
            card.Owner.Creature);
    }

    public static void CoolheadedOnPlay(Coolheaded card, CardOnPlayMirrorContext context)
    {
        // BetterDefect v0.11.66 draws before channeling; the vanilla order is
        // reversed.  The order matters for hand-full and draw-trigger powers.
        if (BetterDefectMobileCompatibility.IsTransformed<Coolheaded>())
        {
            context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.BaseValue);
            if (context.Simulator.HasPendingChoice)
                return;
            context.Simulator.OrbChannel<FrostOrb>(card.Owner);
            return;
        }
        context.Simulator.OrbChannel<FrostOrb>(card.Owner);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.BaseValue);
    }

    public static void DarknessOnPlay(Darkness card, CardOnPlayMirrorContext context)
    {
        context.Simulator.OrbChannel<DarkOrb>(card.Owner);
        if (context.Simulator.HasPendingChoice)
            return;

        var triggerCount = card.IsUpgraded ? 2 : 1;
        var darkOrbs = context.OwnerState.OrbQueue.Orbs.OfType<DarkOrb>().ToArray();
        foreach (var darkOrb in darkOrbs)
        {
            for (var i = 0; i < triggerCount; i++)
            {
                context.Simulator.OrbPassive(darkOrb);
                if (context.Simulator.HasPendingChoice)
                    return;
            }
        }
    }

    public static void DualcastOnPlay(Dualcast card, CardOnPlayMirrorContext context)
    {
        context.Simulator.OrbEvokeNext(card.Owner, repeat: 2);
    }

    public static void FusionOnPlay(Fusion card, CardOnPlayMirrorContext context)
    {
        context.Simulator.OrbChannel<PlasmaOrb>(card.Owner);
    }

    public static void GlacierOnPlay(Glacier card, CardOnPlayMirrorContext context)
    {
        context.GainBlock(card.Owner.Creature);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.OrbChannel<FrostOrb>(card.Owner, 2);
    }

    public static void HelixDrillOnPlay(HelixDrill card, CardOnPlayMirrorContext context)
    {
        int hits = context.Card.ResolveEnergyXValue(context.State);
        if (BetterDefectMobileCompatibility.IsTransformed<HelixDrill>() && hits >= 4)
            hits *= 2;
        if (hits > 0)
            context.AttackSingle(hitCount: hits);
    }

    public static void GlassworkOnPlay(Glasswork card, CardOnPlayMirrorContext context)
    {
        context.GainBlock(card.Owner.Creature);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.OrbChannel<GlassOrb>(card.Owner);
    }

    public static void IceLanceOnPlay(IceLance card, CardOnPlayMirrorContext context)
    {
        context.AttackSingle();
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.OrbChannel<FrostOrb>(card.Owner, card.DynamicVars.Repeat.IntValue);
    }

    public static void IgnitionOnPlay(Ignition card, CardOnPlayMirrorContext context)
    {
        context.Simulator.OrbChannel<PlasmaOrb>(context.TargetPlayer);
    }

    public static void MeteorStrikeOnPlay(MeteorStrike card, CardOnPlayMirrorContext context)
    {
        context.AttackSingle();
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.OrbChannel<PlasmaOrb>(card.Owner,
            BetterDefectMobileCompatibility.IsTransformed<MeteorStrike>() ? 2 : 3);
    }

    public static void LightningRodOnPlay(LightningRod card, CardOnPlayMirrorContext context)
    {
        context.GainBlock(card.Owner.Creature);
        if (context.Simulator.HasPendingChoice)
            return;
        if (BetterDefectMobileCompatibility.IsTransformed<LightningRod>())
            context.Simulator.OrbChannel<LightningOrb>(card.Owner);
        // LightningRodPower is applied by CardEffectSpecRegistry after OnPlay.
    }

    public static void MultiCastOnPlay(MultiCast card, CardOnPlayMirrorContext context)
    {
        var repeat = context.Card.ResolveEnergyXValue(context.State) + (card.IsUpgraded ? 1 : 0);
        if (!BetterDefectMobileCompatibility.IsTransformed<MultiCast>())
        {
            context.Simulator.OrbEvokeNext(card.Owner, repeat);
            return;
        }
        for (int i = 0; i < repeat; i++)
        {
            // BetterDefect double-evokes the current rightmost orb, then
            // channels a fresh orb of the same type for the next repetition.
            OrbModel? rightmost = context.OwnerState.OrbQueue.Orbs.FirstOrDefault();
            if (rightmost is null)
                break;
            OrbModel replacement = rightmost switch
            {
                LightningOrb => ModelDb.Orb<LightningOrb>().ToMutable(),
                FrostOrb => ModelDb.Orb<FrostOrb>().ToMutable(),
                DarkOrb => ModelDb.Orb<DarkOrb>().ToMutable(),
                PlasmaOrb => ModelDb.Orb<PlasmaOrb>().ToMutable(),
                GlassOrb => ModelDb.Orb<GlassOrb>().ToMutable(),
                _ => throw new InvalidOperationException($"Cannot restore transformed MultiCast orb {rightmost.GetType().Name}."),
            };
            if (rightmost is DarkOrb dark)
            {
                FieldInfo field = typeof(DarkOrb).GetField("_evokeVal", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? throw new MissingFieldException(typeof(DarkOrb).FullName, "_evokeVal");
                field.SetValue(replacement, dark.EvokeVal);
            }
            context.Simulator.OrbEvokeNext(card.Owner, repeat: 2);
            if (context.Simulator.HasPendingChoice)
                return;
            context.Simulator.OrbChannel(card.Owner, replacement);
            if (context.Simulator.HasPendingChoice)
                return;
        }
    }

    public static void NullOnPlay(Null card, CardOnPlayMirrorContext context)
    {
        bool transformed = BetterDefectMobileCompatibility.IsTransformed<Null>();
        bool alreadyWeak = transformed && context.State.CombatState is SimulatedCombatState combat
            && combat.GetAmount<WeakPower>(context.Target) > 0;
        context.AttackSingle();
        if (context.Simulator.HasPendingChoice)
            return;
        if (transformed && context.State.CombatState is SimulatedCombatState transformedCombat)
            transformedCombat.Apply<WeakPower>(context.Target, card.DynamicVars.Weak.IntValue, card.Owner.Creature);
        context.Simulator.OrbChannel<DarkOrb>(card.Owner);
        if (alreadyWeak && !context.Simulator.HasPendingChoice)
            context.Simulator.OrbChannel<DarkOrb>(card.Owner);
    }

    public static void QuadcastOnPlay(Quadcast card, CardOnPlayMirrorContext context)
    {
        context.Simulator.OrbEvokeNext(card.Owner, repeat: card.DynamicVars.Repeat.IntValue);
    }

    public static void RainbowOnPlay(Rainbow card, CardOnPlayMirrorContext context)
    {
        context.Simulator.OrbChannel<LightningOrb>(card.Owner);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.OrbChannel<FrostOrb>(card.Owner);
        if (context.Simulator.HasPendingChoice)
            return;
        if (BetterDefectMobileCompatibility.IsTransformed<Rainbow>())
        {
            context.Simulator.OrbChannel<GlassOrb>(card.Owner);
            if (context.Simulator.HasPendingChoice)
                return;
        }
        context.Simulator.OrbChannel<DarkOrb>(card.Owner);
        if (BetterDefectMobileCompatibility.IsTransformed<Rainbow>() && !context.Simulator.HasPendingChoice)
            context.Simulator.OrbChannel<PlasmaOrb>(card.Owner);
    }

    public static void RefractOnPlay(Refract card, CardOnPlayMirrorContext context)
    {
        context.AttackSingle(hitCount: 2);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.OrbChannel<GlassOrb>(card.Owner, card.DynamicVars.Repeat.IntValue);
    }

    public static void ShadowShieldOnPlay(ShadowShield card, CardOnPlayMirrorContext context)
    {
        context.GainBlock(card.Owner.Creature);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.OrbChannel<DarkOrb>(card.Owner);
    }

    public static void ShatterOnPlay(Shatter card, CardOnPlayMirrorContext context)
    {
        context.AttackAllOpponents();
        if (context.Simulator.HasPendingChoice)
            return;

        var orbCount = context.OwnerState.OrbQueue.Orbs.Count;
        for (var i = 0; i < orbCount; i++)
        {
            context.Simulator.OrbEvokeNext(card.Owner, repeat: 2);
            if (context.Simulator.HasPendingChoice)
                return;
        }
    }

    public static void SpinnerOnPlay(Spinner card, CardOnPlayMirrorContext context)
    {
        if (card.IsUpgraded)
        {
            context.Simulator.OrbChannel<GlassOrb>(card.Owner);
        }

        // Vanilla applies SpinnerPower after optional channeling, which is not simulated here.
    }

    public static void TempestOnPlay(Tempest card, CardOnPlayMirrorContext context)
    {
        var count = context.Card.ResolveEnergyXValue(context.State) + (card.IsUpgraded ? 1 : 0);
        if (!BetterDefectMobileCompatibility.IsTransformed<Tempest>())
        {
            context.Simulator.OrbChannel<LightningOrb>(card.Owner, count);
            return;
        }
        for (int i = 0; i < count; i++)
        {
            var queue = context.OwnerState.OrbQueue;
            bool evokedLightning = queue.Capacity > 0 && queue.Orbs.Count >= queue.Capacity
                && queue.Orbs[0] is LightningOrb;
            context.Simulator.OrbChannel<LightningOrb>(card.Owner);
            if (context.Simulator.HasPendingChoice)
                return;
            if (evokedLightning)
            {
                context.Simulator.Draw(card.Owner, 1);
                if (context.Simulator.HasPendingChoice)
                    return;
            }
        }
    }

    public static void TeslaCoilOnPlay(TeslaCoil card, CardOnPlayMirrorContext context)
    {
        context.AttackSingle();
        if (context.Simulator.HasPendingChoice)
            return;

        var triggerCount = BetterDefectMobileCompatibility.HasReviewedMobileMod
            ? card.IsUpgraded && BetterDefectMobileCompatibility.IsTransformed<TeslaCoil>() ? 2 : 1
            : card.IsUpgraded ? 2 : 1;
        var lightningOrbs = context.OwnerState.OrbQueue.Orbs.OfType<LightningOrb>().ToArray();
        foreach (var lightningOrb in lightningOrbs)
        {
            for (var i = 0; i < triggerCount; i++)
            {
                context.Simulator.OrbPassive(lightningOrb, context.Target);
                if (context.Simulator.HasPendingChoice)
                    return;
            }
        }
    }

    public static void VoltaicOnPlay(Voltaic card, CardOnPlayMirrorContext context)
    {
        var count = CombatManager.Instance.History.Entries
            .OfType<OrbChanneledEntry>()
            .Count(entry => entry.Actor.Player == card.Owner && entry.Orb is LightningOrb);

        count += context.Simulator.History
            .OfType<CombatPredictionOrbChanneledEntry>()
            .Count(entry => entry.Orb.Owner == card.Owner && entry.Orb is LightningOrb);

        context.Simulator.OrbChannel<LightningOrb>(card.Owner, count);
    }

    public static void ZapOnPlay(Zap card, CardOnPlayMirrorContext context)
    {
        context.Simulator.OrbChannel<LightningOrb>(card.Owner);
    }
}
