using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Modding;
using CombatSolver.Engine.Common;
using CombatSolver.Engine.Common.Mirrors;
using CombatSolver.Engine.InCombat.Mirrors.Cards.OnPlay;
using CombatSolver.Engine.InCombat.Simulation;

namespace CombatSolver;

/// <summary>
/// BetterDefect's encyclopedia choices are global, but most do not affect a
/// particular fight. Admit safe vanilla plays independently of the number of
/// saved transformations; do not pretend an unmirrored transformed play uses
/// vanilla OnPlay merely because the card is already in the deck.
/// </summary>
internal static class BetterDefectMobileCompatibility
{
    private const string ModId = "BetterDefect";
    private const string ReviewedVersion = "0.11.66";
    // Runtime registration is a stronger compatibility signal than MVID:
    // reproducible builds of the same source can have different module IDs.
    // BetterDefect-owned models cannot be referenced statically by a standalone
    // CombatSolver build. Admit only exact reviewed names from the reviewed
    // assembly, and provide an explicit mirror for every admitted type.
    private static readonly HashSet<string> MirroredCustomCards = new(StringComparer.Ordinal)
    {
        "BetterDefect.Cards.BdReinforcedBody",
        "BetterDefect.Cards.BdAutoShields",
        "BetterDefect.Cards.BdStreamline",
        "BetterDefect.Cards.BdConsume",
        "BetterDefect.Cards.BdRecursion",
        "BetterDefect.Cards.BdRecycle",
        "BetterDefect.Cards.BdStaticDischarge",
        "BetterDefect.Cards.BdHeatsinks",
        "BetterDefect.Cards.BdSeek",
    };
    private static readonly HashSet<string> MirroredTemporaryPowers = new(StringComparer.Ordinal)
    {
        "BetterDefect.Cards.BdScrapeTemporaryStrengthPower",
        "BetterDefect.Cards.BdHyperbeamTemporaryFocusDownPower",
        "BetterDefect.Cards.BdBarrageTemporaryFocusPower",
    };
    private static readonly HashSet<string> MirroredOtherPowers = new(StringComparer.Ordinal)
    {
        "BetterDefect.Cards.BdLockOnPower",
        "BetterDefect.Cards.BdSpinnerNoDecayPower",
        "BetterDefect.Cards.BdStaticDischargePower",
        "BetterDefect.Cards.BdStormChargePower",
        "BetterDefect.Cards.BdStaticDischargeChargePower",
        "BetterDefect.Cards.BdHeatsinksPower",
    };
    // These exact v0.11.66 transformations have explicit branch mirrors below,
    // or only change model data already read from the captured card. Do not add
    // a type merely because its vanilla OnPlay happens to look similar.
    private static readonly HashSet<Type> MirroredTransformations =
    [
        typeof(BiasedCognition), typeof(MegaCrit.Sts2.Core.Models.Cards.Buffer),
        typeof(ChargeBattery), typeof(Chaos), typeof(Claw), typeof(ColdSnap), typeof(Compact),
        typeof(ConsumingShadow), typeof(Coolant), typeof(Coolheaded),
        typeof(Defragment), typeof(DoubleEnergy), typeof(Feral), typeof(FightThrough),
        typeof(FlakCannon),
        typeof(FocusedStrike),
        typeof(Fusion), typeof(GeneticAlgorithm), typeof(GoForTheEyes),
        typeof(GunkUp), typeof(HelixDrill), typeof(Hotfix), typeof(Leap),
        typeof(LightningRod), typeof(Loop),
        typeof(MeteorStrike), typeof(MultiCast), typeof(Null), typeof(Rainbow),
        typeof(RocketPunch), typeof(Shatter),
        typeof(Sunder), typeof(SweepingBeam), typeof(Synchronize), typeof(Tempest),
        typeof(TeslaCoil), typeof(TrashToTreasure), typeof(Voltaic),
        typeof(AllForOne), typeof(Rebound),
        typeof(Stack),
        typeof(Scrape), typeof(Barrage), typeof(Hyperbeam),
        typeof(Ftl),
        typeof(EchoForm), typeof(Uproar), typeof(Spinner), typeof(Subroutine),
        typeof(Smokestack),
        typeof(WhiteNoise),
        typeof(CreativeAi), typeof(HelloWorld),
        typeof(Storm),
        typeof(Iteration),
    ];
    private static readonly HashSet<Type> PotentialCardGenerators =
    [
        typeof(Abundance), typeof(BundleOfJoy), typeof(Distraction), typeof(Discovery),
        typeof(InfernalBlade), typeof(JackOfAllTrades), typeof(Jackpot), typeof(Largesse),
        typeof(Havoc), typeof(Cascade), typeof(MadScience), typeof(ManifestAuthority),
        typeof(Metamorphosis), typeof(Quasar),
        typeof(Splash), typeof(Stoke), typeof(WhiteNoise), typeof(HelloWorld), typeof(CreativeAi)
    ];

