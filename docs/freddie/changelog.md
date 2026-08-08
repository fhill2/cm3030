## 7th - 8th August 2026

move SwingTester.cs to Testing/Scripts/
move WeaponGizmo.cs to Testing/Scripts/
create a UI healthplate for enemies

Today I worked on getting a playtest working based on all of our recent changes we've merged into main branch:

Decoupled the WaveManager modules from the GameStateMachine:

- add CurrentWave to GameStateChangedEvent GameStateMachine -> WaveManager, so the WaveManager doesn't access this state from the GameStateMachine reference.
- added OnWaveCleared event to use WaveManager -> GameStateMachine, so the WaveManager doesn't change the state of GameStateMachine. Instead it raises OnWaveCleared which GameStateMachine subscribes to.

- WaveSpawner holds a single reference to the parent GameObject containing the spawn points, instead of setting each reference to the spawn points individually.
- removed GameLoop/ChaseState.cs - it's now redundant as the enemy AI module provides basic Patrol / Chase / Attack mechanics. Also removed "player" reference in WaveSpawner as this was the only logic using this reference. Now the WaveSpawner spawns the enemy, and the enemy AI owns movement for the enemy after it's been spawned. If we want the enemy to go directly to Chase state after its spawned, we configure "Initial State" on the Npc FSM script.
- removed Enemy/EnemyHealth.cs, this file was boilerplate and I added it before alessio's health system was completed. Moved alessio's GameLoop/EnemyHealth.cs -> Enemy/EnemyHealth.cs.

Attack System:

- Unified the melee attack system between player and enemy. Before, there were 2 different systems for the player & enemy, now each share the base attack class Combat/Melee.cs
- create sword, shield, round shield, axe, prefabs to use with the equipment loader.
- WeaponOverride ShieldOverride added to Wave.asset - allows loading different weapons and shields on the enemies

UI:

- PlayerUI Module: shows Player health bar screen
- EnemyUI Module: shows enemy health as nameplates above each enemy.

Moved testing only code to Testing/ directory:
GameLoop/StateDebugKeys.cs -> Testing/Scripts/StateDebugKeys.cs

---
