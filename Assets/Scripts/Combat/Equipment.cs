using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Sockets equipment prefabs (weapon, shield) onto the actor's hand bones
    /// at startup. All equipment prefabs are canonically oriented (weapons:
    /// blade on +Y; shields: face normal on +Z), so a single grip per kind
    /// positions them correctly in the hand.
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
        [Tooltip("Re-apply grips every frame during Play so def fine-tune offsets can be adjusted live.")]
        [SerializeField] private bool liveTuning;

        [Header("Hand Anchors")]
        [Tooltip("Child transform under the weapon socket marking where the weapon's anchor lands. Move it in the prefab to tune per character.")]
        [SerializeField] private Transform weaponHandAnchor;

        [Tooltip("Child transform under the shield socket marking where the shield's anchor lands. Move it in the prefab to tune per character.")]
        [SerializeField] private Transform shieldHandAnchor;

        private static readonly Vector3 WeaponGripRotation = new Vector3(0f, 90f, 280f);
        private static readonly Vector3 ShieldGripRotation = new Vector3(350f, 115.00001f, 90f);

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
            Vector3 anchor = Vector3.zero;
            Vector3 tweakRot = Vector3.zero;

            Weapon weapon = weaponInstance.GetComponent<Weapon>();
            if (weapon != null)
            {
                if (weapon.Anchor != null) anchor = weapon.Anchor.localPosition;
                if (weapon.Def != null) tweakRot = weapon.Def.RotationOffset;
            }

            Quaternion rot = Quaternion.Euler(WeaponGripRotation + tweakRot);
            weaponInstance.transform.localRotation = rot;
            Vector3 weaponHand = weaponHandAnchor != null ? weaponHandAnchor.localPosition : Vector3.zero;
            weaponInstance.transform.localPosition = weaponHand - (rot * anchor);
        }

        private void ApplyShieldOffset()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 tweakRot = Vector3.zero;

            Shield shield = shieldInstance.GetComponent<Shield>();
            if (shield != null)
            {
                if (shield.Anchor != null) anchor = shield.Anchor.localPosition;
                if (shield.Def != null) tweakRot = shield.Def.RotationOffset;
            }

            Quaternion rot = Quaternion.Euler(ShieldGripRotation + tweakRot);
            shieldInstance.transform.localRotation = rot;
            Vector3 shieldHand = shieldHandAnchor != null ? shieldHandAnchor.localPosition : Vector3.zero;
            shieldInstance.transform.localPosition = shieldHand - (rot * anchor);
        }
    }
}
