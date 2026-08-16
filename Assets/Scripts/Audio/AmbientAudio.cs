using UnityEngine;

namespace Game.Audio
{
    public class AmbientAudio : MonoBehaviour
    {
        [Tooltip("Clip under Resources/Ambient to loop from scene start.")]
        [SerializeField] private string clipPath = "Ambient/The Stronghold";

        [Tooltip("Loudness of the ambient loop relative to combat SFX.")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

        private void Awake()
        {
            AudioClip clip = Resources.Load<AudioClip>(clipPath);
            if (clip == null)
            {
                Debug.LogWarning($"[AmbientAudio] No clip at Resources/{clipPath}");
                return;
            }

            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.volume = volume;
            source.Play();
        }
    }
}
