using UnityEngine;
using Game.Movement;

namespace Game.Health
{
    /// <summary>
    /// PLACEHOLDER stamina model for the UI stamina bar (design sheet row 18).
    /// Drains while the player holds block and regenerates otherwise, with a
    /// short lockout after hitting empty so it doesn't flicker straight back up.
    ///
    /// This is a stand-in for Alessio's real stamina/stun system (row 10:
    /// "Stamina for hits and blocks: implement stun status when stamina = 0").
    /// PlayerUI only reads CurrentStamina/MaxStamina, so once row 10 lands this
    /// component can be replaced (or extended with the stun behavior) without
    /// touching the UI code.
    /// </summary>
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
