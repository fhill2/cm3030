using UnityEngine;
using Game.Core;

namespace Game.Core
{
    // One place that answers "does the player control the knight right now".
    // The camera, attack and movement scripts read this instead of each
    // subscribing to the state machine themselves.
    //
    // Also frees the cursor while the shop is open so the market UI is
    // clickable, and relocks it when the next wave starts.
    //
    // Goes on the GameManager object.
    public class PlayerInputLock : MonoBehaviour
    {
        [Tooltip("States in which the player cannot move, look, attack or block.")]
        [SerializeField]
        private GameStateId[] lockedStates =
        {
            GameStateId.Menu,
            GameStateId.Shop,
            GameStateId.GameOver
        };

        [Tooltip("Show the mouse cursor while input is locked.")]
        [SerializeField] private bool freeCursorWhenLocked = true;

        // Static so the player scripts can ask without holding a reference to
        // the GameManager. There is only ever one game loop.
        public static bool InputLocked { get; private set; }

        void OnEnable()
        {
            EventManager.OnGameStateChanged += HandleStateChanged;
        }

        void OnDisable()
        {
            EventManager.OnGameStateChanged -= HandleStateChanged;
        }

        // Static state survives a scene reload, so clear it or the next run
        // starts with the lock still on.
        void Awake()
        {
            InputLocked = false;
        }

        private void HandleStateChanged(GameStateChangedArgs e)
        {
            bool locked = false;
            for (int i = 0; i < lockedStates.Length; i++)
            {
                if (lockedStates[i] == e.Current)
                {
                    locked = true;
                    break;
                }
            }

            InputLocked = locked;

            if (!freeCursorWhenLocked) return;

            // GameOver is left alone — PlayerMovement already frees the cursor
            // on death and relocking it here would fight that.
            if (e.Current == GameStateId.GameOver) return;

            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }
    }
}