Module requirements:
WaveSpawner -> spawn + count deaths.

Decoupled the WaveManager modules from the GameStateMachine:

- add CurrentWave to GameStateChangedEvent GameStateMachine -> WaveManager, so the WaveManager doesn't access this state from the GameStateMachine reference.
- added OnWaveCleared event to use WaveManager -> GameStateMachine, so the WaveManager doesn't change the state of GameStateMachine. Instead it raises OnWaveCleared which GameStateMachine subscribes to.

- WaveSpawner holds a single reference to the parent GameObject containing the spawn points, instead of setting each reference to the spawn points individually.
- removed GameLoop/ChaseState.cs - it's now redundant as the enemy AI module provides basic Patrol / Chase / Attack mechanics. Also removed "player" reference in WaveSpawner as this was the only logic using this reference. Now the WaveSpawner spawns the enemy, and the enemy AI owns movement for the enemy after it's been spawned. If we want the enemy to go directly to Chase state after its spawned, we configure "Initial State" on the Npc FSM script.

Moved GameLoop/StateDebugKeys.cs -> Testing/De

___

TODO:

make the enemy AI attack phase more intelligent:

- enemy tries to block player (and success chance can be customized)
- enemy moves around player
- change enemy swing time separate to player

remove speed slow down on swing
revisit the collider detection.

- Setup prefabs and test scenes: freddie_environment.unity -> environment + player
- How to make the run animations look more natural. Do not know if this is due to the model's rig or the animation.
- We can't use the Cinemachine camera, 3rd party script.

___
Animation Clip Settings:
Locomotion Clips (Walk, Run, Idle)
Root Transform Rotation: Bake Into Pose ON, Based Upon Body Orientation, Offset -40
Root Transform Position (Y): Bake Into Pose ON, Based Upon Original, Offset 0
Root Transform Position (XZ): Bake Into Pose OFF, Based Upon Original, Offset 0
Jump Clip``
Root Transform Rotation: Bake Into Pose ON, Based Upon Body Orientation, Offset -40
Root Transform Position (Y): Bake Into Pose OFF, Based Upon Feet, Offset 0
Root Transform Position (XZ): Bake Into Pose OFF, Based Upon Original, Offset 0
Falling Clip
Root Transform Rotation: Bake Into Pose ON, Based Upon Body Orientation, Offset -40
Root Transform Position (Y): Bake Into Pose OFF, Based Upon Original, Offset 0
Root Transform Position (XZ): Bake Into Pose OFF, Based Upon Original, Offset 0
Death Clip
Root Transform Rotation: Bake Into Pose ON, Based Upon Body Orientation, Offset -40
Root Transform Position (Y): Bake Into Pose ON, Based Upon Original, Offset 0
Root Transform Position (XZ): Bake Into Pose OFF, Based Upon Original, Offset 0

___
