# BetterDefect v0.11.66 手机卡牌适配清点（2026-09-30）

目标：CombatSolver 手机 v0.111.0；依据 `BetterDefectCode/OldDefectCards.cs`、`CardVersionUpgrades.cs`、`CardsAndPowers.cs` 与 `src/Prediction/BetterDefectMobileCompatibility.cs` 的源码逐项比对。此清点仅证明当前**放行名单**，不能替代真实战斗差分。

## 一代重制／先古自定义卡

共 23 张 `Bd*` 卡（22 张一代重制卡、1 张达弗先古卡），当前放行 9 张：`BdAutoShields`、`BdConsume`、`BdHeatsinks`、`BdRecursion`、`BdRecycle`、`BdReinforcedBody`、`BdSeek`、`BdStaticDischarge`、`BdStreamline`。

仍未适配 14 张：

| 类型 | 卡名 | 主要额外语义 |
| --- | --- | --- |
| `BdAggregate` | 汇集 | 按抽牌堆张数获得能量 |
| `BdBlizzard` | 暴雪 | 读取本场已生成冰霜球次数 |
| `BdBullseye` | 瞄准靶心 | 锁定与改造后的优先目标能力 |
| `BdCoreSurge` | 核心电涌 | 伤害、人工制品 |
| `BdDoomAndGloom` | 愁云惨淡 | 群伤后生成黑暗球 |
| `BdElectrodynamics` | 电动力学 | 自定义能力让闪电命中全体 |
| `BdFission` | 裂变 | 移除／激发充能球并逐个回能、抽牌 |
| `BdForceField` | 力场 | 动态费用修改 hook |
| `BdMelter` | 熔化 | 移除格挡、伤害及改造追加易伤 |
| `BdReprogram` | 重编程 | 集中、力量、敏捷及改造后的球处理 |
| `BdReworkedBiasedCognition` | 偏差认知*改 | 达弗先古卡及持续集中能力 |
| `BdSelfRepair` | 自我修复 | 战斗结束时回血能力 |
| `BdSteamBarrier` | 蒸汽护壁 | 打出后本场该牌格挡值减少 |
| `BdThunderStrike` | 雷霆打击 | 读取本场已生成闪电球次数、随机多段攻击 |

以上 14 张中除达弗先古卡外均可进入普通卡池；只要其中一张已在本场牌组／战斗牌堆中，预检就会停止求解器，而非只跳过该张牌。

## 百科大全可选改造

`VersionedCardTypes` 共 79 种。按当前求解器放行名单并将 `BufferCard`、`StackCard` 别名还原后，62 种在名单中，17 种尚未放行：

`AdaptiveStrike`, `BdBullseye`, `BdCoreSurge`, `BdForceField`, `BdMelter`, `BdReprogram`, `BdSteamBarrier`, `BdThunderStrike`, `BeamCell`, `BulkUp`, `Hailstorm`, `IceLance`, `MomentumStrike`, `Refract`, `RipAndTear`, `Skim`, `Synthesis`。

其中 7 种也是上表中的自定义卡。此前用户手机存档开启的 59 项改造均在放行名单中，但这**不等于**全部 79 项都适配。未放行的改造即使全局开启，也只在其卡牌进入当前战斗时触发预检拦截；不能因此把原版／未改造效果冒充为改造效果。

## 验证边界

本次只有源码清点，ADB 未连接，没有对上述缺口制作预测镜像，也未实测 9 张已放行自定义卡的所有升级、附魔、球／能力组合。特别是 `BdHeatsinks` 与新加入的 `BdSeek` 仍缺 Android 原生结算对预测的差分证据。缺口应逐张审阅卡牌、能力、随机与隐藏计数语义后补镜像，不能直接把类型加入放行名单。
