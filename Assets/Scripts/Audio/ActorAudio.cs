using System.Collections;
using UnityEngine;
using Game.Core;
using Game.Health;
using Game.Shared;

namespace Game.Audio
{
    // Per-entity audio. Clips are loaded from Resources once and shared by every actor. 
    [RequireComponent(typeof(AudioSource))]
    public class ActorAudio : MonoBehaviour
    {
        private static readonly string EffortPath  = "Grunts/effort";
        private static readonly string DamagePath  = "Grunts/damage";
        private static readonly string DeathPath   = "Grunts/death";
        private static readonly string FleshPath   = "Grunts/flesh";
        private static readonly string FleshSplatPath = "Grunts/flesh_splat";
        private static readonly string WeaponPath  = "Weapon/swing";
        private static readonly string DropPath    = "Weapon/drop";
        private static readonly string UnequipPath = "Weapon/unequip";
        private static readonly string BlockPath   = "Shield/hit";
        private static readonly string TauntPath   = "Taunt";
        private static readonly string ShortScreamPath = "Screams/short";
        private static readonly string DeathScreamPath = "Screams/death";
        private static readonly string FleeScreamPath  = "Screams/flee";

        // The cuts folder holds individual steps; the parent folder holds the
        // full uncut recordings, which are too long to use as one-shots.
        private static readonly string FootstepPath = "Footsteps/cuts";

        private const float SwingDelay = 0.03f;
        private const float FleshDelay = 0.1f;
        private const float FleshVolume = 0.65f;
        private const float TauntVolume = 0.9f;
        private const float UnequipDelay = 0.5f;

        private static AudioClip[] effortClips;
        private static AudioClip[] damageClips;
        private static AudioClip[] deathClips;
        private static AudioClip[] fleshClips;
        private static AudioClip[] fleshSplatClips;
        private static AudioClip[] swingClips;
        private static AudioClip[] dropClips;
        private static AudioClip[] unequipClips;
        private static AudioClip[] blockClips;
        private static AudioClip[] tauntClips;
        private static AudioClip[] shortScreamClips;
        private static AudioClip[] deathScreamClips;
        private static AudioClip[] fleeScreamClips;
        private static AudioClip[] footstepClips;

        [Header("Vocal Chances")]
        [Tooltip("Chance a hit plays a vocal at all (damage grunt or short scream). On a failed roll only the flesh layers sound.")]
        [SerializeField, Range(0f, 1f)] private float hitVocalChance = 0.75f;
        [Tooltip("Chance an enemy death plays a scream. On a failed roll only the weapon drop and unequip sound.")]
        [SerializeField, Range(0f, 1f)] private float deathVocalChance = 0.75f;
        [Tooltip("Chance a fleeing enemy screams.")]
        [SerializeField, Range(0f, 1f)] private float fleeVocalChance = 0.75f;

        private AudioSource source;

        void Awake()
        {
            source = GetComponent<AudioSource>();
            source.spatialBlend = 1f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 1.5f;
            source.maxDistance = 25f;
            source.dopplerLevel = 0f;
            LoadClips();
        }

        static void LoadClips()
        {
            if (effortClips != null) return;
            effortClips = Resources.LoadAll<AudioClip>(EffortPath);
            damageClips = Resources.LoadAll<AudioClip>(DamagePath);
            deathClips = Resources.LoadAll<AudioClip>(DeathPath);
            fleshClips = Resources.LoadAll<AudioClip>(FleshPath);
            fleshSplatClips = Resources.LoadAll<AudioClip>(FleshSplatPath);
            swingClips = Resources.LoadAll<AudioClip>(WeaponPath);
            dropClips = Resources.LoadAll<AudioClip>(DropPath);
            unequipClips = Resources.LoadAll<AudioClip>(UnequipPath);
            blockClips = Resources.LoadAll<AudioClip>(BlockPath);
            tauntClips = Resources.LoadAll<AudioClip>(TauntPath);
            shortScreamClips = Resources.LoadAll<AudioClip>(ShortScreamPath);
            deathScreamClips = Resources.LoadAll<AudioClip>(DeathScreamPath);
            fleeScreamClips = Resources.LoadAll<AudioClip>(FleeScreamPath);
            footstepClips = Resources.LoadAll<AudioClip>(FootstepPath);
        }

        void OnEnable()
        {
            EventManager.OnDamage += HandleDamage;
            EventManager.OnDeath += HandleDeath;
            EventManager.OnHit += HandleHit;
            EventManager.OnBlock += HandleBlock;
            EventManager.OnFlee += HandleFlee;
        }

