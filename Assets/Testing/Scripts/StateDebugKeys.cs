using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    // Temporary. Lets us drive the state machine from the keyboard while
    // there's no menu, no shop UI and no death handling yet.
    // Delete once the real triggers exist.
    //
    // Goes on the GameManager object.
    public class StateDebugKeys : MonoBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;

        private void Update()
        {
            if (Keyboard.current == null) return;

            // 1 — start the next wave. Works from Menu, WaveComplete or Shop.
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                stateMachine.MoveToState(GameStateId.WaveActive);
            }

            // 2 — open the shop.
            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                stateMachine.MoveToState(GameStateId.Shop);
            }

            // 3 — back to the menu, resets the wave counter to 0.
            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                stateMachine.MoveToState(GameStateId.Menu);
            }
        }
    }
}