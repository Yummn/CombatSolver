# Android v0.111.0 完整控制移植预览版

本分支从 Combat Solver 0.47.1 移植。依赖完整的 STS2-RitsuLib 0.6.2 安装目录；
不依赖 BaseLib。请勿只复制 RitsuLib 的根 DLL。

## 当前范围（原版单人战斗）

- 手动搜索、自动计算、本回合执行和连续全自动均已在手机端接通。
- 手机端使用暖色厚描边面板、较大的字号和按钮；面板默认收起并避开顶栏，点击标题栏可展开。
- 首次安装默认关闭自动计算和自动开启全自动；默认单线程、6 秒、最多 4000 扩展节点、Beam 16。
  先前建议预览版的个人设置保留，性能预算可在设置中调整，手机端最高限制为
  30 秒、20000 节点、Beam 32、单线程。
- 未适配效果的路线禁止手机自动出牌；开启 BetterDefect v0.11.x 时会在计算入口明确停止，
  因其 Android 中央出牌补丁不在原版 `CardModel.OnPlay` 审计范围内。
- 战前独立进程预测、在线统计和桌面专用性能预设仍未移植。
- 不修改游戏运行时 GC 配置；不启用 No-GC 区域或 Windows 内存清理工具。
- 其他第三方战斗语义仍需逐项镜像及真实结算差分，不能把有风险提示的路线当作精确结果。

**这不是 BetterDefect 兼容版。** 更好的故障机器人新增卡、改造牌、球和能力的预测镜像
尚未开发，因此装有该模组时不能使用求解器。挑战点系统、回合回溯等组合也未完成兼容验证。

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
