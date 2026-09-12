using UnityEngine;
using Game.Movement;

namespace Game.Health
{
    // Drains stamina while the player blocks and regenerates it otherwise,
    // with a short pause after hitting empty so the bar doesn't flicker.
    [RequireComponent(typeof(PlayerMovement))]
    public class PlayerStamina : MonoBehaviour
    {
        [SerializeField] private float maxStamina = 100f;
        [Tooltip("Stamina drained per second while holding block.")]
        [SerializeField] private float drainPerSecond = 25f;
        [Tooltip("Stamina regenerated per second while not blocking.")]
        [SerializeField] private float regenPerSecond = 15f;
        [Tooltip("Regen pauses this long after stamina hits 0, before it starts recovering again.")]
        [SerializeField] private float regenDelayAfterEmpty = 0.75f;

        private PlayerMovement movement;
        private float regenLockTimer;

        public float MaxStamina => maxStamina;
        public float CurrentStamina { get; private set; }

        void Awake()
        {
            movement = GetComponent<PlayerMovement>();
            CurrentStamina = maxStamina;
        }

        void Update()
        {
            bool blocking = movement != null && movement.IsBlocking;

            if (blocking)
            {
                CurrentStamina -= drainPerSecond * Time.deltaTime;
                if (CurrentStamina <= 0f) regenLockTimer = regenDelayAfterEmpty;
            }
            else if (regenLockTimer > 0f)
            {
                regenLockTimer -= Time.deltaTime;
            }
            else
            {
                CurrentStamina += regenPerSecond * Time.deltaTime;
            }

            CurrentStamina = Mathf.Clamp(CurrentStamina, 0f, maxStamina);
        }
    }
}