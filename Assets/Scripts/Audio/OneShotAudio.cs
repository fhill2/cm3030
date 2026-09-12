using UnityEngine;

namespace Game.Audio
{
    public static class OneShotAudio
    {
        public static void Play2D(AudioClip clip, Vector3 position)
        {
            if (clip == null) return;

            GameObject holder = new GameObject("OneShotAudio");
            holder.transform.position = position;

            AudioSource source = holder.AddComponent<AudioSource>();
            source.clip = clip;
            source.spatialBlend = 0f;
            source.volume = 0.9f;
            source.Play();

            Object.Destroy(holder, clip.length);
        }
    }
}