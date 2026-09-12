using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    // Which school a spell belongs to. Fire is keyed to 1-3, ice to 4-6.
    public enum SpellSchool
    {
        Fire,
        Ice
    }

    // One spell at one level. Six of these exist: fire 1-3 and ice 1-3.
    // Create > Fall of Camelot > Spell
    [CreateAssetMenu(fileName = "Spell", menuName = "Fall of Camelot/Spell")]
    public class SpellDef : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "New Spell";
        [SerializeField] private SpellSchool school = SpellSchool.Fire;
        [Tooltip("1 to 3. Higher levels replace lower ones of the same school.")]
        [SerializeField] private int level = 1;

        [Header("Casting")]
        [SerializeField] private float damage = 20f;
        [SerializeField] private float staminaCost = 30f;
        [Tooltip("Seconds before this school can be cast again.")]
        [SerializeField] private float cooldown = 1.5f;

        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;
        [Tooltip("All three levels of a school share one prefab, so scale is what tells them apart.")]
        [SerializeField] private float projectileScale = 1f;
        [SerializeField] private float projectileSpeed = 25f;
        [Tooltip("Seconds before an unspent projectile removes itself.")]
        [SerializeField] private float projectileLifetime = 4f;

        [Header("Impact")]
        [Tooltip("Effect spawned where the spell lands.")]
        [SerializeField] private GameObject impactEffect;
        [Tooltip("Raise it for higher-level spells.")]
        [SerializeField] private float impactScale = 1f;
        [Tooltip("Seconds before the spent effect removes itself.")]
        [SerializeField] private float impactLifetime = 3f;

        [Header("Drop")]
        [Tooltip("Relative chance of this tome dropping. Weight 6 is picked twice as often as weight 3. Set 0 to stop it dropping.")]
        [SerializeField] private float dropWeight = 1f;

        private AudioClip[] castClip;
        private AudioClip[] hitClip;

        public string DisplayName => displayName;
        public SpellSchool School => school;
        public int Level => level;
        public float Damage => damage;
        public float StaminaCost => staminaCost;
        public float Cooldown => cooldown;
        public GameObject ProjectilePrefab => projectilePrefab;
        public float ProjectileScale => projectileScale;
        public float ProjectileSpeed => projectileSpeed;
        public float ProjectileLifetime => projectileLifetime;
        public GameObject ImpactEffect => impactEffect;
        public float ImpactScale => impactScale;
        public float ImpactLifetime => impactLifetime;
        public float DropWeight => dropWeight;

        public AudioClip[] CastClip
        {
            get
            {
                if (castClip == null || castClip.Length == 0) castClip = LoadClips("cast");
                return castClip;
            }
        }

        public AudioClip[] HitClip
        {
            get
            {
                if (hitClip == null || hitClip.Length == 0) hitClip = LoadClips("hit");
                return hitClip;
            }
        }

        // Clips are pulled from Resources/Spells/<asset name>/cast or /hit,
        // so new spells only need the folder, not an Inspector reference.
        private AudioClip[] LoadClips(string subfolder)
        {
            string path = "Spells/" + name + "/" + subfolder;
            Object[] loadedAssets = Resources.LoadAll(path);
            if (loadedAssets == null) return null;

            List<AudioClip> clips = new List<AudioClip>();
            foreach (Object asset in loadedAssets)
                if (asset is AudioClip clip) clips.Add(clip);

            return clips.Count > 0 ? clips.ToArray() : null;
        }
    }
}