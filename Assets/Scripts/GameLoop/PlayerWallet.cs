using UnityEngine;
using Game.Health;

namespace Game.Core
{
    // Holds the player's gold and raises OnGoldChanged on every change.
    // Awards gold off the event bus on enemy death and wave clear, so nothing
    // has to call in to pay out.
    // Sits on the GameManager.
    public class PlayerWallet : MonoBehaviour
    {
        [Header("Starting Balance")]
        [SerializeField] private int startingGold = 0;

        [Header("Rewards")]
        [Tooltip("Gold for each enemy killed.")]
        [SerializeField] private int goldPerKill = 10;

        [Tooltip("Bonus for clearing a wave. Multiplied by the wave number, so wave 3 pays three times this.")]
        [SerializeField] private int waveClearBonus = 25;

        [Header("Debug")]
        [SerializeField] private bool logChanges = true;

        private int gold;

        public int Gold => gold;

        private void Awake()
        {
            gold = startingGold;
        }

        private void Start()
        {
            // In Start, so the UI subscribing in OnEnable has a balance to
            // show from the first frame.
            Announce(0);
        }

        private void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
            EventManager.OnWaveCleared += HandleWaveCleared;
        }

        private void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
            EventManager.OnWaveCleared -= HandleWaveCleared;
        }

        private void HandleDeath(DeathArgs e)
        {
            if (e.Entity == null) return;

            // EnemyHealth is what marks something as an enemy, so the player
            // dying doesn't pay out.
            EnemyHealth enemy = e.Entity.GetComponentInParent<EnemyHealth>();
            if (enemy == null) return;

            Add(enemy.GoldReward > 0 ? enemy.GoldReward : goldPerKill);
        }

        private void HandleWaveCleared(WaveClearedArgs e)
        {
            Add(waveClearBonus * Mathf.Max(1, e.Wave));
        }

        // A negative amount deducts without the affordability check.
        public void Add(int amount)
        {
            if (amount == 0) return;

            gold = Mathf.Max(0, gold + amount);
            Announce(amount);
        }

        // Changes nothing and returns false if the player is short.
        public bool TrySpend(int amount)
        {
            if (amount <= 0) return false;
            if (gold < amount) return false;

            gold -= amount;
            Announce(-amount);
            return true;
        }

        public void ResetToStart()
        {
            int change = startingGold - gold;
            gold = startingGold;
            Announce(change);
        }

        private void Announce(int change)
        {
            if (logChanges && change != 0)
            {
                Debug.Log($"[Wallet] {(change > 0 ? "+" : "")}{change} gold, total {gold}");
            }

            EventManager.RaiseGoldChanged(new GoldChangedArgs(gold, change));
        }
    }
}