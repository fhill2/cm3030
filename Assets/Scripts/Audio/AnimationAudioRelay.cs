using UnityEngine;

namespace Game.Audio
{
    public class AnimationAudioRelay : MonoBehaviour
    {
        [Tooltip("Leave empty to find the ActorAudio on a parent object.")]
        [SerializeField] private ActorAudio actorAudio;

        void Awake()
        {
            if (actorAudio == null) actorAudio = GetComponentInParent<ActorAudio>();
        }

        // ── Animation Event targets ────────────────────────────
        public void AnimSwing()
        {
            if (actorAudio != null) actorAudio.PlaySwing();
        }

        public void AnimEffort()
        {
            if (actorAudio != null) actorAudio.PlayEffort();
        }

        public void AnimFootstep()
        {
            if (actorAudio != null) actorAudio.PlayFootstep();
        }
    }
}
