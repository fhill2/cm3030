## How to create a Weapon or Shield prefab

### Weapon prefab (e.g. `Sword.prefab`)

- **WeaponDef**: right-click in `Assets/Equipment/Weapons/` → **Create → Equipment → Weapon** → rename (e.g. `Sword`) → set Damage + Speed in the Inspector. Assign to the Weapon component's **Def** field on the prefab.

1. In the Project window, navigate to `Assets/Equipment/Weapons/`.
2. Drag a mesh source (e.g. `MC_Sword_01.prefab` from The Modular Medieval Castle) into the scene Hierarchy.
3. Rename the instance to the weapon name (e.g. `Sword`).
4. Select the root → **Add Component**:
   - **Box Collider** → check **Is Trigger** → resize to cover the blade volume.
   - **Weapon Collider** (the hit-detection script).
   - **Weapon** (holds the WeaponDef reference) → set **Def** to the matching `.asset` file.
5. **Remove all colliders on child objects** — expand the hierarchy, find every LOD/mesh child, and remove or disable their Mesh Colliders. Only the root Box Collider should remain (this prevents the OverlapBox from catching mesh colliders instead of the tagged collider).
6. Drag the object from the Hierarchy into `Assets/Equipment/Weapons/` to save as a prefab (choose **Original Prefab**).
7. Delete the scene instance.

### Shield prefab (e.g. `Shield.prefab`)

- **ShieldDef**: right-click in `Assets/Equipment/Shields/` → **Create → Equipment → Shield** → rename (e.g. `Shield`). Assign to the Shield component's **Def** field on the prefab. Tune Position Offset + Rotation Offset live during Play (check **Live Tuning** on the Equipment component).

1. Navigate to `Assets/Equipment/Shields/`.
2. Drag a mesh source (e.g. `MC_Shields_01.prefab`) into the Hierarchy.
3. Rename to `Shield`.
4. Select the root → set the **Tag** dropdown to **`Shield`** (this is required — the WeaponCollider checks `CompareTag("Shield")` for block detection).
5. **Add Component**:
   - **Box Collider** → check **Is Trigger** → resize to cover the shield face.
   - **Shield Collider** (manages enable/disable when blocking).
   - **Shield** (holds the ShieldDef reference) → set **Def** to the matching `.asset` file.
6. **Remove all colliders on child objects** — same as weapons: expand the hierarchy, find every LOD/mesh child, remove their Mesh Colliders. Only the root Box Collider (tagged `Shield`) should remain.
7. Drag into `Assets/Equipment/Shields/` to save as a prefab (**Original Prefab**).
8. Delete the scene instance.

### Notes

It's better to have a larger collider box on the shield for better block detection
Adjust shield .def pos value so body does not overlap with the shield & collider
