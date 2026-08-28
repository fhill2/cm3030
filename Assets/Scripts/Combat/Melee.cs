using System.Collections;
using UnityEngine;
using Game.Shared;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    public class Melee : MonoBehaviour
    {
        private Weapon equipped;
        private WeaponCollider weaponCollider;
        [SerializeField] private Animator animator;
        private float lastAttackTime = float.NegativeInfinity;

        public event System.Action OnAttackStart;
        public event System.Action OnAttackEnd;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        private const float Pullback = 0.2f; // blade pulled back, not yet dangerous
        private const float UnarmedSpeed = 0.8f; // swing pace with fists, when no weapon is equipped

        // Whether a swing would actually start right now. Callers check this
        // before spending stamina, so clicks during the cooldown cost nothing.
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

        // The weapon is instantiated by Equipment in Start, so we can't cache
        // these in Awake — they're resolved on first use instead. A cached
        // reference that has left the hierarchy (thrown weapon, ejected gear)
        // is re-resolved so a freshly collected weapon is picked up.
        private void ResolveReferences()
        {
            if (equipped == null || !equipped.transform.IsChildOf(transform))
                equipped = GetComponentInChildren<Weapon>();
            if (weaponCollider == null || !weaponCollider.transform.IsChildOf(transform))
                weaponCollider = GetComponentInChildren<WeaponCollider>();
        }

        private IEnumerator SwingRoutine()
        {
            float speed = SwingSpeed;

            if (animator != null) animator.SetTrigger(AnimParams.Attack);
            EventManager.RaiseHit(new HitArgs(gameObject));
            OnAttackStart?.Invoke();

            // Pullback: blade drawn back, not yet dangerous.
            yield return new WaitForSeconds(Pullback);

            // Active swing: arm the blade for the remaining duration.
            if (weaponCollider != null) weaponCollider.BeginSwing();
            yield return new WaitForSeconds(speed - Pullback);
            if (weaponCollider != null) weaponCollider.EndSwing();

            OnAttackEnd?.Invoke();
        }
    }
}