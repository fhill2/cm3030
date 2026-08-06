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
