using System.Collections;
using UnityEngine;
using Game.Core;
using Game.Health;

namespace Game.Audio
{
    public class CinematicAudio : MonoBehaviour
    {
        private const string ViolinPath = "Ambient/Violins";
        private const string BattlecryPath = "Battlecry";
        private const float ViolinVolume = 0.4f;
        private const float BattlecryDelay = 4f;
        private const float BattlecryInterval = 10f;

        private AudioSource violinSource;
        private AudioSource crySource;

        void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
        }

        void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
        }

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity != gameObject) return;

            if (violinSource == null) StartViolins();
            if (crySource == null) StartCoroutine(BattlecrySequence());
        }

        private void StartViolins()
        {
            AudioClip clip = Resources.Load<AudioClip>(ViolinPath);
            if (clip == null)
            {
                Debug.LogWarning($"[CinematicAudio] No clip at Resources/{ViolinPath}");
                return;
            }

            violinSource = gameObject.AddComponent<AudioSource>();
            violinSource.clip = clip;
            violinSource.loop = true;
            violinSource.volume = ViolinVolume;
            violinSource.spatialBlend = 0f;
            violinSource.Play();
        }

        private IEnumerator BattlecrySequence()
        {
            yield return new WaitForSeconds(BattlecryDelay);

            AudioClip[] cries = Resources.LoadAll<AudioClip>(BattlecryPath);
            if (cries == null || cries.Length == 0) yield break;

            crySource = gameObject.AddComponent<AudioSource>();
            crySource.spatialBlend = 0f;

            foreach (AudioClip cry in cries)
            {
                if (cry == null) continue;

                crySource.PlayOneShot(cry, 0.9f);
                yield return new WaitForSeconds(BattlecryInterval);
            }
        }
    }
}
