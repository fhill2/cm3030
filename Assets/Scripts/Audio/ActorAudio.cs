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
        private static readonly string EffortPath = "Grunts/effort";
        private static readonly string DamagePath = "Grunts/damage";
        private static readonly string DeathPath  = "Grunts/death";

        private static AudioClip[] s_effortClips;
        private static AudioClip[] s_damageClips;
        private static AudioClip[] s_deathClips;

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
        }

        void OnEnable()
        {
            EventManager.OnDamage += HandleDamage;
            EventManager.OnDeath += HandleDeath;
        }

        void OnDisable()
        {
            EventManager.OnDamage -= HandleDamage;
            EventManager.OnDeath -= HandleDeath;
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

        // ── Direct-call API ────────────────────────────────────

        public void PlayEffort()   => Play(RandomClip(s_effortClips));

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
