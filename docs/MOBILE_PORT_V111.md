# Android v0.111.0 完整控制移植预览版

本分支从 Combat Solver 0.47.1 移植。依赖完整的 STS2-RitsuLib 0.6.2 安装目录；
不依赖 BaseLib。请勿只复制 RitsuLib 的根 DLL。

## 当前范围（原版单人战斗）

- 手动搜索、自动计算、本回合执行和连续全自动均已在手机端接通。
- 手机端使用暖色厚描边面板、较大的字号和按钮；面板默认收起并避开顶栏，点击标题栏可展开。
- 首次安装默认关闭自动计算和自动开启全自动；默认单线程、6 秒、最多 4000 扩展节点、Beam 16。
  先前建议预览版的个人设置保留，性能预算可在设置中调整，手机端最高限制为
  30 秒、20000 节点、Beam 32、单线程。
- 未适配效果的路线禁止手机自动出牌；BetterDefect 现在按本场可触达的效果判定，
  不再仅因百科大全保存了多项改造而停止计算。未镜像的改造牌不会进入自动路线，
  其他原版安全牌可搜索并执行；本场存在这些改造牌时面板会提示其须手动打出。
- 战前独立进程预测、在线统计和桌面专用性能预设仍未移植。
- 不修改游戏运行时 GC 配置；不启用 No-GC 区域或 Windows 内存清理工具。
- 其他第三方战斗语义仍需逐项镜像及真实结算差分，不能把有风险提示的路线当作精确结果。

**BetterDefect 兼容正在分阶段开发。** `preview.4` 取消“启用 58 项改造就全局拒绝”的
误判：对实际牌组中的未镜像改造牌排除自动动作，仍可自动使用其他安全牌。手机
v0.111.0 / BetterDefect v0.11.66 / RitsuLib 0.6.2、59 项改造开启的实战中，
求解器完成搜索、执行本回合并进入下一回合；上一个测试构建还执行出击杀。
这**不是** 59 项改造效果的自动出牌支持：改造牌本身、BetterDefect 新增卡／能力／球、
可生成未知卡的牌，以及部分改变原版 Power 的组合仍需逐项镜像与差分验证，
出现时可能仍被明确拒绝。用户也可手动打出求解器跳过的牌并重新计算。
`preview.3` 仅能搜索有限路线且不能执行；`preview.2` 会阻断整个 BetterDefect。
挑战点系统、回合回溯等组合也未完成兼容验证。

## 构建

以游戏 v0.111.0 的 `sts2.dll`、`GodotSharp.dll`、`0Harmony.dll` 和
RitsuLib v0.6.2 完整发行目录为引用：

```powershell
dotnet build CombatSolver.csproj -c Release `
  -p:CombatSolverMobilePort=true -p:CopyModOnBuild=false `
  -p:Sts2DataDir="<0.111.0-managed-assemblies>" `
  -p:RitsuWorkshopRoot="<RitsuLib-0.6.2-release-directory>"
```

安装时在游戏的 `mods/` 下分别放置 `CombatSolver/` 和 `STS2-RitsuLib/`。
`CombatSolver/` 包含 `CombatSolver.dll`、`CombatSolver.json`、`LICENSE` 和
`THIRD_PARTY_NOTICES.md`。RitsuLib 必须保留其 `shared/`、`compat/0.111.0/`、
`assets.zip`、模块清单及根文件。

## 已有真机证据与后续门槛

REDMI K80 Pro / v0.111.0 / RitsuLib 0.6.2：仅加载两份依赖模组，进入原版
铁甲战士首战；手动搜索、执行本回合和次回合全自动执行至胜利均在真机观察到。
首回合预计掉 8 HP，实际 64→56，战斗结束 62/80。
另一次同时加载 BetterDefect 的测试中，求解器拒绝搜索并显示明确提示。
这些观察**不是**全部卡牌、药水、遗物、跨回合续用、随机数或整场严格差分验证。

后续完整兼容 BetterDefect 至少需要：中央出牌桥接／69 种改造／新增卡／Power／
充能球的分支镜像，覆盖牌堆、费用、球位、目标、回合触发和 RNG 的 actual-vs-predicted
差分；还要独立验证挑战点系统与回合回溯组合。没有这些证据之前保持阻断。

### 下一步真机对账

1. 在隔离备份存档中只开启 BetterDefect、RitsuLib、CombatSolver，先用无改造的机器人
   初始卡组比对至少两回合的手牌、能量、伤害、球位和 RNG。
2. 仅启用寒流改造，分别测试未升级／升级、空球位／球位满、冰霜溢出激发，并与
   预测结果逐动作比较；任何差异都维持自动出牌阻断。
3. 才逐张扩充改造牌、能力牌、遗物与多模组组合；每一项都需同样的差分证据。
