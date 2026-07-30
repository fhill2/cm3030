using UnityEngine;
using Game.Core;
using Game.Health;
using Game.Movement;

namespace Game.Audio
{
    /// <summary>
    /// Centralised audio playback driven entirely by GameHub events.
    /// Subscribes to notifications from other modules; never references
    /// HealthSystem, PlayerMovement, or any other module class directly.
    ///
    /// Setup: add to any GameObject with an AudioSource component.
    /// Assign clips in the Inspector. The module handles the rest.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AudioManager : MonoBehaviour
    {
        private AudioSource sfxSource;

        [Header("Clip Assignments")]
        [SerializeField] private AudioClip hurtClip;
        [SerializeField] private AudioClip deathClip;
        [SerializeField] private AudioClip footstepClip;
        [SerializeField] private AudioClip jumpClip;
        [SerializeField] private AudioClip landClip;

        void Awake()
        {
            sfxSource = GetComponent<AudioSource>();
        }

        void OnEnable()
        {
            GameHub.Subscribe<DamageDealt>(OnDamageDealt);
            GameHub.Subscribe<EntityDied>(OnEntityDied);
            GameHub.Subscribe<Footstep>(OnFootstep);
            GameHub.Subscribe<Jump>(OnJump);
            GameHub.Subscribe<Land>(OnLand);
        }

        void OnDisable()
        {
            GameHub.Unsubscribe<DamageDealt>(OnDamageDealt);
            GameHub.Unsubscribe<EntityDied>(OnEntityDied);
            GameHub.Unsubscribe<Footstep>(OnFootstep);
            GameHub.Unsubscribe<Jump>(OnJump);
            GameHub.Unsubscribe<Land>(OnLand);
        }

        // ── Event handlers ─────────────────────────────────────

        private void OnDamageDealt(DamageDealt e)
        {
            if (hurtClip != null) sfxSource.PlayOneShot(hurtClip);
        }

        private void OnEntityDied(EntityDied e)
        {
            if (deathClip != null) sfxSource.PlayOneShot(deathClip);
        }

        private void OnFootstep(Footstep e)
        {
            if (footstepClip != null) sfxSource.PlayOneShot(footstepClip);
        }

        private void OnJump(Jump e)
        {
            if (jumpClip != null) sfxSource.PlayOneShot(jumpClip);
        }

        private void OnLand(Land e)
        {
            if (landClip != null) sfxSource.PlayOneShot(landClip);
        }
    }
}
