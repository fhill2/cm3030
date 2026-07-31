using UnityEngine;
using Game.Core;
using Game.Health;

namespace Game.Audio
{
    /// <summary>
    /// Centralised audio playback driven entirely by EventManagerScript events.
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
            EventManagerScript.OnDamageDealt += OnDamageDealt;
            EventManagerScript.OnEntityDied += OnEntityDied;
            EventManagerScript.OnFootstep += OnFootstep;
            EventManagerScript.OnJump += OnJump;
            EventManagerScript.OnLand += OnLand;
        }

        void OnDisable()
        {
            EventManagerScript.OnDamageDealt -= OnDamageDealt;
            EventManagerScript.OnEntityDied -= OnEntityDied;
            EventManagerScript.OnFootstep -= OnFootstep;
            EventManagerScript.OnJump -= OnJump;
            EventManagerScript.OnLand -= OnLand;
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

        private void OnFootstep()
        {
            if (footstepClip != null) sfxSource.PlayOneShot(footstepClip);
        }

        private void OnJump()
        {
            if (jumpClip != null) sfxSource.PlayOneShot(jumpClip);
        }

        private void OnLand()
        {
            if (landClip != null) sfxSource.PlayOneShot(landClip);
        }
    }
}
