using UnityEngine;

namespace Game.Combat
{
    // Spawns the weapon and shield prefabs onto the actor's hand bones at startup. 
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

        // Settable so WaveSpawner can override the prefabs before Start.
        public GameObject WeaponPrefab { get => weaponPrefab; set => weaponPrefab = value; }
        public GameObject ShieldPrefab { get => shieldPrefab; set => shieldPrefab = value; }

        public GameObject WeaponInstance => weaponInstance;
        public GameObject ShieldInstance => shieldInstance;

        public void Detach(GameObject instance)
        {
            if (weaponInstance == instance) weaponInstance = null;
            if (shieldInstance == instance) shieldInstance = null;
        }

        public void EquipWeapon(GameObject prefab)
        {
            if (prefab == null || weaponSocket == null) return;

            if (weaponInstance != null) Destroy(weaponInstance);
            weaponPrefab = prefab;
            weaponInstance = Instantiate(prefab, weaponSocket);
            ApplyWeaponOffset();
        }

        // Used when picking a weapon up off the ground, so the existing object
        // is kept rather than a fresh copy spawned.
        public void TakeWeaponInstance(GameObject instance)
        {
            if (instance == null || weaponSocket == null) return;

            if (weaponInstance != null) Destroy(weaponInstance);
            weaponInstance = instance;
            instance.transform.SetParent(weaponSocket, false);

            foreach (WeaponCollider collider in instance.GetComponentsInChildren<WeaponCollider>())
                collider.enabled = true;

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

        // Position works back from the anchor point on the weapon, so the grip
        // lands in the hand whatever the model's own origin is.
        private void ApplyWeaponOffset()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 rotationTweak = Vector3.zero;

            Weapon weapon = weaponInstance.GetComponent<Weapon>();
            if (weapon != null)
            {
                if (weapon.Anchor != null) anchor = weapon.Anchor.localPosition;
                if (weapon.Def != null) rotationTweak = weapon.Def.RotationOffset;
            }

            Quaternion gripRotation = Quaternion.Euler(WeaponGripRotation + rotationTweak);
            weaponInstance.transform.localRotation = gripRotation;
            Vector3 weaponHand = weaponHandAnchor != null ? weaponHandAnchor.localPosition : Vector3.zero;
            weaponInstance.transform.localPosition = weaponHand - (gripRotation * anchor);
        }

        private void ApplyShieldOffset()
        {
            Vector3 anchor = Vector3.zero;
            Vector3 rotationTweak = Vector3.zero;

            Shield shield = shieldInstance.GetComponent<Shield>();
            if (shield != null)
            {
                if (shield.Anchor != null) anchor = shield.Anchor.localPosition;
                if (shield.Def != null) rotationTweak = shield.Def.RotationOffset;
            }

            Quaternion gripRotation = Quaternion.Euler(ShieldGripRotation + rotationTweak);
            shieldInstance.transform.localRotation = gripRotation;
            Vector3 shieldHand = shieldHandAnchor != null ? shieldHandAnchor.localPosition : Vector3.zero;
            shieldInstance.transform.localPosition = shieldHand - (gripRotation * anchor);
        }
    }
}