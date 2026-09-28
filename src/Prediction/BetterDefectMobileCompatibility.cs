using System.Reflection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Potions;
using MegaCrit.Sts2.Core.Modding;

namespace CombatSolver;

/// <summary>
/// A deliberately narrow BetterDefect contract. The central Android card-play
/// bridge cannot be inferred from CardModel.OnPlay, so only unchanged vanilla
/// effects and the explicitly mirrored Cold Snap transformation are admitted.
/// </summary>
internal static class BetterDefectMobileCompatibility
{
    private const string ModId = "BetterDefect";
    private const string ReviewedVersion = "0.11.66";
    // Runtime registration is a stronger compatibility signal than MVID:
    // reproducible builds of the same source can have different module IDs.
    private static readonly HashSet<Type> AlwaysReplacedCards =
        [typeof(Shatter), typeof(TeslaCoil), typeof(Fuel), typeof(Scrape)];
    private static readonly HashSet<Type> PotentialCardGenerators =
    [
        typeof(Abundance), typeof(BundleOfJoy), typeof(Distraction), typeof(Discovery),
        typeof(InfernalBlade), typeof(JackOfAllTrades), typeof(Jackpot), typeof(Largesse),
        typeof(MadScience), typeof(ManifestAuthority), typeof(Metamorphosis), typeof(Quasar),
        typeof(Splash), typeof(Stoke), typeof(WhiteNoise), typeof(HelloWorld), typeof(CreativeAi)
    ];

    // Written at main-thread root capture, read by branch-local card mirrors.
    private static Assembly? _activeAssembly;
    private static int _coldSnapTransformed;

    internal static bool ColdSnapTransformed =>
        MobilePortPolicy.IsMobile && Volatile.Read(ref _coldSnapTransformed) != 0;

    // This narrow mirror has only passed source/compile checks. Do not let a
    // predicted route control the real game before actual-vs-predicted tests.
    internal static bool RequiresDeploymentValidation =>
        MobilePortPolicy.IsMobile && Volatile.Read(ref _activeAssembly) is not null;

    internal static void Validate(CombatState combat)
    {
        if (!MobilePortPolicy.IsMobile)
            return;

        Mod? mod = ModManager.GetLoadedMods().FirstOrDefault(item =>
            string.Equals(item.manifest?.id, ModId, StringComparison.OrdinalIgnoreCase));
        if (mod is null)
        {
            Volatile.Write(ref _coldSnapTransformed, 0);
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
        bool coldSnap;
        try
        {
            count = (int)(countMethod.Invoke(null, null) ?? -1);
            coldSnap = (bool)(enabledMethod.Invoke(null, [ModelDb.Card<ColdSnap>()]) ?? false);
        }
        catch (Exception error)
        {
            throw Unsupported($"读取改造状态失败：{error.GetType().Name}");
        }
        // All other transformations can change card hooks, powers, or even orb
        // targeting without the transformed card currently being in the deck.
        if (count != (coldSnap ? 1 : 0))
            Reject($"仍有 {count - (coldSnap ? 1 : 0)} 张未适配的改造牌启用");

        foreach (var player in combat.Players)
        {
            foreach (CardModel card in player.Deck.Cards)
                ValidateCard(card, assembly);
            foreach (CardModel card in player.PlayerCombatState?.AllCards ?? [])
                ValidateCard(card, assembly);
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
        }

        Volatile.Write(ref _activeAssembly, assembly);
        Volatile.Write(ref _coldSnapTransformed, coldSnap ? 1 : 0);
    }

    internal static void ValidateCardOnPlay(CardModel card)
    {
        if (!MobilePortPolicy.IsMobile)
            return;
        Assembly? assembly = Volatile.Read(ref _activeAssembly);
        if (assembly is not null)
            ValidateCard(card, assembly);
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
