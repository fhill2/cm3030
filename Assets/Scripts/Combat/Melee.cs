using System.Collections;
using UnityEngine;
using Game.Shared;
using Game.Core;
using Game.Health;

namespace Game.Combat
{
    public class Melee : MonoBehaviour
    {
        private Weapon equipped;                      // the equipped weapon (holds the WeaponDef)
        private WeaponCollider weaponCollider;        // the blade collider, armed during the active window
        [SerializeField] private Animator animator;
        private float lastAttackTime = float.NegativeInfinity;

        // Local hooks so actors can react to a swing without owning any timing
        // (e.g. the player slows movement for the duration of the swing).
        public event System.Action OnAttackStart;
        public event System.Action OnAttackEnd;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        public bool TryAttack()
        {
            // Lazily resolve the equipped weapon + blade collider (children of this actor).
            if (equipped == null) equipped = GetComponentInChildren<Weapon>();
            if (weaponCollider == null) weaponCollider = GetComponentInChildren<WeaponCollider>();
            if (equipped == null || equipped.Def == null) return false;

            float cooldown = equipped.Def.AttackCooldown;
            if (Time.time - lastAttackTime < cooldown) return false;

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

            yield return new WaitForSeconds(def.Windup);

            // Active window: arm the blade collider so contact deals damage.
            if (weaponCollider != null) weaponCollider.BeginSwing();
            yield return new WaitForSeconds(def.ActiveWindow);
            if (weaponCollider != null) weaponCollider.EndSwing();

            OnAttackEnd?.Invoke();
        }
    }
}
