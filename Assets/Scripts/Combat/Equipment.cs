using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Sockets equipment prefabs (weapon, shield) onto the actor's hand bones
    /// at startup. Offsets are read from the WeaponDef / ShieldDef .asset files
    /// so each weapon/shield carries its own grip position.
    /// Enable Live Tuning during Play to adjust offsets in real-time.
    /// </summary>
    public class Equipment : MonoBehaviour
    {
        [Header("Weapon")]
        [Tooltip("Weapon prefab to instantiate on the weapon hand at startup.")]
        [SerializeField] private GameObject weaponPrefab;
        [Tooltip("The hand bone to parent the weapon to.")]
        [SerializeField] private Transform weaponSocket;

        [Header("Shield")]
        [Tooltip("Shield prefab to instantiate on the shield hand at startup (optional).")]
        [SerializeField] private GameObject shieldPrefab;
        [Tooltip("The hand bone to parent the shield to.")]
        [SerializeField] private Transform shieldSocket;

        [Header("Tuning")]
        [Tooltip("Re-read offsets from the .asset every frame for live tuning during Play. Uncheck for production.")]
        [SerializeField] private bool liveTuning;

        private GameObject weaponInstance;
        private GameObject shieldInstance;

        // Allow external code (e.g. WaveSpawner) to override the prefabs
        // before Start() loads them.
        public GameObject WeaponPrefab { get => weaponPrefab; set => weaponPrefab = value; }
        public GameObject ShieldPrefab { get => shieldPrefab; set => shieldPrefab = value; }

        public GameObject WeaponInstance => weaponInstance;
        public GameObject ShieldInstance => shieldInstance;

        public void EquipWeapon(GameObject prefab)
        {
            if (prefab == null || weaponSocket == null) return;

            if (weaponInstance != null) Destroy(weaponInstance);
            weaponPrefab = prefab;
            weaponInstance = Instantiate(prefab, weaponSocket);
            ApplyWeaponOffset();
        }

        public void EquipShield(GameObject prefab)
        {
            if (prefab == null || shieldSocket == null) return;

            if (shieldInstance != null) Destroy(shieldInstance);
            shieldPrefab = prefab;
            shieldInstance = Instantiate(prefab, shieldSocket);
            ApplyShieldOffset();
        }

        void Start()
        {
            if (weaponPrefab != null && weaponSocket != null)
            {
                weaponInstance = Instantiate(weaponPrefab, weaponSocket);
                ApplyWeaponOffset();
            }

            if (shieldPrefab != null && shieldSocket != null)
            {
                shieldInstance = Instantiate(shieldPrefab, shieldSocket);
                ApplyShieldOffset();
            }
        }

        void LateUpdate()
        {
            if (!liveTuning) return;
            if (weaponInstance != null) ApplyWeaponOffset();
            if (shieldInstance != null) ApplyShieldOffset();
        }

        private void ApplyWeaponOffset()
        {
            var weapon = weaponInstance.GetComponent<Weapon>();
            if (weapon != null && weapon.Def != null)
            {
                weaponInstance.transform.localPosition = weapon.Def.PositionOffset;
                weaponInstance.transform.localRotation = Quaternion.Euler(weapon.Def.RotationOffset);
            }
        }

        private void ApplyShieldOffset()
        {
            var shield = shieldInstance.GetComponent<Shield>();
            if (shield != null && shield.Def != null)
            {
                shieldInstance.transform.localPosition = shield.Def.PositionOffset;
                shieldInstance.transform.localRotation = Quaternion.Euler(shield.Def.RotationOffset);
            }
        }
    }
}
