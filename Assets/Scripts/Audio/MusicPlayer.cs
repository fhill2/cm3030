using System.Collections;
using UnityEngine;
using Game.Core;

namespace Game.Audio
{
    /// <summary>
    /// Looping background music that follows the game loop, two AudioSiources are kept so tracks can crossfade
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        [Header("Tracks")]
        [Tooltip("Played while the game is on the start screen.")]
        [SerializeField] private string menuClipPath = "Music/StartMenu";
        [Tooltip("Played once a run starts.")]
        [SerializeField] private string gameplayClipPath = "Music/InGameMusic";

        [Header("Mix")]
        [Tooltip("Music loudness")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.35f;
        [Tooltip("Seconds to crossfade when the track changes.")]
        [SerializeField] private float crossfadeTime = 1.5f;

        private AudioClip menuClip;
        private AudioClip gameplayClip;

        // Two sources so one can fade down while the other fades up
        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource active;

        private Coroutine fadeRoutine;

        void Awake()
        {
            menuClip     = Load(menuClipPath);
            gameplayClip = Load(gameplayClipPath);

            sourceA = CreateSource();
            sourceB = CreateSource();
            active  = sourceA;
        }

        void OnEnable()
        {
            EventManager.OnGameStateChanged += HandleGameStateChanged;
        }

        void OnDisable()
        {
            EventManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        void HandleGameStateChanged(GameStateChangedArgs e)
        {
            PlayTrack(e.Current == GameStateId.Menu ? menuClip : gameplayClip);
        }

        void PlayTrack(AudioClip next)
        {
            if (next == null) return;
            if (active != null && active.clip == next && active.isPlaying) return;

            AudioSource from = active;
            AudioSource to   = active == sourceA ? sourceB : sourceA;

            to.clip = next;
            to.volume = 0f;
            to.Play();
            active = to;

            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(Crossfade(from, to));
        }

        IEnumerator Crossfade(AudioSource from, AudioSource to)
        {
            float span = Mathf.Max(0.01f, crossfadeTime);
            float fromStart = from != null ? from.volume : 0f;
            float elapsed = 0f;

            while (elapsed < span)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / span);

                if (from != null) from.volume = Mathf.Lerp(fromStart, 0f, t);
                to.volume = Mathf.Lerp(0f, volume, t);
                yield return null;
            }

            if (from != null)
            {
                from.Stop();
                from.volume = 0f;
            }
            to.volume = volume;
            fadeRoutine = null;
        }

        AudioSource CreateSource()
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
            source.spatialBlend = 0f;
            return source;
        }

        AudioClip Load(string path)
        {
            var clip = Resources.Load<AudioClip>(path);
            if (clip == null)
                Debug.LogWarning($"[MusicPlayer] No clip at Resources/{path}");
            return clip;
        }
    }
}
