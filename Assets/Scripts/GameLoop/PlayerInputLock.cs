using UnityEngine;

namespace Game.Core
{
    // Answers whether the player controls the knight right now. The camera,
    // attack and movement scripts read this instead of each subscribing to
    // the state machine. Also frees the cursor while the shop is open.
    // Sits on the GameManager.
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

        // Static so player scripts can ask without a reference to the GameManager.
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

            // PlayerMovement already frees the cursor on death, so relocking
            // here would fight it.
            if (e.Current == GameStateId.GameOver) return;

            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }
    }
}