using UnityEngine;
using Game.Health;

namespace Game.Core
{
    // Holds the player's gold and broadcasts every change.
    //
    // Awards gold on enemy death and on wave clear. Nothing else needs to
    // call in to award it — the wallet listens to the event bus itself.
    // The UI reads the balance by subscribing to OnGoldChanged rather than
    // holding a reference to this.
    //
    // Goes on the GameManager object.
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
            // Announce the opening balance in Start so the UI, which subscribes
            // in OnEnable, has something to display from the first frame.
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

            // Only enemies pay out. EnemyHealth is what marks something as an
            // enemy, so the player dying doesn't award gold.
            if (e.Entity.GetComponentInParent<EnemyHealth>() == null) return;

            Add(goldPerKill);
        }

        private void HandleWaveCleared(WaveClearedArgs e)
        {
            Add(waveClearBonus * Mathf.Max(1, e.Wave));
        }

        // Adds gold and tells everyone. Use a negative amount to deduct
        // without the affordability check.
        public void Add(int amount)
        {
            if (amount == 0) return;

            gold = Mathf.Max(0, gold + amount);
            Announce(amount);
        }

        // Spend if the player can afford it. Returns false and changes
        // nothing if they can't — the shop uses this to gate purchases.
        public bool TrySpend(int amount)
        {
            if (amount <= 0) return false;
            if (gold < amount) return false;

            gold -= amount;
            Announce(-amount);
            return true;
        }

        // Back to the starting balance, for a new run.
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