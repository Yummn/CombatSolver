# BetterDefect 59-card mobile adaptation (work in progress)

Target: Android game v0.111.0, BetterDefect v0.11.66. The reviewed save has 59 enabled transformations. This branch is **not** a complete 59-card release.

## Current boundary

- 37 enabled card types have reviewed branch mirrors or only captured model-data changes. Two further mirrored types (`GoForTheEyes`, `MeteorStrike`) are not enabled in this save.
- `AllForOne` and `Rebound` now have Android v0.111.0 full-auto fixtures that exercise their native discard-pile selection paths. Their mirrors are admitted in release builds.
- The remaining 22 enabled types are still protected from prediction as transformed cards in release builds. The solver must not silently substitute vanilla effects for them.
- Some effects are power, orb, choice, or card-generation hooks rather than `OnPlay`. A card is not complete merely because its play animation can finish.
- A version mismatch against BetterDefect v0.11.66 remains a hard rejection.

## Confirmed Android differential tests

The deterministic DEFECT/SLIMES_WEAK fixtures use the actual Android v0.111.0 game and full-auto execution. They assert the first played card, zero unmirrored types in the injected combat, and no unexpected replan. Where the fight reaches turn two, they additionally assert exact route-state reuse. Confirmed cases:

`ColdSnap`, `GunkUp`, `DoubleEnergy`, `ChargeBattery`, `FightThrough`, `Rainbow`, `Leap`, `FocusedStrike`, `SweepingBeam`, `Coolheaded`, `LightningRod`, `TeslaCoil`, `Buffer`, `HelixDrill`, `Sunder` (including a non-lethal first hit and in-combat cost reduction), `FlakCannon`, `Tempest`, `Null` against already-Weak enemies, `RocketPunch` both alone and after `GunkUp` generates a Status, `Shatter`, `Compact` with Fuel generation, `Chaos`, `MultiCast`, `Loop`, `Coolant`, `ConsumingShadow`, `Claw`, and `Feral` with a zero-energy Skill returning to hand.

2026-09-29: `Rebound` and `AllForOne` passed isolated BetterDefect + CombatSolver + RitsuLib v0.111.0 full-auto fixtures (`bd59-rebound-strict-20260929`, `bd59-all-for-one-strict-20260929`). Both asserted the first played card, zero unmirrored cards, exact turn-two route reuse, and zero unexpected replans. Native logs additionally recorded `Rebound` selecting `CLAW` from discard and placing it atop the draw pile; `AllForOne` selected `CLAW` and subsequently played it. These are basic selection-path checks, not exhaustive deck/combat-state coverage.

With six other installed mods enabled, even the previously passing `ColdSnap` control timed out awaiting the first solver result; the same two candidates also timed out in that mixed-mod profile. An additional `ColdSnap` control with only `EventSkip`, `SpireBank`, and `LoserEatDust` added to the three-mod fixture also timed out; that narrows but does not identify the interacting mod. After isolating the three-mod fixture, both candidates passed. The mixed-mod startup/search interaction remains unverified and must not be presented as full compatibility.

These fixtures do not prove every upgrade level, status interaction, enemy, relic, or enchantment combination. `TrashToTreasure` changes only captured cost/Innate model data in the reviewed BetterDefect source; its prior basic test did not force the AI to play it.

## Still protected in release builds (22)

`BD_RECURSION`, `SCRAPE`, `WHITE_NOISE`, `SUBROUTINE`, `SMOKESTACK`, `CREATIVE_AI`, `BD_STREAMLINE`, `UPROAR`, `FTL`, `ECHO_FORM`, `HELLO_WORLD`, `BARRAGE`, `BD_AUTO_SHIELDS`, `HYPERBEAM`, `BD_REINFORCED_BODY`, `SPINNER`, `BD_RECYCLE`, `BD_STATIC_DISCHARGE`, `STACK`, `STORM`, `ITERATION`, `BD_CONSUME`.

The desktop Steam game currently installed for development is v0.107.1, while this adapter targets v0.111.0. Running BetterDefect on desktop v0.107.1 can check its native card effects but cannot verify this v0.111.0 solver binary or its shadow-state differential semantics. Do not count such a desktop run as Android adapter acceptance.

Several of these use BetterDefect-owned card or power types, persistent per-turn counters, nested selections, or generated-card RNG. They require explicit prediction-state capture/forking and differential fixtures; removing them from the guard is not an adaptation.

## Test-install hygiene

The Android unattended request listener is compiled only with `CombatSolverMobileTest=true`; normal mobile builds omit it. Never publish or leave a test build on the player's phone. Restore the pre-test phone backup and saved mod profile after each test round.
