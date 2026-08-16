using UnityEngine;
using UnityEngine.InputSystem;
using Game.Audio;
using Game.Core;
using Game.Shared;

namespace Game.Movement
{
    public class PlayerTaunt : MonoBehaviour
    {
        [Tooltip("Minimum seconds between taunts, so voice lines don't stack when mashing E. Set 0 to disable.")]
        [SerializeField] private float cooldown = 20f;

        private ActorAudio actorAudio;
        private Animator animator;
        private float nextTauntTime = float.NegativeInfinity;

        void Awake()
        {
            actorAudio = GetComponent<ActorAudio>();
            animator = GetComponentInChildren<Animator>();
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!kb.eKey.wasPressedThisFrame) return;
            if (Time.time < nextTauntTime) return;

            nextTauntTime = Time.time + cooldown;

            if (actorAudio != null) actorAudio.PlayTaunt();
            if (animator != null) animator.SetTrigger(AnimParams.Buff);
            EventManager.RaiseTaunt();
        }
    }
}
