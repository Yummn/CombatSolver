# Android v0.111.0 路线建议预览版

本分支从 Combat Solver 0.47.1 移植。依赖完整的 STS2-RitsuLib 0.6.2 安装目录；
不依赖 BaseLib。请勿只复制 RitsuLib 的根 DLL。

## 当前范围

- 战斗中手动点击「开始计算」查看路线。
- 手机端使用暖色厚描边面板、较大的字号和按钮；面板默认收起并避开顶栏，点击标题栏可展开。
- 默认单线程、3 秒、最多 2000 扩展节点、Beam 12；不在进入战斗时自动搜索。
- 禁止执行本回合、全自动、自动开启全自动，以及战前独立进程预测。
- 不修改游戏运行时 GC 配置；不启用 No-GC 区域或 Windows 内存清理工具。
- 遇到未知模组战斗语义，应由原求解器的覆盖边界拒绝预测，不能把路线当作精确结果。

此预览版**尚未完成 BetterDefect、挑战点系统和回合回溯的战斗语义适配**。
不要在装有这些模组的战斗中把预测当作可信操作指导。

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

## 后续门槛

真实手机测试须分别确认：主菜单加载、进入涅奥、原版战斗的手动路线搜索，
并逐步比对预测与真实出牌后的手牌、能量、生命、球、怪物意图及 RNG 状态。
只有通过模组适配和多场差分验证后，才能解除建议版的执行门禁。