        void OnDisable()
        {
            EventManager.OnDamage -= HandleDamage;
            EventManager.OnDeath -= HandleDeath;
            EventManager.OnHit -= HandleHit;
            EventManager.OnBlock -= HandleBlock;
            EventManager.OnFlee -= HandleFlee;
        }

        void HandleDamage(DamageArgs e)
        {
            if (e.Target != gameObject) return;

            if (Random.value <= hitVocalChance)
                Play(RandomVocal(damageClips, shortScreamClips));

            if (e.Type == DamageType.Melee)
                StartCoroutine(FleshRoutine());
        }

        // Delayed so the flesh layer lands after the blade contact, not with it.
        private IEnumerator FleshRoutine()
        {
            yield return new WaitForSeconds(FleshDelay);
            Play(RandomClip(fleshClips), FleshVolume);
            Play(RandomClip(fleshSplatClips));
        }

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity != gameObject) return;

            if (CompareTag("Enemy"))
            {
                if (Random.value <= deathVocalChance)
                    Play(RandomClip(deathScreamClips));
            }
            else
            {
                Play(RandomClip(deathClips));
            }

            Play(RandomClip(dropClips));
            StartCoroutine(UnequipRoutine());
        }

        // Played on its own source so it can be faded out. The clips run longer
        // than the moment needs.
        private IEnumerator UnequipRoutine()
        {
            yield return new WaitForSeconds(UnequipDelay);

            AudioClip clip = RandomClip(unequipClips);
            if (clip == null) yield break;

            GameObject holder = new GameObject("UnequipSound");
            holder.transform.SetParent(transform, false);
            AudioSource unequip = holder.AddComponent<AudioSource>();
            unequip.spatialBlend = source != null ? source.spatialBlend : 0f;
            unequip.clip = clip;
            unequip.volume = 1f;
            unequip.Play();

            const float TotalDuration = 2f;
            const float FadeStartTime = 1.2f;

            yield return new WaitForSeconds(FadeStartTime);

            float fade = TotalDuration - FadeStartTime;
            float elapsed = 0f;
            while (elapsed < fade)
            {
                elapsed += Time.deltaTime;
                unequip.volume = Mathf.Max(0f, 1f - elapsed / fade);
                yield return null;
            }

            unequip.Stop();
            Destroy(holder);
        }

        void HandleHit(HitArgs e)
        {
            if (e.Entity == gameObject)
            {
                Play(RandomClip(effortClips));
                StartCoroutine(SwingRoutine());
            }
        }

        void HandleBlock(BlockArgs e)
        {
            if (e.Defender == gameObject)
                Play(RandomClip(blockClips));
        }

        void HandleFlee(FleeArgs e)
        {
            if (e.Entity != gameObject) return;
            if (Random.value > fleeVocalChance) return;

            Play(RandomClip(fleeScreamClips));
        }

        public void PlayEffort() => Play(RandomClip(effortClips));

        public void PlaySwing() => StartCoroutine(SwingRoutine());

        public void PlayFootstep() => Play(RandomClip(footstepClips));

        // No dedicated jump or land clips yet. An effort grunt reads as
        // exertion on take-off and a footstep reads as the landing impact.
        public void PlayJump() => Play(RandomClip(effortClips));

        public void PlayLand() => Play(RandomClip(footstepClips));

        public void PlayTaunt() => Play(RandomClip(tauntClips), TauntVolume);

        public void PlayHit() => Play(RandomVocal(damageClips, shortScreamClips));

        public void PlayDrop() => Play(RandomClip(dropClips));

        public void PlayUnequipDelayed() => StartCoroutine(UnequipRoutine());

        // Delayed so the swing whoosh lands with the blade, not the windup.
        private IEnumerator SwingRoutine()
        {
            yield return new WaitForSeconds(SwingDelay);
            Play(RandomClip(swingClips));
        }

        private static AudioClip RandomClip(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0) return null;
            return clips[Random.Range(0, clips.Length)];
        }

        // Picks from both pools as if they were one, so the odds follow how
        // many clips are in each rather than a fifty-fifty split.
        private static AudioClip RandomVocal(AudioClip[] grunts, AudioClip[] screams)
        {
            int gruntCount = grunts?.Length ?? 0;
            int screamCount = screams?.Length ?? 0;
            int total = gruntCount + screamCount;
            if (total == 0) return null;

            int roll = Random.Range(0, total);
            return roll < gruntCount ? grunts[roll] : screams[roll - gruntCount];
        }

        private void Play(AudioClip clip, float volumeScale = 0.9f)
        {
            if (clip != null && source != null)
                source.PlayOneShot(clip, volumeScale);
        }
    }
}