# BetterDefect 59-card mobile adaptation

Target: Android game v0.111.0, BetterDefect v0.11.66. The phone's persisted encyclopedia state contains 59 enabled transformations. All 59 named transformed types now have an explicit mirror or captured-data-only admission path. This is an adapter for that exact version and selection, not a promise that every BetterDefect original card or every third-party Mod is supported.

## Current boundary

- The original 37 enabled card types retain reviewed branch mirrors or captured model-data handling. Two further mirrored types (`GoForTheEyes`, `MeteorStrike`) are not enabled in this save.
- `AllForOne` and `Rebound` now have Android v0.111.0 full-auto fixtures that exercise their native discard-pile selection paths. Their mirrors are admitted in release builds.
- The remaining 22 enabled types now have branch mirrors for their card, generated-card, orb, power, selection, per-turn and auto-play effects. The solver still rejects unknown BetterDefect-owned cards and powers rather than silently substituting vanilla behavior.
- Some effects are power, orb, choice, or card-generation hooks rather than `OnPlay`. A card is not complete merely because its play animation can finish.
- A version mismatch against BetterDefect v0.11.66 remains a hard rejection.

## Confirmed Android differential tests

The deterministic DEFECT/SLIMES_WEAK fixtures use the actual Android v0.111.0 game and full-auto execution. They assert the first played card, zero unmirrored types in the injected combat, and no unexpected replan. Where the fight reaches turn two, they additionally assert exact route-state reuse. Confirmed cases:

`ColdSnap`, `GunkUp`, `DoubleEnergy`, `ChargeBattery`, `FightThrough`, `Rainbow`, `Leap`, `FocusedStrike`, `SweepingBeam`, `Coolheaded`, `LightningRod`, `TeslaCoil`, `Buffer`, `HelixDrill`, `Sunder` (including a non-lethal first hit and in-combat cost reduction), `FlakCannon`, `Tempest`, `Null` against already-Weak enemies, `RocketPunch` both alone and after `GunkUp` generates a Status, `Shatter`, `Compact` with Fuel generation, `Chaos`, `MultiCast`, `Loop`, `Coolant`, `ConsumingShadow`, `Claw`, and `Feral` with a zero-energy Skill returning to hand.

2026-09-29: `Rebound` and `AllForOne` passed isolated BetterDefect + CombatSolver + RitsuLib v0.111.0 full-auto fixtures (`bd59-rebound-strict-20260929`, `bd59-all-for-one-strict-20260929`). Both asserted the first played card, zero unmirrored cards, exact turn-two route reuse, and zero unexpected replans. Native logs additionally recorded `Rebound` selecting `CLAW` from discard and placing it atop the draw pile; `AllForOne` selected `CLAW` and subsequently played it. These are basic selection-path checks, not exhaustive deck/combat-state coverage.

With six other installed mods enabled, even the previously passing `ColdSnap` control timed out awaiting the first solver result; the same two candidates also timed out in that mixed-mod profile. An additional `ColdSnap` control with only `EventSkip`, `SpireBank`, and `LoserEatDust` added to the three-mod fixture also timed out; that narrows but does not identify the interacting mod. After isolating the three-mod fixture, both candidates passed. The mixed-mod startup/search interaction remains unverified and must not be presented as full compatibility.

These fixtures do not prove every upgrade level, status interaction, enemy, relic, or enchantment combination. `TrashToTreasure` changes only captured cost/Innate model data in the reviewed BetterDefect source; its prior basic test did not force the AI to play it.

## Newly mirrored transformations (22)

`BD_RECURSION`, `SCRAPE`, `WHITE_NOISE`, `SUBROUTINE`, `SMOKESTACK`, `CREATIVE_AI`, `BD_STREAMLINE`, `UPROAR`, `FTL`, `ECHO_FORM`, `HELLO_WORLD`, `BARRAGE`, `BD_AUTO_SHIELDS`, `HYPERBEAM`, `BD_REINFORCED_BODY`, `SPINNER`, `BD_RECYCLE`, `BD_STATIC_DISCHARGE`, `STACK`, `STORM`, `ITERATION`, `BD_CONSUME`.

The desktop Steam game currently installed for development is v0.107.1, while this adapter targets v0.111.0. Running BetterDefect on desktop v0.107.1 can check its native card effects but cannot verify this v0.111.0 solver binary or its shadow-state differential semantics. Do not count such a desktop run as Android adapter acceptance.

All 22 have passed at least one isolated Android v0.111.0 actual/simulated differential fixture. `Uproar` also passed an upgraded two-auto-play fixture; `CreativeAi` and `HelloWorld` passed turn-start native-choice fixtures. The saved 59-card profile passed a short real-search fixture with `expectedInitialUnmirroredCount=0`, first-card deployment, turn-two reuse and no unexpected replan. `Storm`, `Iteration`, `Smokestack`, `Subroutine`, `Spinner`, `BdStaticDischarge` and other hooks are represented in branch-local state; hidden counters and charge batches are included in search fingerprints and continuation comparison. BetterDefect's mutable private per-power counters are frozen at main-thread root capture before worker search reads them; branch changes are owned by forked `PredictionStateStore` entries.

2026-09-30 resumed acceptance: the phone's persisted `BetterDefect.CardUpgrades.state.dat` contains 59 enabled IDs, all 59 matching the adapter's reviewed transformation/custom-card names. `bd59-final-final-short-search-corrected-result.json` passed initial search (`Unmirrored=0`, first action `REBOUND`). `bd59-final-subroutine-power-continuation-single-result.json` passed a real two-turn deployment with transformed `SUBROUTINE_POWER` already active, `Reuse:Turn=2`, and `UnexpectedReplans:0`. The three-enemy `Rebound` follow-up on an earlier intermediate build ended without a result; it is not counted as passing evidence. Evidence files are under `../.port-build/mobile-betterdefect-compat-20260928/`.

The non-test Android Release DLL was installed after the fixture listener and request files were removed. Its on-device SHA-256 matched the local production build (`5e42579f2761ab7f4c87a678edd44c9ee079e779173b3fbf05c8bca672203bb0`). The original nine enabled mods were preserved and CombatSolver was enabled as the tenth. The real v0.111.0 game reached its main menu with `CombatSolver` initialized and BetterDefect reporting `upgrades=59`; screenshot `production-10-mods-menu-20260930.png`. The production binary was not exercised in another complete battle after installation; the isolated differential and two-turn tests used the same behavior source compiled with the test listener.

This acceptance is deliberately narrower than exhaustive gameplay: mixed-mod startup/search, every card upgrade/enchantment interaction, generated BetterDefect cards outside the reviewed set, all relics and enemies, and every multiple-stack/multiple-hit timing remain unverified. Unknown card/power effects fail closed. Do not describe this as universal BetterDefect compatibility.

## Test-install hygiene

The Android unattended request listener is compiled only with `CombatSolverMobileTest=true`; normal mobile builds omit it. Never publish or leave a test build on the player's phone. Restore the pre-test phone backup and saved mod profile after each test round.
