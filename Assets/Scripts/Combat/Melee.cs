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

        // Whether a swing would actually start right now. Callers check this
        // before spending stamina, so clicks during the cooldown cost nothing.
        public bool CanAttack
        {
            get
            {
                ResolveReferences();
                if (equipped == null || equipped.Def == null) return false;

                return Time.time - lastAttackTime >= equipped.Def.Speed;
            }
        }

        public bool TryAttack()
        {
            if (!CanAttack) return false;

            lastAttackTime = Time.time;
            StartCoroutine(SwingRoutine());
            return true;
        }

        // The weapon is instantiated by Equipment in Start, so we can't cache
        // these in Awake — they're resolved on first use instead.
        private void ResolveReferences()
        {
            if (equipped == null) equipped = GetComponentInChildren<Weapon>();
            if (weaponCollider == null) weaponCollider = GetComponentInChildren<WeaponCollider>();
        }

        private IEnumerator SwingRoutine()
        {
            var def = equipped.Def;

            if (animator != null) animator.SetTrigger(AnimParams.Attack);
            EventManager.RaiseHit(new HitArgs(gameObject));
            OnAttackStart?.Invoke();

            // Pullback: blade drawn back, not yet dangerous.
            yield return new WaitForSeconds(Pullback);

            // Active swing: arm the blade for the remaining duration.
            if (weaponCollider != null) weaponCollider.BeginSwing();
            yield return new WaitForSeconds(def.Speed - Pullback);
            if (weaponCollider != null) weaponCollider.EndSwing();

            OnAttackEnd?.Invoke();
        }
    }
}