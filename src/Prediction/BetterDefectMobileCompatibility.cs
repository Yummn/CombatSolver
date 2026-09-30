using System.Reflection;
using System.Runtime.CompilerServices;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Extensions;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Orbs;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Modding;
using CombatSolver.Engine.Common;
using CombatSolver.Engine.Common.Mirrors;
using CombatSolver.Engine.InCombat.Mirrors;
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
        "BetterDefect.Cards.BdAggregate",
        "BetterDefect.Cards.BdCoreSurge",
        "BetterDefect.Cards.BdDoomAndGloom",
        "BetterDefect.Cards.BdSteamBarrier",
        "BetterDefect.Cards.BdMelter",
        "BetterDefect.Cards.BdReprogram",
        "BetterDefect.Cards.BdBlizzard",
        "BetterDefect.Cards.BdThunderStrike",
        "BetterDefect.Cards.BdForceField",
        "BetterDefect.Cards.BdSelfRepair",
        "BetterDefect.Cards.BdReworkedBiasedCognition",
        "BetterDefect.Cards.BdBullseye",
        "BetterDefect.Cards.BdElectrodynamics",
        "BetterDefect.Cards.BdFission",
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
        "BetterDefect.Cards.BdSelfRepairPower",
        "BetterDefect.Cards.BdReworkedBiasedCognitionPower",
        "BetterDefect.Cards.BdBullseyeTargetPower",
        "BetterDefect.Cards.BdElectrodynamicsPower",
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
        typeof(IceLance), typeof(MomentumStrike), typeof(BeamCell),
        typeof(AdaptiveStrike), typeof(BulkUp), typeof(Refract), typeof(Hailstorm),
        typeof(RipAndTear), typeof(Skim), typeof(Synthesis),
    ];
    private static readonly HashSet<Type> PotentialCardGenerators =
    [
        typeof(Abundance), typeof(BundleOfJoy), typeof(Distraction), typeof(Discovery),
        typeof(InfernalBlade), typeof(JackOfAllTrades), typeof(Jackpot), typeof(Largesse),
        typeof(Havoc), typeof(Cascade), typeof(MadScience), typeof(ManifestAuthority),
        typeof(Metamorphosis), typeof(Quasar),
        typeof(Splash), typeof(Stoke), typeof(WhiteNoise), typeof(HelloWorld), typeof(CreativeAi)
    ];

    private sealed record HiddenPowerSnapshot(
        (int Round, bool Drew)? DrawState,
        int? SmokestackStacks,
        (int EligibleSerial, int Bonus)[]? StormBatches);

    // Native patch tables are mutable. Freeze them on the main-thread root
    // capture; worker branches only read these immutable values or their own
    // newly created power instance. Weak keys do not retain finished combats.
    private static readonly ConditionalWeakTable<PowerModel, HiddenPowerSnapshot> RootPowerSnapshots = new();
    private sealed record RootCombatCounters(int Frost, int Lightning, int Powers);
    private static readonly ConditionalWeakTable<Player, RootCombatCounters> RootCounters = new();

    internal static int CombatCounter(CombatPredictionSimulator simulator, Player owner, string kind)
    {
        if (!RootCounters.TryGetValue(owner, out RootCombatCounters? root))
            throw Unsupported("未捕获 BetterDefect 战斗累计计数");
        return kind switch
        {
            "Frost" => root.Frost + simulator.History.OfType<CombatPredictionOrbChanneledEntry>()
                .Count(entry => entry.Orb is FrostOrb && entry.Orb.Owner == owner),
            "Lightning" => root.Lightning + simulator.History.GetCounters(owner).LightningChannels,
            "Powers" => root.Powers + simulator.History.OfType<CombatPredictionCardPlayStartedEntry>()
                .Count(entry => entry.Card.Owner == owner && entry.Card.Type == CardType.Power),
            _ => throw Unsupported($"未知战斗累计计数 {kind}"),
        };
    }

    private static RootCombatCounters ReadNativeCombatCounters(Assembly assembly, Player player)
    {
        Type tracker = assembly.GetType("BetterDefect.BdCombatTracker", true)!;
        MethodInfo method = tracker.GetMethod("For", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw Unsupported("找不到 BetterDefect 战斗计数接口");
        object stats = method.Invoke(null, [player])
            ?? throw Unsupported("BetterDefect 战斗计数为空");
        int Read(string name) => stats.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(stats) is int value ? value : throw Unsupported($"战斗计数 {name} 不可读取");
        return new(Read("FrostChanneled"), Read("LightningChanneled"), Read("PowerCardsPlayed"));
    }

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

    // The reviewed BetterDefect build installs Harmony OnPlay prefixes on PC.
    // They are the native side of the explicit transformations mirrored here,
    // not an unknown second gameplay mod. Keep all other foreign patches gated.
    internal static bool IsReviewedOnPlayPatch(Type patchType, MethodInfo target)
    {
        Assembly? assembly = Volatile.Read(ref _activeAssembly);
        Type? cardType = target.DeclaringType;
        return MobilePortPolicy.IsMobile && assembly is not null
            && patchType.Assembly == assembly
            && patchType.FullName?.StartsWith("BetterDefect.", StringComparison.Ordinal) == true
            && cardType is not null
            && (MirroredTransformations.Contains(cardType)
                || MirroredCustomCards.Contains(cardType.FullName ?? ""));
    }

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
        // The PC v0.111.0 fixture uses BetterDefect's native Harmony route;
        // only Android production requires the Ritsu card-play callback.
        if (OperatingSystem.IsAndroid() && readyProperty?.GetValue(null) is not true)
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
            RootCounters.Remove(player);
            RootCounters.Add(player, ReadNativeCombatCounters(assembly, player));
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

    internal static int ModifyReviewedFocusLoss(SimulatedCombatState combat,
        MegaCrit.Sts2.Core.Entities.Creatures.Creature target, int amount)
    {
        if (amount >= 0 || !HasReviewedMobileMod)
            return amount;
        return combat.EffectivePowers().Any(power => power.Owner == target && power.Amount > 0
            && power.GetType().FullName == "BetterDefect.Cards.BdReworkedBiasedCognitionPower"
            && IsMirroredPower(power))
            ? Math.Min(0, amount + 1) : amount;
    }

    internal static MegaCrit.Sts2.Core.Entities.Creatures.Creature? PriorityOrbTarget(
        CombatPredictionSimulator simulator, MegaCrit.Sts2.Core.Entities.Players.Player owner)
    {
        if (!IsTransformedCustomCard("BetterDefect.Cards.BdBullseye")
            || simulator.State.CombatState is not SimulatedCombatState combat)
            return null;
        return simulator.State.GetOpponentsOf(owner.Creature)
            .Where(simulator.State.IsHittable)
            .FirstOrDefault(enemy =>
                combat.EffectivePowers().Any(power => power.Owner == enemy && power.Amount > 0
                    && power.GetType().FullName == "BetterDefect.Cards.BdBullseyeTargetPower"
                    && IsMirroredPower(power))
                && combat.EffectivePowers().Any(power => power.Owner == enemy && power.Amount > 0
                    && power.GetType().FullName == "BetterDefect.Cards.BdLockOnPower"
                    && IsMirroredPower(power)));
    }

    internal static bool HasElectrodynamics(CombatPredictionSimulator simulator, Player owner)
        => simulator.State.CombatState is SimulatedCombatState combat
            && combat.EffectivePowers().Any(power => power.Owner == owner.Creature && power.Amount > 0
                && power.GetType().FullName == "BetterDefect.Cards.BdElectrodynamicsPower"
                && IsMirroredPower(power));

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
            case "BetterDefect.Cards.BdElectrodynamics":
            {
                if (simulator.State.CombatState is not SimulatedCombatState combat)
                    throw Unsupported("电动力学缺少分支战斗状态");
                var owner = card.Preview.Owner;
                combat.ApplyPower(ReviewedPowerType("BetterDefect.Cards.BdElectrodynamicsPower"),
                    owner.Creature, 1, owner.Creature);
                if (!simulator.HasPendingChoice)
                    ChannelElectrodynamics(simulator, owner,
                        card.MutablePreview.DynamicVars["Amount"].IntValue);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdFission":
            {
                var owner = card.Preview.Owner;
                int count = simulator.State.GetPlayerCombatState(owner).OrbQueue.Orbs.Count;
                ContinueFission(simulator, owner, card.Preview.IsUpgraded, count, gainAfterEvoke: false);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdBullseye":
            {
                if (cardPlay.Target is not { } target)
                    throw Unsupported("瞄准靶心缺少目标");
                if (simulator.State.CombatState is not SimulatedCombatState combat)
                    throw Unsupported("瞄准靶心缺少分支战斗状态");
                var owner = card.Preview.Owner.Creature;
                simulator.Damage([target], card.MutablePreview.DynamicVars.Damage.BaseValue,
                    card.MutablePreview.DynamicVars.Damage.Props, owner, card, null);
                if (simulator.HasPendingChoice)
                {
                    simulator.AppendExecutionContinuation(new BdAfterDamageFrame(card, target,
                        BdAfterDamageKind.Bullseye));
                    result = new(MirrorDispatchKind.Handled);
                    return true;
                }
                ContinueAfterDamage(simulator, card, target, BdAfterDamageKind.Bullseye);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdReworkedBiasedCognition":
            {
                if (simulator.State.CombatState is not SimulatedCombatState combat)
                    throw Unsupported("偏差认知*改缺少分支战斗状态");
                var owner = card.Preview.Owner.Creature;
                combat.Apply<FocusPower>(owner,
                    card.MutablePreview.DynamicVars["FocusPower"].IntValue, owner);
                if (!simulator.HasPendingChoice)
                    combat.ApplyPower(ReviewedPowerType("BetterDefect.Cards.BdReworkedBiasedCognitionPower"),
                        owner, card.MutablePreview.DynamicVars["Decay"].IntValue, owner);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdSelfRepair":
            {
                if (simulator.State.CombatState is not SimulatedCombatState combat)
                    throw Unsupported("自我修复缺少分支战斗状态");
                var owner = card.Preview.Owner.Creature;
                combat.ApplyPower(ReviewedPowerType("BetterDefect.Cards.BdSelfRepairPower"),
                    owner, card.MutablePreview.DynamicVars.Heal.IntValue, owner);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdBlizzard":
            {
                decimal damage = card.MutablePreview.DynamicVars.Damage.BaseValue
                    * CombatCounter(simulator, card.Preview.Owner, "Frost");
                simulator.Damage(simulator.State.HittableEnemies, damage,
                    MegaCrit.Sts2.Core.ValueProps.ValueProp.Move,
                    card.Preview.Owner.Creature, card, null);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdThunderStrike":
            {
                int hits = CombatCounter(simulator, card.Preview.Owner, "Lightning");
                ContinueThunderStrike(simulator, card, hits);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdForceField":
            {
                simulator.GainBlock(card.Preview.Owner.Creature,
                    card.MutablePreview.DynamicVars.Block, card, cardPlay);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdAggregate":
            {
                int divisor = Math.Max(1, checked((int)card.MutablePreview.DynamicVars["Divisor"].BaseValue));
                int count = simulator.State.GetPlayerCombatState(card.Preview.Owner).DrawPile.Cards.Count;
                simulator.GainEnergy(card.Preview.Owner, count / divisor);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdCoreSurge":
            {
                if (cardPlay.Target is not null)
                    simulator.Damage([cardPlay.Target], card.MutablePreview.DynamicVars.Damage.BaseValue,
                        card.MutablePreview.DynamicVars.Damage.Props,
                        card.Preview.Owner.Creature, card, null);
                if (simulator.HasPendingChoice)
                    simulator.AppendExecutionContinuation(new BdAfterDamageFrame(card, null,
                        BdAfterDamageKind.CoreSurge));
                else
                    ContinueAfterDamage(simulator, card, null, BdAfterDamageKind.CoreSurge);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdDoomAndGloom":
            {
                simulator.Damage(simulator.State.HittableEnemies,
                    card.MutablePreview.DynamicVars.Damage.BaseValue,
                    card.MutablePreview.DynamicVars.Damage.Props,
                    card.Preview.Owner.Creature, card, null);
                if (simulator.HasPendingChoice)
                    simulator.AppendExecutionContinuation(new BdAfterDamageFrame(card, null,
                        BdAfterDamageKind.DoomAndGloom));
                else
                    ContinueAfterDamage(simulator, card, null, BdAfterDamageKind.DoomAndGloom);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdSteamBarrier":
            {
                simulator.GainBlock(card.Preview.Owner.Creature,
                    card.MutablePreview.DynamicVars.Block, card, cardPlay);
                if (!simulator.HasPendingChoice)
                    card.MutablePreview.DynamicVars.Block.BaseValue =
                        Math.Max(0, card.MutablePreview.DynamicVars.Block.BaseValue - 1);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdMelter":
            {
                if (cardPlay.Target is not null)
                {
                    var target = simulator.State.GetCreature(cardPlay.Target);
                    int before = target.Block;
                    if (before > 0)
                    {
                        target.DamageBlock(before, MegaCrit.Sts2.Core.ValueProps.ValueProp.Unpowered);
                        HookMirrors.AfterBlockBroken(simulator, cardPlay.Target, card.Preview.Owner.Creature);
                    }
                    if (simulator.HasPendingChoice)
                        simulator.AppendExecutionContinuation(new BdMelterAfterBlockFrame(card, cardPlay.Target));
                    else
                        ContinueMelterAfterBlock(simulator, card, cardPlay.Target);
                }
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
            case "BetterDefect.Cards.BdReprogram":
            {
                var owner = card.Preview.Owner;
                var queue = simulator.State.GetPlayerCombatState(owner).OrbQueue;
                int evokes = 0;
                if (IsTransformed(card.Preview))
                {
                    int count = queue.Orbs.Count;
                    for (int index = 0; index < count; index++)
                    {
                        if (card.Preview.IsUpgraded)
                            evokes++;
                        else
                            queue.Remove(queue.Orbs[0]);
                    }
                }
                ContinueReprogram(simulator, owner, evokes,
                    card.MutablePreview.DynamicVars["Focus"].IntValue,
                    card.MutablePreview.DynamicVars.Strength.IntValue,
                    card.MutablePreview.DynamicVars.Dexterity.IntValue);
                result = new(MirrorDispatchKind.Handled);
                return true;
            }
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

    private static bool ChannelElectrodynamics(CombatPredictionSimulator simulator,
        Player owner, int remaining)
    {
        while (remaining > 0 && !simulator.HasPendingChoice)
        {
            int before = simulator.History.Count<CombatPredictionOrbChanneledEntry>();
            _ = simulator.OrbChannel(owner, ModelDb.Orb<LightningOrb>().ToMutable());
            if (simulator.History.Count<CombatPredictionOrbChanneledEntry>() > before)
                remaining--;
            if (simulator.HasPendingChoice)
                simulator.AppendExecutionContinuation(new BdLightningChannelFrame(owner, remaining));
            else if (simulator.History.Count<CombatPredictionOrbChanneledEntry>() == before)
                throw Unsupported("电动力学未能生成闪电球");
        }
        return !simulator.HasPendingChoice;
    }

    private sealed record BdLightningChannelFrame(Player Owner, int Remaining)
        : ICombatPredictionExecutionFrame
    {
        public ICombatPredictionExecutionFrame Fork(PredictionForkContext context) => this;
        public bool Resume(CombatPredictionSimulator simulator)
            => ChannelElectrodynamics(simulator, Owner, Remaining);
    }

    // Orb evocation and draw can both open a native choice. Keep the card's
    // remaining loop in a forkable frame instead of silently dropping it.
    private static bool ContinueFission(CombatPredictionSimulator simulator,
        Player owner, bool upgraded, int remaining, bool gainAfterEvoke)
    {
        while (remaining > 0)
        {
            if (!gainAfterEvoke)
            {
                var queue = simulator.State.GetPlayerCombatState(owner).OrbQueue;
                if (queue.Orbs.Count == 0)
                    break;
                if (upgraded)
                    simulator.OrbEvokeNext(owner);
                else
                    queue.Remove(queue.Orbs[0]);
                gainAfterEvoke = true;
                if (simulator.HasPendingChoice)
                {
                    simulator.AppendExecutionContinuation(
                        new BdFissionFrame(owner, upgraded, remaining, gainAfterEvoke));
                    return false;
                }
            }
            simulator.GainEnergy(owner, 1);
            simulator.Draw(owner, 1);
            remaining--;
            gainAfterEvoke = false;
            if (simulator.HasPendingChoice)
            {
                simulator.AppendExecutionContinuation(
                    new BdFissionFrame(owner, upgraded, remaining, gainAfterEvoke));
                return false;
            }
        }
        return true;
    }

    private sealed record BdFissionFrame(Player Owner, bool Upgraded, int Remaining,
        bool GainAfterEvoke) : ICombatPredictionExecutionFrame
    {
        public ICombatPredictionExecutionFrame Fork(PredictionForkContext context) => this;
        public bool Resume(CombatPredictionSimulator simulator)
            => ContinueFission(simulator, Owner, Upgraded, Remaining, GainAfterEvoke);
    }

    private static bool ContinueReprogram(CombatPredictionSimulator simulator,
        Player owner, int evokesRemaining, int focusLoss, int strength, int dexterity)
    {
        while (evokesRemaining > 0 && !simulator.HasPendingChoice)
        {
            simulator.OrbEvokeNext(owner);
            evokesRemaining--;
            if (simulator.HasPendingChoice)
            {
                simulator.AppendExecutionContinuation(new BdReprogramFrame(
                    owner, evokesRemaining, focusLoss, strength, dexterity));
                return false;
            }
        }
        if (simulator.State.CombatState is not SimulatedCombatState combat)
            throw Unsupported("重编程缺少分支战斗状态");
        combat.Apply<FocusPower>(owner.Creature, -focusLoss, owner.Creature);
        combat.Apply<StrengthPower>(owner.Creature, strength, owner.Creature);
        combat.Apply<DexterityPower>(owner.Creature, dexterity, owner.Creature);
        return !simulator.HasPendingChoice;
    }

    private sealed record BdReprogramFrame(Player Owner, int EvokesRemaining,
        int FocusLoss, int Strength, int Dexterity) : ICombatPredictionExecutionFrame
    {
        public ICombatPredictionExecutionFrame Fork(PredictionForkContext context) => this;
        public bool Resume(CombatPredictionSimulator simulator)
            => ContinueReprogram(simulator, Owner, EvokesRemaining, FocusLoss, Strength, Dexterity);
    }

    private static bool ContinueThunderStrike(CombatPredictionSimulator simulator,
        PredictedCard card, int remaining)
    {
        while (remaining > 0 && !simulator.HasPendingChoice)
        {
            var enemies = simulator.State.HittableEnemies.ToList();
            if (enemies.Count == 0)
                break;
            var target = simulator.Rng.CombatTargets.NextItem(enemies)
                ?? throw Unsupported("雷霆打击没有可选目标");
            simulator.Damage([target], card.MutablePreview.DynamicVars.Damage.BaseValue,
                card.MutablePreview.DynamicVars.Damage.Props,
                card.Preview.Owner.Creature, card, null);
            remaining--;
            if (simulator.HasPendingChoice)
            {
                simulator.AppendExecutionContinuation(new BdThunderStrikeFrame(card, remaining));
                return false;
            }
        }
        return true;
    }

    private sealed record BdThunderStrikeFrame(PredictedCard Card, int Remaining)
        : ICombatPredictionExecutionFrame
    {
        public ICombatPredictionExecutionFrame Fork(PredictionForkContext context)
            => this with { Card = context.RequireRemap(Card) };
        public bool Resume(CombatPredictionSimulator simulator)
            => ContinueThunderStrike(simulator, Card, Remaining);
    }

    private enum BdAfterDamageKind { Bullseye, CoreSurge, DoomAndGloom, Melter }

    private static bool ContinueMelterAfterBlock(CombatPredictionSimulator simulator,
        PredictedCard card, Creature target)
    {
        simulator.Damage([target], card.MutablePreview.DynamicVars.Damage.BaseValue,
            card.MutablePreview.DynamicVars.Damage.Props,
            card.Preview.Owner.Creature, card, null);
        if (simulator.HasPendingChoice)
        {
            simulator.AppendExecutionContinuation(new BdAfterDamageFrame(card, target,
                BdAfterDamageKind.Melter));
            return false;
        }
        return ContinueAfterDamage(simulator, card, target, BdAfterDamageKind.Melter);
    }

    private sealed record BdMelterAfterBlockFrame(PredictedCard Card, Creature Target)
        : ICombatPredictionExecutionFrame
    {
        public ICombatPredictionExecutionFrame Fork(PredictionForkContext context)
            => this with { Card = context.RequireRemap(Card) };
        public bool Resume(CombatPredictionSimulator simulator)
            => ContinueMelterAfterBlock(simulator, Card, Target);
    }

    private static bool ContinueAfterDamage(CombatPredictionSimulator simulator,
        PredictedCard card, Creature? target, BdAfterDamageKind kind)
    {
        if (simulator.State.CombatState is not SimulatedCombatState combat)
            throw Unsupported("后续卡牌效果缺少分支战斗状态");
        Creature owner = card.Preview.Owner.Creature;
        switch (kind)
        {
            case BdAfterDamageKind.Bullseye:
                if (target is null)
                    throw Unsupported("瞄准靶心缺少后续目标");
                combat.ApplyPower(ReviewedPowerType("BetterDefect.Cards.BdLockOnPower"),
                    target, card.MutablePreview.DynamicVars["LockOn"].IntValue, owner);
                if (IsTransformed(card.Preview))
                {
                    foreach (PowerModel marker in combat.EffectivePowers().Where(power =>
                                 power.Owner != target && power.Amount > 0
                                 && power.GetType().FullName == "BetterDefect.Cards.BdBullseyeTargetPower").ToArray())
                        combat.SetPowerAmount(marker, 0);
                    combat.ApplyPower(ReviewedPowerType("BetterDefect.Cards.BdBullseyeTargetPower"),
                        target, 1, owner);
                }
                break;
            case BdAfterDamageKind.CoreSurge:
                combat.Apply<ArtifactPower>(owner, 1, owner);
                break;
            case BdAfterDamageKind.DoomAndGloom:
            {
                int before = simulator.History.Count<CombatPredictionOrbChanneledEntry>();
                _ = simulator.OrbChannel(card.Preview.Owner, ModelDb.Orb<DarkOrb>().ToMutable());
                if (simulator.HasPendingChoice
                    && simulator.History.Count<CombatPredictionOrbChanneledEntry>() == before)
                    simulator.AppendExecutionContinuation(new BdAfterDamageFrame(card, null, kind));
                break;
            }
            case BdAfterDamageKind.Melter:
                if (target is not null && IsTransformed(card.Preview))
                    combat.Apply<VulnerablePower>(target,
                        card.Preview.IsUpgraded ? 2 : 1, owner);
                break;
        }
        return !simulator.HasPendingChoice;
    }

    private sealed record BdAfterDamageFrame(PredictedCard Card, Creature? Target,
        BdAfterDamageKind Kind) : ICombatPredictionExecutionFrame
    {
        public ICombatPredictionExecutionFrame Fork(PredictionForkContext context)
            => this with { Card = context.RequireRemap(Card) };
        public bool Resume(CombatPredictionSimulator simulator)
            => ContinueAfterDamage(simulator, Card, Target, Kind);
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
