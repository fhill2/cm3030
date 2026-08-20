using UnityEngine;
using Game.Health;

namespace Game.Core
{
    // Refills the player's health at the start of every wave.
    // Fires on WaveActive so it also covers wave one and the case where
    // the player closes the shop early.
    //
    // Goes on the GameManager object.
    public class WaveRecovery : MonoBehaviour
    {
        [Tooltip("Leave empty to find the object tagged Player.")]
        [SerializeField] private GameObject player;

        private void OnEnable()
        {
            EventManager.OnGameStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            EventManager.OnGameStateChanged -= HandleStateChanged;
        }

        private void HandleStateChanged(GameStateChangedArgs e)
        {
            if (e.Current != GameStateId.WaveActive) return;

            if (player == null) player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            // The health component sits on a child of the player root.
            HealthSystem health = player.GetComponentInChildren<HealthSystem>();
            if (health != null) health.HealToFull();
        }
    }
}