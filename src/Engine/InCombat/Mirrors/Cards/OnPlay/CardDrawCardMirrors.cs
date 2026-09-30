using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.ValueProps;
using CombatSolver.Engine.Common;
using CombatSolver.Engine.InCombat.Simulation;

namespace CombatSolver.Engine.InCombat.Mirrors.Cards.OnPlay;

internal static class CardDrawCardMirrors
{
    public static void SynthesisOnPlay(Synthesis card, CardOnPlayMirrorContext context)
    {
        context.AttackSingle();
        if (context.Simulator.HasPendingChoice || !BetterDefectMobileCompatibility.IsTransformed<Synthesis>())
            return;
        var powers = context.OwnerState.DrawPile.Cards
            .Where(candidate => candidate.Preview.Type == CardType.Power).ToList();
        if (card.IsUpgraded && powers.Count > 0)
            return; // Search's native grid choice moves the selected power first.
        if (powers.Count > 0)
        {
            var selected = powers.StableShuffle(context.Rng.Shuffle).First();
            context.Simulator.AddToPile(selected, PileType.Hand);
            if (context.Simulator.HasPendingChoice)
                return;
        }
        if (context.CombatState is not SimulatedCombatState combat)
            throw new InvalidOperationException("改造人工合成缺少分支战斗状态。");
        combat.Apply<FreePowerPower>(card.Owner.Creature, 1, card.Owner.Creature);
    }

