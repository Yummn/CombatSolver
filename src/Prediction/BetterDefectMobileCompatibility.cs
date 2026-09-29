using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Modding;

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
    private static readonly HashSet<Type> AlwaysReplacedCards =
        [typeof(Scrape)];
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
        ["EchoFormPower"] = "EchoForm",
        ["HailstormPower"] = "Hailstorm",
        ["IterationPower"] = "Iteration",
        ["SmokestackPower"] = "Smokestack",
        ["SubroutinePower"] = "Subroutine",
    };

    // Written at main-thread root capture, read by branch-local card mirrors.
    private static Assembly? _activeAssembly;
    private static HashSet<Type> _transformedTypes = [];
    private static HashSet<Type> _unmirroredTransformedTypes = [];
    private static int _transformedTypesInCombat;

    internal static bool ColdSnapTransformed =>
        IsTransformed<ColdSnap>();

    internal static bool HasReviewedMobileMod =>
        MobilePortPolicy.IsMobile && Volatile.Read(ref _activeAssembly) is not null;

    internal static bool IsTransformed<TCard>() where TCard : CardModel
        => MobilePortPolicy.IsMobile && Volatile.Read(ref _activeAssembly) is not null
            && Volatile.Read(ref _transformedTypes).Contains(typeof(TCard));

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
                ValidateCard(card, assembly);
                if (transformed.Contains(card.GetType()) && !MirroredTransformations.Contains(card.GetType()))
                    transformedInCombat.Add(card.GetType());
            }
            foreach (CardModel card in player.PlayerCombatState?.AllCards ?? [])
            {
                ValidateCard(card, assembly);
                if (transformed.Contains(card.GetType()) && !MirroredTransformations.Contains(card.GetType()))
                    transformedInCombat.Add(card.GetType());
            }
            foreach (var orb in player.PlayerCombatState?.OrbQueue.Orbs ?? [])
                if (orb.GetType().Assembly == assembly)
                    Reject($"充能球 {orb.GetType().Name} 尚未适配");
            foreach (var potion in player.Potions)
                if (potion is AttackPotion or SkillPotion or PowerPotion or OrobicAcid
                    or ColorlessPotion or CosmicConcoction)
                    Reject($"药水 {potion.GetType().Name} 可能生成尚未适配的卡牌");
        }
        foreach (var power in combat.Creatures.SelectMany(creature => creature.Powers))
        {
            if (power.GetType().Assembly == assembly)
                Reject($"能力 {power.GetType().Name} 尚未适配");
            if (power.GetType().Name is "CreativeAiPower" or "HelloWorldPower")
                Reject($"能力 {power.GetType().Name} 可能生成尚未适配的卡牌");
            if (ModifiedPowerSources.TryGetValue(power.GetType().Name, out string? source)
                && transformed.Any(type => type.Name == source))
                Reject($"已生效的改造能力 {power.GetType().Name} 尚未适配");
        }

        HashSet<Type> unmirrored = [.. transformed.Except(MirroredTransformations)];
        Volatile.Write(ref _transformedTypes, transformed);
        Volatile.Write(ref _unmirroredTransformedTypes, unmirrored);
        Volatile.Write(ref _transformedTypesInCombat, transformedInCombat.Count);
        Volatile.Write(ref _activeAssembly, assembly);
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
            ValidateCard(card, assembly);
            if (Volatile.Read(ref _unmirroredTransformedTypes).Contains(card.GetType()))
                Reject($"改造牌 {card.GetType().Name} 尚未建立预测镜像");
        }
    }

    private static void ValidateCard(CardModel card, Assembly assembly)
    {
        Type type = card.GetType();
        if (type.Assembly == assembly || AlwaysReplacedCards.Contains(type))
            Reject($"卡牌 {type.Name} 的效果尚未适配");
        if (PotentialCardGenerators.Contains(type))
            Reject($"卡牌 {type.Name} 可能生成尚未适配的卡牌");
    }

    private static void Reject(string reason) => throw Unsupported(reason);

    private static IncompatibleGameplayModException Unsupported(string reason) =>
        new(ModId, "更好的故障机器人", reason, "combat");
}
