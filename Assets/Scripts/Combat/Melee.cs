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
        public bool TryAttack()
        {
            if (equipped == null) equipped = GetComponentInChildren<Weapon>();
            if (weaponCollider == null) weaponCollider = GetComponentInChildren<WeaponCollider>();
            if (equipped == null || equipped.Def == null) return false;

            if (Time.time - lastAttackTime < equipped.Def.Speed) return false;

            lastAttackTime = Time.time;
            StartCoroutine(SwingRoutine());
            return true;
        }

        private IEnumerator SwingRoutine()
        {
            var def = equipped.Def;

            if (animator != null) animator.SetTrigger(AnimParams.Attack);
            EventManager.RaiseHit(new HitArgs(gameObject));
            OnAttackStart?.Invoke();

            // The blade is armed for the full swing duration (speed).
            // Per-swing dedup in WeaponCollider ensures each target is hit once.
            if (weaponCollider != null) weaponCollider.BeginSwing();
            yield return new WaitForSeconds(def.Speed);
            if (weaponCollider != null) weaponCollider.EndSwing();

            OnAttackEnd?.Invoke();
        }
    }
}