    // These vanilla powers have conditional BetterDefect behavior even if
    // their source card was played manually before the search was requested.
    private static readonly Dictionary<string, string> ModifiedPowerSources = new(StringComparer.Ordinal)
    {
        ["HailstormPower"] = "Hailstorm",
    };
    private sealed record HiddenPowerSnapshot(
        (int Round, bool Drew)? DrawState,
        int? SmokestackStacks,
        (int EligibleSerial, int Bonus)[]? StormBatches);

    // Native patch tables are mutable. Freeze them on the main-thread root
    // capture; worker branches only read these immutable values or their own
    // newly created power instance. Weak keys do not retain finished combats.
    private static readonly ConditionalWeakTable<PowerModel, HiddenPowerSnapshot> RootPowerSnapshots = new();

    // Written at main-thread root capture, read by branch-local card mirrors.
    private static Assembly? _activeAssembly;
    private static HashSet<Type> _transformedTypes = [];
    private static HashSet<Type> _unmirroredTransformedTypes = [];
    private static int _transformedTypesInCombat;

    internal static bool ColdSnapTransformed =>
        IsTransformed<ColdSnap>();

    internal static bool HasReviewedMobileMod =>
        MobilePortPolicy.IsMobile && Volatile.Read(ref _activeAssembly) is not null;

    // A held card-generating potion is harmless until used. Keep it out of
    // prediction rather than aborting turn setup merely because it occupies a slot.
    internal static bool MayGenerateUnmirroredCard(PotionModel potion)
        => potion is AttackPotion or SkillPotion or PowerPotion or OrobicAcid
            or ColorlessPotion or CosmicConcoction;

    internal static bool IsTransformed<TCard>() where TCard : CardModel
        => MobilePortPolicy.IsMobile && Volatile.Read(ref _activeAssembly) is not null
            && Volatile.Read(ref _transformedTypes).Contains(typeof(TCard));

    internal static bool IsTransformedCustomCard(string fullName)
    {
        Assembly? assembly = Volatile.Read(ref _activeAssembly);
        Type? type = assembly?.GetType(fullName, throwOnError: false);
        return type is not null && Volatile.Read(ref _transformedTypes).Contains(type);
    }

    internal static (int Round, bool Drew) ReadOncePerRoundDrawState(
        PowerModel power, string patchTypeName)
    {
        if (RootPowerSnapshots.TryGetValue(power, out var snapshot)
            && snapshot.DrawState is { } captured)
            return captured;
        return ReadNativeOncePerRoundDrawState(power, patchTypeName);
    }

