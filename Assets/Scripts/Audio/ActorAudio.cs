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
        private const float SwingDelay = 0.03f;

        private static AudioClip[] s_effortClips;
        private static AudioClip[] s_damageClips;
        private static AudioClip[] s_deathClips;
        private static AudioClip[] s_swingClips;

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
