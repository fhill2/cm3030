using System.Collections;
using UnityEngine;
using Game.Shared;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    // Runs a swing: triggers the animation, arms the blade partway through,
    // then disarms it. Shared by the player and the enemies.
    public class Melee : MonoBehaviour
    {
        private const float Pullback = 0.2f;      // blade drawn back, not yet dangerous
        private const float UnarmedSpeed = 0.8f;  // swing pace with fists

        [SerializeField] private Animator animator;

        private Weapon equipped;
        private WeaponCollider weaponCollider;
        private float lastAttackTime = float.NegativeInfinity;

        public event System.Action OnAttackStart;
        public event System.Action OnAttackEnd;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        // Checked before spending stamina, so clicks during the cooldown
        // cost nothing.
        public bool CanAttack
        {
            get
            {
                ResolveReferences();
                if (equipped != null && equipped.Def == null) return false;

                return Time.time - lastAttackTime >= SwingSpeed;
            }
        }

        public bool TryAttack()
        {
            if (!CanAttack) return false;

            lastAttackTime = Time.time;
            StartCoroutine(SwingRoutine());
            return true;
        }

        public float CooldownDuration => SwingSpeed;

        public float CooldownRemaining => Mathf.Max(0f, lastAttackTime + SwingSpeed - Time.time);

        private float SwingSpeed
        {
            get
            {
                ResolveReferences();
                return equipped != null && equipped.Def != null ? equipped.Def.Speed : UnarmedSpeed;
            }
        }

        // Equipment spawns the weapon in Start, so these can't be cached in
        // Awake. A reference that has left the hierarchy (thrown or ejected
        // gear) is re-resolved, so a freshly collected weapon is picked up.
        private void ResolveReferences()
        {
            if (equipped == null || !equipped.transform.IsChildOf(transform))
                equipped = GetComponentInChildren<Weapon>();
            if (weaponCollider == null || !weaponCollider.transform.IsChildOf(transform))
                weaponCollider = GetComponentInChildren<WeaponCollider>();
        }

        private IEnumerator SwingRoutine()
        {
            float swingDuration = SwingSpeed;

            if (animator != null) animator.SetTrigger(AnimParams.Attack);
            EventManager.RaiseHit(new HitArgs(gameObject));
            OnAttackStart?.Invoke();

            yield return new WaitForSeconds(Pullback);

            // Blade is live for the rest of the swing.
            if (weaponCollider != null) weaponCollider.BeginSwing();
            yield return new WaitForSeconds(swingDuration - Pullback);
            if (weaponCollider != null) weaponCollider.EndSwing();

            OnAttackEnd?.Invoke();
        }
    }
}