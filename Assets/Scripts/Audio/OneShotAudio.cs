using UnityEngine;

namespace Game.Audio
{
    public static class OneShotAudio
    {
        public static void Play2D(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;

            var go = new GameObject("OneShotAudio");
            go.transform.position = position;
            var source = go.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f;
            source.volume = 0.9f;
            source.Play();
            Object.Destroy(go, clip.length);
        }
    }
}
