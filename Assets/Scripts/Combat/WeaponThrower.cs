using UnityEngine;
using UnityEngine.InputSystem;
using Game.Audio;
using Game.Core;
using Game.Shared;

namespace Game.Combat
{
    public class WeaponThrower : MonoBehaviour
    {
        [Tooltip("Height above the player's origin the weapon leaves from.")]
        [SerializeField] private float throwHeight = 1.4f;

        [Tooltip("How far in front of the chest the weapon spawns, so it clears the player's own body.")]
        [SerializeField] private float throwForward = 0.6f;

        private Equipment equipment;
        private ActorAudio actorAudio;
        private StaminaSystem stamina;
        private Animator animator;

        void Awake()
        {
            equipment = GetComponent<Equipment>();
            actorAudio = GetComponent<ActorAudio>();
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

            TryThrow();
        }

        void TryThrow()
        {
            GameObject weapon = equipment != null ? equipment.WeaponInstance : null;
            if (weapon == null) return;

            if (animator != null) animator.SetTrigger(AnimParams.Throw);
            if (actorAudio != null) actorAudio.PlayEffort();

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

            Debug.Log($"[WeaponThrower] Threw {weapon.name} — unarmed until collected or bought.");
        }
    }
}
