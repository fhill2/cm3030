using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Game.Core;
using Game.Health;

namespace Game.Core
{
    // Owns the run: ends it when the player dies, and starts a fresh one on
    // request.
    //
    // Death is detected off the bus rather than by holding a reference to the
    // player, so this works regardless of which object is the player.
    //
    // Restart reloads the active scene. Everything in the scene is rebuilt from
    // scratch, which is far less error-prone than resetting a dozen systems by
    // hand. The static events have to be cleared first — they survive a scene
    // load and would otherwise still point at the destroyed objects.
    //
    // Goes on the GameManager object.
    public class RunController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameStateMachine stateMachine;

        [Tooltip("The player object. Used only to tell the player's death from an enemy's.")]
        [SerializeField] private GameObject player;

        [Header("Restart")]
        [Tooltip("Seconds after death before the restart key is accepted, so the death sequence can play out.")]
        [SerializeField] private float restartDelay = 4f;

        [Tooltip("Allow restarting with the R key. The UI can also call Restart() directly.")]
        [SerializeField] private bool allowKeyRestart = true;

        private float restartAllowedAt;
        private bool runOver;

        // True once the player is dead and the restart input will be accepted.
        // The UI reads this to decide when to show the prompt.
        public bool CanRestart => runOver && Time.time >= restartAllowedAt;

        private void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
        }

        private void HandleDeath(DeathArgs e)
        {
            if (runOver) return;
            if (e.Entity == null) return;
            if (player != null && e.Entity != player) return;   // an enemy died, not us

            runOver = true;
            restartAllowedAt = Time.time + restartDelay;

            // Moving to GameOver stops the wave timers, since WaveCompleteState
            // and ShopState both cancel their coroutines on exit.
            if (stateMachine != null) stateMachine.MoveToState(GameStateId.GameOver);
        }

        private void Update()
        {
            if (!allowKeyRestart) return;
            if (!CanRestart) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.rKey.wasPressedThisFrame) Restart();
        }

        // Wipe the event subscriptions and reload. Public so a UI button can
        // call it as well as the R key.
        public void Restart()
        {
            // Static events outlive the scene. Without this, every object from
            // the old scene stays subscribed and fires into destroyed objects.
            EventManager.ClearAll();

            // The cursor is unlocked on death; the reloaded scene locks it again
            // in PlayerMovement.Awake.
            Time.timeScale = 1f;

            Scene active = SceneManager.GetActiveScene();
            SceneManager.LoadScene(active.buildIndex);
        }
    }
}