    internal static (int Round, bool Drew) ReadNativeOncePerRoundDrawState(
        PowerModel power, string patchTypeName)
    {
        Type patch = Volatile.Read(ref _activeAssembly)?.GetType(patchTypeName, true)
            ?? throw Unsupported($"找不到改造补丁 {patchTypeName}");
        object table = patch.GetField("States", BindingFlags.Static | BindingFlags.NonPublic)
            ?.GetValue(null) ?? throw Unsupported($"找不到改造状态 {patchTypeName}");
        MethodInfo method = table.GetType().GetMethod("TryGetValue")
            ?? throw Unsupported($"无法读取改造状态 {patchTypeName}");
        object?[] args = [power, null];
        if (method.Invoke(table, args) is not true || args[1] is not { } state)
            return (int.MinValue, false);
        Type type = state.GetType();
        int round = type.GetField("Round", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(state) is int value ? value : int.MinValue;
        bool drew = type.GetField("Drew", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.GetValue(state) is true;
        return (round, drew);
    }

    internal static int ReadSmokestackStackCount(SmokestackPower power)
    {
        if (RootPowerSnapshots.TryGetValue(power, out var snapshot)
            && snapshot.SmokestackStacks is { } captured)
            return captured;
        return ReadNativeSmokestackStackCount(power);
    }

    internal static int ReadNativeSmokestackStackCount(SmokestackPower power)
    {
        Type patch = Volatile.Read(ref _activeAssembly)?.GetType(
            "BetterDefect.BdCustomSmokestackPowerPatch", true)
            ?? throw Unsupported("找不到烟囱改造补丁");
        MethodInfo method = patch.GetMethod("GetStackCount", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw Unsupported("找不到烟囱叠加计数");
        return method.Invoke(null, [power]) is int count && count > 0 ? count : 1;
    }

    internal static List<(int EligibleSerial, int Bonus)> ReadStormChargeBatches(PowerModel power)
    {
        if (RootPowerSnapshots.TryGetValue(power, out var snapshot)
            && snapshot.StormBatches is { } captured)
            return [.. captured];
        return ReadNativeStormChargeBatches(power);
    }

    internal static List<(int EligibleSerial, int Bonus)> ReadNativeStormChargeBatches(PowerModel power)
    {
        object? batches = power.GetType().GetField("_batches",
            BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(power);
        if (batches is not System.Collections.IEnumerable sequence)
            throw Unsupported("找不到雷暴的已蓄积伤害记录");
        List<(int, int)> result = [];
        foreach (object item in sequence)
        {
            Type type = item.GetType();
            int serial = type.GetProperty("EligibleFromPlaySerial")?.GetValue(item) is int s
                ? s : throw Unsupported("雷暴蓄积回合号格式不匹配");
            int bonus = type.GetProperty("Bonus")?.GetValue(item) is decimal b
                ? (int)b : throw Unsupported("雷暴蓄积伤害格式不匹配");
            result.Add((serial, bonus));
        }
        return result;
    }

    internal static bool CanSolverPlay(CardModel card)
        => !MobilePortPolicy.IsMobile || Volatile.Read(ref _activeAssembly) is null
            || !Volatile.Read(ref _unmirroredTransformedTypes).Contains(card.GetType());

    internal static int TransformedTypesInCombat =>
        MobilePortPolicy.IsMobile ? Volatile.Read(ref _transformedTypesInCombat) : 0;

    internal static void Validate(CombatState combat)
    {
        if (!MobilePortPolicy.IsMobile)
            return;

        Mod? mod = ModManager.GetLoadedMods().FirstOrDefault(item =>
            string.Equals(item.manifest?.id, ModId, StringComparison.OrdinalIgnoreCase));
        if (mod is null)
        {
            Volatile.Write(ref _transformedTypes, []);
            Volatile.Write(ref _transformedTypesInCombat, 0);
            Volatile.Write(ref _unmirroredTransformedTypes, []);
            Volatile.Write(ref _activeAssembly, null);
            return;
        }

        if (!string.Equals(mod.manifest?.version?.ToString(), ReviewedVersion, StringComparison.Ordinal))
            Reject("当前 BetterDefect 版本尚未审阅");

        Assembly assembly = mod.assemblies.FirstOrDefault(item =>
            item.GetType("BetterDefect.BdCardUpgradeState", false) is not null)
            ?? throw Unsupported("找不到改造状态接口");
        Type? listenerType = assembly.GetType("BetterDefect.BdRitsuCardOnPlayListener", false);
        PropertyInfo? readyProperty = listenerType?.GetProperty(
            "IsRegistered", BindingFlags.Static | BindingFlags.NonPublic);
        if (readyProperty?.GetValue(null) is not true)
            Reject("BetterDefect 的手机 v0.111.0 出牌钩子未注册");
        Type stateType = assembly.GetType("BetterDefect.BdCardUpgradeState", true)!;
        MethodInfo countMethod = stateType.GetMethod("GetVersionUpgradeCount", BindingFlags.Public | BindingFlags.Static)
            ?? throw Unsupported("找不到改造点数接口");
        MethodInfo enabledMethod = stateType.GetMethod("IsCardVersionUpgraded", BindingFlags.Public | BindingFlags.Static)
            ?? throw Unsupported("找不到单卡改造接口");

        int count;
        HashSet<Type> transformed = [];
        try
        {
            count = (int)(countMethod.Invoke(null, null) ?? -1);
            foreach (CardModel card in ModelDb.AllCards)
                if (enabledMethod.Invoke(null, [card]) is true)
                    transformed.Add(card.GetType());
        }
        catch (Exception error)
        {
            throw Unsupported($"读取改造状态失败：{error.GetType().Name}");
        }

        HashSet<Type> transformedInCombat = [];
        foreach (var player in combat.Players)
        {
            foreach (CardModel card in player.Deck.Cards)
            {
                ValidateCard(card, assembly, transformed);
                if (transformed.Contains(card.GetType()) && !MirroredTransformations.Contains(card.GetType()))
                    transformedInCombat.Add(card.GetType());
            }
            foreach (CardModel card in player.PlayerCombatState?.AllCards ?? [])
            {
                ValidateCard(card, assembly, transformed);
                if (transformed.Contains(card.GetType()) && !MirroredTransformations.Contains(card.GetType()))
                    transformedInCombat.Add(card.GetType());
            }
            foreach (var orb in player.PlayerCombatState?.OrbQueue.Orbs ?? [])
                if (orb.GetType().Assembly == assembly)
                    Reject($"充能球 {orb.GetType().Name} 尚未适配");
        }
        foreach (var power in combat.Creatures.SelectMany(creature => creature.Powers))
        {
            if (power.GetType().Assembly == assembly && !IsMirroredPower(power))
                Reject($"能力 {power.GetType().Name} 尚未适配");
            if (power.GetType().Name is "CreativeAiPower" or "HelloWorldPower"
                && !transformed.Any(type => type.Name ==
                    (power.GetType().Name == "CreativeAiPower" ? "CreativeAi" : "HelloWorld")))
                Reject($"能力 {power.GetType().Name} 可能生成尚未适配的卡牌");
            if (ModifiedPowerSources.TryGetValue(power.GetType().Name, out string? source)
                && transformed.Any(type => type.Name == source))
                Reject($"已生效的改造能力 {power.GetType().Name} 尚未适配");
        }

        HashSet<Type> unmirrored = [.. transformed.Where(type =>
            !MirroredTransformations.Contains(type) && !MirroredCustomCards.Contains(type.FullName ?? ""))];
        Volatile.Write(ref _transformedTypes, transformed);
        Volatile.Write(ref _unmirroredTransformedTypes, unmirrored);
        Volatile.Write(ref _transformedTypesInCombat, transformedInCombat.Count);
        Volatile.Write(ref _activeAssembly, assembly);
        foreach (PowerModel power in combat.Creatures.SelectMany(creature => creature.Powers))
        {
            (int Round, bool Drew)? drawState = power switch
            {
                SubroutinePower when transformed.Contains(typeof(Subroutine)) =>
                    ReadNativeOncePerRoundDrawState(power, "BetterDefect.BdCustomSubroutinePowerPatch"),
                SmokestackPower when transformed.Contains(typeof(Smokestack)) =>
                    ReadNativeOncePerRoundDrawState(power, "BetterDefect.BdCustomSmokestackPowerPatch"),
                _ => null,
            };
            int? stacks = power is SmokestackPower smokestack
                && transformed.Contains(typeof(Smokestack))
                    ? ReadNativeSmokestackStackCount(smokestack) : null;
            (int EligibleSerial, int Bonus)[]? batches =
                power.GetType().FullName == "BetterDefect.Cards.BdStaticDischargeChargePower"
                    ? [.. ReadNativeStormChargeBatches(power)] : null;
            if (drawState is null && stacks is null && batches is null)
                continue;
            RootPowerSnapshots.Remove(power);
            RootPowerSnapshots.Add(power, new HiddenPowerSnapshot(drawState, stacks, batches));
        }
        Entry.Logger.Info($"[CombatSolver/Mobile] BetterDefect reviewed capture: enabled={count}, " +
            $"mirroredTypes={transformed.Count - unmirrored.Count}, unmirroredTypes={unmirrored.Count}, " +
            $"unmirroredInCombat={transformedInCombat.Count}.");
    }

    internal static void ValidateCardOnPlay(CardModel card)
    {
        if (!MobilePortPolicy.IsMobile)
            return;
        Assembly? assembly = Volatile.Read(ref _activeAssembly);
        if (assembly is not null)
        {
            ValidateCard(card, assembly, Volatile.Read(ref _transformedTypes));
            if (Volatile.Read(ref _unmirroredTransformedTypes).Contains(card.GetType()))
                Reject($"改造牌 {card.GetType().Name} 尚未建立预测镜像");
        }
    }

    internal static bool CanMirrorCustomCard(CardModel card)
        => MobilePortPolicy.IsMobile && Volatile.Read(ref _activeAssembly) == card.GetType().Assembly
            && MirroredCustomCards.Contains(card.GetType().FullName ?? "");

    internal static bool IsMirroredRecycle(CardModel card)
        => CanMirrorCustomCard(card)
            && card.GetType().FullName == "BetterDefect.Cards.BdRecycle";

    internal static bool IsMirroredSeek(CardModel card)
        => CanMirrorCustomCard(card)
            && card.GetType().FullName == "BetterDefect.Cards.BdSeek";

    internal static bool IsMirroredTemporaryPower(PowerModel power)
        => MirroredTemporaryPowers.Contains(power.GetType().FullName ?? "") && IsMirroredPower(power);

    internal static bool IsMirroredPower(PowerModel power)
    {
        Type type = power.GetType();
        string name = type.FullName ?? "";
        if (!MirroredTemporaryPowers.Contains(name) && !MirroredOtherPowers.Contains(name))
            return false;
        Assembly? active = Volatile.Read(ref _activeAssembly);
        if (active is not null)
            return type.Assembly == active;
        return ModManager.GetLoadedMods().Any(mod =>
            string.Equals(mod.manifest?.id, ModId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(mod.manifest?.version?.ToString(), ReviewedVersion, StringComparison.Ordinal)
            && mod.assemblies.Contains(type.Assembly));
    }

    internal static Type ReviewedPowerType(string fullName)
    {
        if (!MirroredTemporaryPowers.Contains(fullName) && !MirroredOtherPowers.Contains(fullName))
            throw Unsupported($"能力 {fullName} 未登记");
        return Volatile.Read(ref _activeAssembly)?.GetType(fullName, throwOnError: true)
            ?? throw Unsupported("未捕获 BetterDefect 能力程序集");
    }

    internal static void ApplyReviewedTemporaryPower(
        SimulatedCombatState combat, string powerName, CardModel card,
        int amount, bool focus, bool positive)
    {
        Type type = ReviewedPowerType(powerName);
        var owner = card.Owner.Creature;
        combat.ApplyReviewedTemporaryPower(type, owner, amount, owner, focus, positive);
    }

    internal static bool TryInvokeCustomCard(
        CombatPredictionSimulator simulator, PredictedCard card, CardPlay cardPlay,
        out MirrorDispatchResult result)
    {
        result = default;
        if (!CanMirrorCustomCard(card.Preview))
            return false;
        switch (card.Preview.GetType().FullName)
        {
            case "BetterDefect.Cards.BdHeatsinks":
            {
                if (simulator.State.CombatState is not SimulatedCombatState combat)
                    throw Unsupported("散热器缺少分支战斗状态");
                var owner = card.Preview.Owner.Creature;
                combat.ApplyPower(ReviewedPowerType("BetterDefect.Cards.BdHeatsinksPower"),
                    owner, checked((int)card.MutablePreview.DynamicVars["Draw"].BaseValue), owner);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdRecycle":
                // The hand selection and post-selection energy gain are owned by
                // CardChoiceSupport/CardChoiceResolution, not by this OnPlay.
                result = new(MirrorDispatchKind.Handled);
                return true;
            case "BetterDefect.Cards.BdSeek":
                // Its draw-pile selection is resolved through CardChoiceSupport.
                // OnPlay itself has no effect before the selected cards move to hand.
                result = new(MirrorDispatchKind.Handled);
                return true;
            case "BetterDefect.Cards.BdStaticDischarge":
            {
                if (simulator.State.CombatState is not SimulatedCombatState combat)
                    throw Unsupported("静电释放缺少分支战斗状态");
                var owner = card.Preview.Owner.Creature;
                combat.ApplyPower(ReviewedPowerType("BetterDefect.Cards.BdStaticDischargePower"),
                    owner, card.MutablePreview.DynamicVars["Amount"].IntValue, owner);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdRecursion":
            {
                var owner = card.Preview.Owner;
                var queue = simulator.State.GetPlayerCombatState(owner).OrbQueue;
                var orb = IsTransformed(card.Preview)
                    ? queue.Orbs.LastOrDefault() : queue.Orbs.FirstOrDefault();
                if (orb is not null)
                {
                    // The replacement keeps the accumulated Dark evoke value.
                    var replacement = orb switch
                    {
                        LightningOrb => ModelDb.Orb<LightningOrb>().ToMutable(),
                        FrostOrb => ModelDb.Orb<FrostOrb>().ToMutable(),
                        DarkOrb => ModelDb.Orb<DarkOrb>().ToMutable(),
                        PlasmaOrb => ModelDb.Orb<PlasmaOrb>().ToMutable(),
                        GlassOrb => ModelDb.Orb<GlassOrb>().ToMutable(),
                        _ => throw Unsupported($"递归未适配充能球 {orb.GetType().Name}"),
                    };
                    if (orb is DarkOrb dark)
                    {
                        FieldInfo valueField = typeof(DarkOrb).GetField("_evokeVal",
                            BindingFlags.Instance | BindingFlags.NonPublic)
                            ?? throw Unsupported("递归找不到黑暗球数值字段");
                        valueField.SetValue(replacement, dark.EvokeVal);
                    }
                    if (IsTransformed(card.Preview))
                    {
                        simulator.OrbEvoke(owner, orb, dequeue: false);
                        if (!simulator.HasPendingChoice)
                            simulator.OrbEvoke(owner, orb);
                    }
                    else
                        simulator.OrbEvoke(owner, orb);
                    if (!simulator.HasPendingChoice)
                        simulator.OrbChannel(owner, replacement);
                }
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdConsume":
            {
                if (simulator.State.CombatState is not SimulatedCombatState combat)
                    throw Unsupported("耗尽缺少分支战斗状态");
                var owner = card.Preview.Owner;
                combat.Apply<FocusPower>(owner.Creature,
                    card.MutablePreview.DynamicVars["Focus"].IntValue, owner.Creature);
                simulator.State.GetPlayerCombatState(owner).OrbQueue.RemoveCapacity(1);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdAutoShields":
            {
                if (IsTransformed(card.Preview))
                    simulator.OrbChannel<FrostOrb>(card.Preview.Owner);
                if (!simulator.HasPendingChoice &&
                    simulator.State.GetCreature(card.Preview.Owner.Creature).Block <= 0)
                    simulator.GainBlock(card.Preview.Owner.Creature,
                        card.MutablePreview.DynamicVars.Block, card, cardPlay);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdStreamline":
            {
                if (cardPlay.Target is not null)
                    DamageCmd.Attack(card.MutablePreview.DynamicVars.Damage.BaseValue)
                        .FromCard(card.MutablePreview, cardPlay)
                        .Targeting(cardPlay.Target)
                        .Simulate(simulator);
                if (!simulator.HasPendingChoice)
                {
                    if (IsTransformed(card.Preview))
                    {
                        foreach (PredictedCard candidate in simulator.State
                                     .GetPlayerCombatState(card.Preview.Owner).AllCards)
                            if (candidate.Preview.GetType() == card.Preview.GetType())
                                candidate.MutablePreview.EnergyCost.AddThisCombat(-1, reduceOnly: true);
                    }
                    else
                        card.MutablePreview.EnergyCost.AddThisCombat(-1, reduceOnly: true);
                }
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdReinforcedBody":
            {
                int hits = card.ResolveEnergyXValue(simulator.State);
                if (IsTransformed(card.Preview) && hits >= 4)
                    hits = checked(hits * 2);
                for (int index = 0; index < hits && !simulator.HasPendingChoice; index++)
                    simulator.GainBlock(card.Preview.Owner.Creature,
                        card.MutablePreview.DynamicVars.Block, card, cardPlay);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            default:
                throw Unsupported($"卡牌 {card.Preview.GetType().Name} 缺少预测镜像");
        }
    }

    private static bool IsTransformed(CardModel card)
        => MobilePortPolicy.IsMobile && Volatile.Read(ref _activeAssembly) is not null
            && Volatile.Read(ref _transformedTypes).Contains(card.GetType());

    private static void ValidateCard(CardModel card, Assembly assembly, IReadOnlySet<Type> transformed)
    {
        Type type = card.GetType();
        if (type.Assembly == assembly && !MirroredCustomCards.Contains(type.FullName ?? ""))
            Reject($"卡牌 {type.Name} 的效果尚未适配");
        if (PotentialCardGenerators.Contains(type)
            && !(type == typeof(WhiteNoise) && transformed.Contains(type))
            && !(type == typeof(CreativeAi) && transformed.Contains(type))
            && !(type == typeof(HelloWorld) && transformed.Contains(type)))
            Reject($"卡牌 {type.Name} 可能生成尚未适配的卡牌");
    }

    private static void Reject(string reason) => throw Unsupported(reason);

    private static IncompatibleGameplayModException Unsupported(string reason) =>
        new(ModId, "更好的故障机器人", reason, "combat");
}
