using System.Collections;
using UnityEngine;
using Game.Core;
using Game.Health;

namespace Game.Audio
{
    /// <summary>
    /// Per-entity audio component. Owns one AudioSource.
    /// Grunt clips are auto-loaded from Resources/Grunts/{effort,damage,death}.
    /// Combat grunts are driven by EventManager OnDamage/OnDeath, filtered
    /// so only events belonging to this entity are played.
    /// Movement SFX (jump/land/footstep) are triggered via direct calls.
    /// Effort grunts are triggered explicitly by callers (e.g. CombatDemoController).
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ActorAudio : MonoBehaviour
    {
        private static readonly string EffortPath  = "Grunts/effort";
        private static readonly string DamagePath  = "Grunts/damage";
        private static readonly string DeathPath   = "Grunts/death";
        private static readonly string WeaponPath  = "Weapon/swing";
        // The cuts folder holds individual steps; the parent folder holds the
        // full uncut recordings, which are too long to use as one-shots.
        private static readonly string FootstepPath = "Footsteps/cuts";
        private const float SwingDelay = 0.03f;

        private static AudioClip[] s_effortClips;
        private static AudioClip[] s_damageClips;
        private static AudioClip[] s_deathClips;
        private static AudioClip[] s_swingClips;
        private static AudioClip[] s_footstepClips;

        private AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            LoadClips();
        }

        static void LoadClips()
        {
            if (s_effortClips != null) return;
            s_effortClips = Resources.LoadAll<AudioClip>(EffortPath);
            s_damageClips = Resources.LoadAll<AudioClip>(DamagePath);
            s_deathClips  = Resources.LoadAll<AudioClip>(DeathPath);
            s_swingClips  = Resources.LoadAll<AudioClip>(WeaponPath);
            s_footstepClips = Resources.LoadAll<AudioClip>(FootstepPath);
        }

        void OnEnable()
        {
            EventManager.OnDamage += HandleDamage;
            EventManager.OnDeath += HandleDeath;
            EventManager.OnHit += HandleHit;
        }

        void OnDisable()
        {
            EventManager.OnDamage -= HandleDamage;
            EventManager.OnDeath -= HandleDeath;
            EventManager.OnHit -= HandleHit;
        }

        // ── Event-driven combat audio ──────────────────────────

        void HandleDamage(DamageArgs e)
        {
            if (e.Target == gameObject)
                Play(RandomClip(s_damageClips));
        }

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity == gameObject)
                Play(RandomClip(s_deathClips));
        }

        void HandleHit(HitArgs e)
        {
            if (e.Entity == gameObject)
            {
                Play(RandomClip(s_effortClips));
                StartCoroutine(SwingRoutine());
            }
        }

        // ── Direct-call API ────────────────────────────────────

        public void PlayEffort()   => Play(RandomClip(s_effortClips));

        public void PlaySwing() => StartCoroutine(SwingRoutine());

        public void PlayFootstep() => Play(RandomClip(s_footstepClips));

        // Resources has no dedicated jump/land clips yet. An effort grunt reads
        // as exertion on take-off and a footstep reads as the landing impact;
        // swap these for real clips once they exist.
        public void PlayJump() => Play(RandomClip(s_effortClips));

        public void PlayLand() => Play(RandomClip(s_footstepClips));

        private IEnumerator SwingRoutine()
        {
            yield return new WaitForSeconds(SwingDelay);
            Play(RandomClip(s_swingClips));
        }

        private static AudioClip RandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        private void Play(AudioClip clip)
        {
            if (clip != null && source != null)
                source.PlayOneShot(clip);
        }
    }
}
