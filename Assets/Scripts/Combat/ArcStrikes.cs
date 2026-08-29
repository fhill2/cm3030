using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Audio;
using Game.Core;
using Game.Movement;
using Game.Shared;

namespace Game.Combat
{
    public class ArcStrikes : MonoBehaviour
    {
        [Tooltip("Reach of the knockback arc in metres.")]
        [SerializeField] private float range = 4f;

        [Tooltip("Total width of the arc in degrees. 90 hits everything within 45 degrees either side of the player's facing.")]
        [SerializeField] private float arcAngle = 90f;

        [Tooltip("Height above the player's origin the arc is measured from.")]
        [SerializeField] private float arcHeight = 1.2f;

        [Tooltip("Seconds after a knockback before it can be used again.")]
        [SerializeField] private float cooldown = 10f;

        [Tooltip("Chance (0-1) that each enemy in the arc gets knocked back.")]
        [SerializeField, Range(0f, 1f)] private float knockChance = 0.6666f;

        [Tooltip("Impact clip under Resources/ played when the knockback fires.")]
        [SerializeField] private string slamClip = "Knockback/FF_IE_fx_slam_smash";

        [Tooltip("Colour of the ground ripple VFX.")]
        [SerializeField] private Color rippleColor = new Color(0.75f, 0.85f, 1f, 1f);

        [Tooltip("Seconds the ground ripple takes to expand to the arc's full range.")]
        [SerializeField] private float rippleDuration = 0.45f;

        public float CooldownDuration => cooldown;

        public float CooldownRemaining => Mathf.Max(0f, readyAt - Time.time);

        private Melee melee;
        private StaminaSystem stamina;
        private PlayerMovement movement;
        private Animator animator;
        private int knockbackLayer = -1;
        private bool knockbackLayerHot;
        private float readyAt = float.NegativeInfinity;

        void Awake()
        {
            melee = GetComponent<Melee>();
            stamina = GetComponent<StaminaSystem>();
            movement = GetComponent<PlayerMovement>();
            animator = GetComponentInChildren<Animator>();

            if (animator != null)
            {
                knockbackLayer = animator.GetLayerIndex("Knockback");
                if (knockbackLayer >= 0) animator.SetLayerWeight(knockbackLayer, 0f);
            }
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            ReleaseKnockbackLayer();

            if (PlayerInputLock.InputLocked) return;
            if (movement != null && !movement.ControlEnabled) return;
            if (stamina != null && !stamina.CanAct) return;

            if (kb.qKey.wasPressedThisFrame) KnockbackStrike();
        }

        void ReleaseKnockbackLayer()
        {
            if (!knockbackLayerHot || knockbackLayer < 0 || animator == null) return;

            if (animator.GetCurrentAnimatorStateInfo(knockbackLayer).IsName("Empty") &&
                animator.GetNextAnimatorStateInfo(knockbackLayer).shortNameHash == 0)
            {
                animator.SetLayerWeight(knockbackLayer, 0f);
                knockbackLayerHot = false;
            }
        }

        void KnockbackStrike()
        {
            if (Time.time < readyAt) return;
            if (melee != null && melee.CooldownRemaining > 0f) return;
            if (stamina != null && !stamina.TrySpendAttack()) return;

            readyAt = Time.time + cooldown;

            GroundRipple.Spawn(RippleOrigin(), range, rippleDuration, rippleColor);

            AudioClip slam = Resources.Load<AudioClip>(slamClip);
            if (slam != null)
                OneShotAudio.Play2D(slam, transform.position);
            else
                Debug.LogWarning($"[ArcStrikes] No clip at Resources/{slamClip}");

            if (knockbackLayer >= 0 && animator != null)
            {
                animator.SetLayerWeight(knockbackLayer, 1f);
                knockbackLayerHot = true;
            }

            if (animator != null) animator.SetTrigger(AnimParams.Knockback);

            Vector3 origin = transform.position + Vector3.up * arcHeight;

            foreach (IDamageable target in Probe())
            {
                Component component = target as Component;
                if (component == null) continue;

                if (Random.value > knockChance) continue;

                Knockback knockback = component.GetComponentInParent<Knockback>();
                if (knockback == null) knockback = component.gameObject.AddComponent<Knockback>();
                knockback.Launch(origin);
            }
        }

        private Vector3 RippleOrigin()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f,
                    ~0, QueryTriggerInteraction.Ignore) &&
                !CombatProbe.IsIgnored(hit.transform, transform))
                return hit.point;

            return transform.position;
        }

        private List<IDamageable> Probe()
        {
            Vector3 origin = transform.position + Vector3.up * arcHeight;
            return CombatProbe.Arc(origin, transform.forward, range, arcAngle * 0.5f, transform);
        }
    }
}
