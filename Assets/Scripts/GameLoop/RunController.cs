using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Game.Health;

namespace Game.Core
{
    // Ends the run when the player dies and restarts it on request.
    // Death comes off the event bus rather than a direct reference.
    // Sits on the GameManager.
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

        // Read by the UI to decide when to show the restart prompt.
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

            // GameOver stops the wave timers, since WaveCompleteState and
            // ShopState both cancel their coroutines on exit.
            if (stateMachine != null) stateMachine.MoveToState(GameStateId.GameOver);
        }

        private void Update()
        {
            if (!allowKeyRestart) return;
            if (!CanRestart) return;
            if (Keyboard.current == null) return;

            if (Keyboard.current.rKey.wasPressedThisFrame) Restart();
        }

        // Public so a UI button can call it as well as the R key.
        public void Restart()
        {
            // Static events outlive the scene, so old objects would stay
            // subscribed and fire into destroyed references.
            EventManager.ClearAll();

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}