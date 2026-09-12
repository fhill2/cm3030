using UnityEngine;

namespace Game.Audio
{
    public class AmbientAudio : MonoBehaviour
    {
        [Tooltip("Clip under Resources/Ambient to loop from scene start.")]
        [SerializeField] private string clipPath = "Ambient/The Stronghold";

        [Tooltip("Extra clips to loop alongside the first, e.g. rain and thunder layers.")]
        [SerializeField] private string[] clipPaths;

        [Tooltip("Loudness of the ambient loop relative to combat SFX.")]
        [SerializeField, Range(0f,1f)] private float volume = 0.5f;

        private static AudioSource MakeSource(GameObject host, AudioClip clip, float sourceVolume)
        {
            AudioSource source = host.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = sourceVolume;
            return source;
        }

        private void Awake()
        {
            bool anyPlaying = false;

            AudioClip primary = Resources.Load<AudioClip>(clipPath);
            if (primary != null)
            {
                MakeSource(gameObject, primary, volume).Play();
                anyPlaying = true;
            }

            if (clipPaths != null)
            {
                foreach (string path in clipPaths)
                {
                    if (string.IsNullOrEmpty(path)) continue;
                    AudioClip clip = Resources.Load<AudioClip>(path);
                    if (clip == null)
                    {
                        Debug.LogWarning($"[AmbientAudio] No clip at Resources/{path}");
                        continue;
                    }
                    MakeSource(gameObject, clip, volume).Play();
                    anyPlaying = true;
                }
            }

            if (!anyPlaying)
                Debug.LogWarning($"[AmbientAudio] No clip at Resources/{clipPath}");
        }
    }
}