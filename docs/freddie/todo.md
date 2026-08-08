Questions:
Do we want to simplify the block detection? right click to block will block a percentage of blows.
Should we use CC5 for both player & enemy and not use?

___
Module requirements:
WaveSpawner -> spawn + count deaths.

___

TODO:

make the enemy AI attack phase more intelligent:

- enemy tries to block player (and success chance can be customized)
- enemy moves around player
- change enemy swing time separate to player
- add flesh hit
- fix broken enemy materials

remove speed slow down on swing
revisit the collider detection.

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
