using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Core;
using Game.Shared;

namespace Game.Combat
{
    public class WeaponThrower : MonoBehaviour
    {
        [Tooltip("Minimum seconds between throws.")]
        [SerializeField] private float cooldown = 20f;

        [Tooltip("Height above the player's origin the weapon leaves from.")]
        [SerializeField] private float throwHeight = 1.4f;

        [Tooltip("How far in front of the chest the weapon spawns, so it clears the player's own body.")]
        [SerializeField] private float throwForward = 0.6f;

        [Tooltip("Seconds after the throw before a fresh weapon is drawn.")]
        [SerializeField] private float rearmDelay = 3f;

        public float CooldownDuration => cooldown;

        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);

        public void SetCooldown(float seconds)
        {
            cooldown = Mathf.Max(0f, seconds);
            readyAt = float.NegativeInfinity;
        }

        private Equipment equipment;
        private StaminaSystem stamina;
        private Animator animator;
        private float readyAt = float.NegativeInfinity;

        void Awake()
        {
            equipment = GetComponent<Equipment>();
            stamina = GetComponent<StaminaSystem>();
            animator = GetComponentInChildren<Animator>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb.rKey.wasPressedThisFrame) return;
            if (PlayerInputLock.InputLocked) return;
            if (stamina != null && !stamina.CanAct) return;
            if (Time.time < readyAt) return;

            TryThrow();
        }

        void TryThrow()
        {
            GameObject weapon = equipment != null ? equipment.WeaponInstance : null;
            if (weapon == null) return;

            readyAt = Time.time + cooldown;

            if (animator != null) animator.SetTrigger(AnimParams.Throw);

            GameObject prefab = equipment.WeaponPrefab;
            Weapon defHolder = weapon.GetComponent<Weapon>();
            WeaponDef def = defHolder != null ? defHolder.Def : null;

            Camera camera = Camera.main;
            Vector3 aim = camera != null ? camera.transform.forward : transform.forward;
            aim.y = 0f;
            if (aim.sqrMagnitude < 0.001f)
            {
                aim = transform.forward;
                aim.y = 0f;
            }
            Vector3 direction = aim.normalized;
            Vector3 origin = transform.position
                             + Vector3.up * throwHeight
                             + direction * throwForward;

            foreach (var wc in weapon.GetComponentsInChildren<WeaponCollider>())
                wc.enabled = false;

            weapon.transform.SetParent(null);
            weapon.transform.position = origin;
            weapon.transform.rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(90f, 0f, 0f);
            equipment.Detach(weapon);

            ThrownWeapon thrown = weapon.GetComponent<ThrownWeapon>();
            if (thrown == null) thrown = weapon.AddComponent<ThrownWeapon>();
            thrown.Launch(gameObject, direction);

            StartCoroutine(Rearm(prefab, def));
        }

        private IEnumerator Rearm(GameObject prefab, WeaponDef def)
        {
            yield return new WaitForSeconds(rearmDelay);

            if (prefab == null) yield break;

            Debug.Log($"[WeaponThrower] Rearming after {rearmDelay}s");

            equipment.EquipWeapon(prefab);

            if (def != null && equipment.WeaponInstance != null)
            {
                Weapon fresh = equipment.WeaponInstance.GetComponent<Weapon>();
                if (fresh != null) fresh.Def = def;
            }
        }
    }
}