    public static void AdrenalineOnPlay(Adrenaline card, CardOnPlayMirrorContext context)
    {
        context.Simulator.GainEnergy(card.Owner, card.DynamicVars.Energy.IntValue);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.BaseValue);
    }

    public static void OfferingOnPlay(Offering card, CardOnPlayMirrorContext context)
    {
        context.Simulator.Damage([card.Owner.Creature], card.DynamicVars.HpLoss.BaseValue,
            ValueProp.Unblockable | ValueProp.Unpowered | ValueProp.Move,
            card.Owner.Creature, context.Card, context.CardPlay);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.GainEnergy(card.Owner, card.DynamicVars.Energy.IntValue);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.BaseValue);
    }

    public static void NeurosurgeOnPlay(Neurosurge card, CardOnPlayMirrorContext context)
    {
        context.Simulator.GainEnergy(card.Owner, card.DynamicVars.Energy.IntValue);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.BaseValue);
        if (context.Simulator.HasPendingChoice)
            return;
        SimulatedCombatState combat = context.State.CombatState as SimulatedCombatState
            ?? throw new InvalidOperationException("Neurosurge requires simulated combat state.");
        combat.Apply<NeurosurgePower>(card.Owner.Creature, card.DynamicVars["NeurosurgePower"].IntValue,
            card.Owner.Creature);
    }

    public static void SpoilsOfBattleOnPlay(SpoilsOfBattle card, CardOnPlayMirrorContext context)
    {
        PersistentPowerSupport.Forge(context.Simulator, card.Owner, card.DynamicVars.Forge.IntValue);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.BaseValue);
    }

    public static void CompileDriverOnPlay(CompileDriver card, CardOnPlayMirrorContext context)
    {
        context.AttackSingle();
        if (context.Simulator.HasPendingChoice)
            return;
        var drawCount = context.OwnerState.OrbQueue.Orbs.Select(orb => orb.Id).Distinct().Count();
        context.Simulator.Draw(card.Owner, drawCount);
    }

    public static void CalculatedGambleOnPlay(CalculatedGamble card, CardOnPlayMirrorContext context)
    {
        var cards = context.OwnerState.Hand.Cards.ToArray();
        context.Simulator.DiscardAndDraw(cards, cards.Length);
    }

    public static void ConstellationOnPlay(Constellation card, CardOnPlayMirrorContext context)
    {
        var player = context.TargetPlayer;
        context.Simulator.Draw(player, card.DynamicVars.Cards.BaseValue);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.GainEnergy(player, card.DynamicVars.Energy.IntValue);
        if (context.Simulator.HasPendingChoice)
            return;
        context.GainBlock(player.Creature);
    }

    public static void EscapePlanOnPlay(EscapePlan card, CardOnPlayMirrorContext context)
    {
        var drawnCards = context.Simulator.Draw(card.Owner, 1);
        if (context.Simulator.HasPendingChoice)
            return;
        if (drawnCards is [{ Preview.Type: CardType.Skill }])
        {
            context.GainBlock(card.Owner.Creature);
        }
    }

    public static void ExpertiseOnPlay(Expertise card, CardOnPlayMirrorContext context)
    {
        var drawnCards = context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.IntValue);
        if (context.Simulator.HasPendingChoice)
            return;
        foreach (var drawnCard in drawnCards)
        {
            drawnCard.MutablePreview.GiveSingleTurnRetain();
        }
    }

    public static void FetchOnPlay(Fetch card, CardOnPlayMirrorContext context)
    {
        if (context.State.GetOsty(card.Owner) is not { } osty || context.State.GetCreature(osty).IsDead)
        {
            return;
        }

        DamageCmd.Attack(card.DynamicVars.OstyDamage.BaseValue)
            .FromOsty(osty, card, context.CardPlay)
            .Targeting(context.Target)
            .Simulate(context.Simulator);
        if (context.Simulator.HasPendingChoice)
            return;

        SimulatedCombatState combat = context.Simulator.State.CombatState as SimulatedCombatState
            ?? throw new InvalidOperationException("Fetch requires simulated combat state.");
        if (!combat.WasFetchPlayedThisTurn(context.Card))
        {
            context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.BaseValue);
        }
    }

    public static void FtlOnPlay(Ftl card, CardOnPlayMirrorContext context)
    {
        context.AttackSingle();
        if (context.Simulator.HasPendingChoice)
            return;

        SimulatedCombatState combat = context.Simulator.State.CombatState as SimulatedCombatState
            ?? throw new InvalidOperationException("FTL requires simulated combat state.");
        if (combat.GetCardsPlayedThisTurn(card.Owner.Creature) < card.DynamicVars[Ftl._playMaxKey].IntValue)
        {
            context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.BaseValue);
        }
        else if (BetterDefectMobileCompatibility.IsTransformed<Ftl>())
        {
            combat.ApplyPower(BetterDefectMobileCompatibility.ReviewedPowerType(
                    "BetterDefect.Cards.BdLockOnPower"),
                context.Target, card.IsUpgraded ? 2 : 1, card.Owner.Creature);
        }
    }

    public static void HuddleUpOnPlay(HuddleUp card, CardOnPlayMirrorContext context)
    {
        var allies = context.State.GetTeammatesOf(card.Owner.Creature)
            .Where(creature => creature.IsPlayer && context.State.GetCreature(creature).IsAlive);
        foreach (var ally in allies)
        {
            context.Simulator.Draw(ally.Player!, card.DynamicVars.Cards.BaseValue);
            if (context.Simulator.HasPendingChoice)
                return;
        }
    }

    public static void ImpatienceOnPlay(Impatience card, CardOnPlayMirrorContext context)
    {
        if (context.OwnerState.Hand.Cards.All(predicted => predicted.Preview.Type != CardType.Attack))
        {
            context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.BaseValue);
        }
    }

    public static void PillageOnPlay(Pillage card, CardOnPlayMirrorContext context)
    {
        context.AttackSingle();
        if (context.Simulator.HasPendingChoice)
            return;

        while (true)
        {
            var drawnCards = context.Simulator.Draw(card.Owner, 1);
            if (context.Simulator.HasPendingChoice)
                return;
            if (drawnCards is not [{ Preview.Type: CardType.Attack }] ||
                context.OwnerState.Hand.Cards.Count >= context.Simulator.GetMaxHandSize(card.Owner))
            {
                break;
            }
        }
    }

    public static void RebootOnPlay(Reboot card, CardOnPlayMirrorContext context)
    {
        context.Simulator.MoveHandToDrawPile(card.Owner);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.Shuffle(card.Owner);
        if (context.Simulator.HasPendingChoice)
            return;
        context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.BaseValue);
    }

    public static void RestlessnessOnPlay(Restlessness card, CardOnPlayMirrorContext context)
    {
        if (context.OwnerState.Hand.IsEmpty)
        {
            context.Simulator.Draw(card.Owner, card.DynamicVars.Cards.IntValue);
            if (context.Simulator.HasPendingChoice)
                return;
            context.Simulator.GainEnergy(card.Owner, card.DynamicVars.Energy.IntValue);
        }
    }

    public static void ScrapeOnPlay(Scrape card, CardOnPlayMirrorContext context)
    {
        context.AttackSingle();
        if (context.Simulator.HasPendingChoice)
            return;

        IReadOnlyList<PredictedCard> drawnCards = context.Simulator.Draw(card.Owner,
            card.DynamicVars.Cards.IntValue);
        if (context.Simulator.HasPendingChoice)
        {
            if (drawnCards is not List<PredictedCard> pendingDrawn)
                throw new InvalidOperationException("刮削抽牌挂起后缺少可继续的选牌列表。");
            context.Simulator.AppendExecutionContinuation(new ScrapePostDrawFrame(context.Card, pendingDrawn));
            return;
        }
        FinishScrape(context.Simulator, context.Card, drawnCards);
    }

    private static bool FinishScrape(CombatPredictionSimulator simulator, PredictedCard source,
        IReadOnlyList<PredictedCard> drawn)
    {
        bool transformed = BetterDefectMobileCompatibility.IsTransformed<Scrape>();
        PredictedCard[] discarded = drawn.Where(candidate =>
            candidate.Preview.EnergyCost.CostsX || candidate.Preview.HasStarCostX
            || candidate.Preview.CurrentStarCost > 0
            || (transformed
                ? candidate.GetEnergyCostValueWithModifiers(simulator)
                : candidate.Preview.EnergyCost.GetWithModifiers(CostModifiers.Local)) != 0).ToArray();
        int retained = drawn.Count - discarded.Length;
        simulator.Discard(discarded);
        if (simulator.HasPendingChoice)
        {
            simulator.AppendExecutionContinuation(new ScrapeAfterDiscardFrame(source, retained));
            return false;
        }
        return FinishScrapeStrength(simulator, source, retained);
    }

    private static bool FinishScrapeStrength(CombatPredictionSimulator simulator, PredictedCard source, int retained)
    {
        if (retained > 0 && BetterDefectMobileCompatibility.IsTransformed<Scrape>())
        {
            if (simulator.State.CombatState is not SimulatedCombatState combat)
                throw new InvalidOperationException("刮削缺少分支战斗状态。");
            BetterDefectMobileCompatibility.ApplyReviewedTemporaryPower(combat,
                "BetterDefect.Cards.BdScrapeTemporaryStrengthPower", source.Preview,
                retained, focus: false, positive: true);
        }
        return !simulator.HasPendingChoice;
    }

    private sealed record ScrapePostDrawFrame(PredictedCard Source, List<PredictedCard> Drawn)
        : ICombatPredictionExecutionFrame
    {
        public void PrepareFork(PredictionForkContext context)
            => CombatPredictionSimulator.ForkExecutionCardList(Drawn, context);
        public ICombatPredictionExecutionFrame Fork(PredictionForkContext context)
            => this with { Source = context.RequireRemap(Source), Drawn = context.RequireRemap(Drawn) };
        public bool Resume(CombatPredictionSimulator simulator)
            => FinishScrape(simulator, Source, Drawn);
    }

    private sealed record ScrapeAfterDiscardFrame(PredictedCard Source, int Retained)
        : ICombatPredictionExecutionFrame
    {
        public ICombatPredictionExecutionFrame Fork(PredictionForkContext context)
            => this with { Source = context.RequireRemap(Source) };
        public bool Resume(CombatPredictionSimulator simulator)
            => FinishScrapeStrength(simulator, Source, Retained);
    }

    public static void ScrawlOnPlay(Scrawl card, CardOnPlayMirrorContext context)
    {
        int count = context.Simulator.GetMaxHandSize(card.Owner) - context.OwnerState.Hand.Cards.Count;
        context.Simulator.Draw(card.Owner, count);
    }

}
