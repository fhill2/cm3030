using UnityEngine;

namespace Game.Core
{
    // One asset per wave. Right-click in the Project window to make them:
    // Create > Fall of Camelot > Wave Config
    //
    // Kept as a ScriptableObject so we can tune horde size and difficulty
    // in the Inspector without recompiling, and so waves can be swapped
    // around without touching the spawner.
    [CreateAssetMenu(fileName = "Wave", menuName = "Fall of Camelot/Wave Config")]
    public class WaveConfig : ScriptableObject
    {
        [Header("Horde")]
        [SerializeField] private int enemyCount = 5;
        [SerializeField] private float spawnInterval = 0.5f;   // gap between each spawn

        [Header("Difficulty")]
        [SerializeField] private float healthMultiplier = 1f;  // 2 = twice the health on the prefab

        [Header("Enemies")]
        [SerializeField] private GameObject[] enemyPrefabs;    // picked from at random

        [Header("Equipment Override (optional)")]
        [Tooltip("Weapon prefab to equip on all enemies in this wave. Leave empty to use the enemy's default.")]
        [SerializeField] private GameObject weaponOverride;
        [Tooltip("Shield prefab to equip on all enemies in this wave. Leave empty to use the enemy's default.")]
        [SerializeField] private GameObject shieldOverride;

        public int EnemyCount => enemyCount;
        public float SpawnInterval => spawnInterval;
        public float HealthMultiplier => healthMultiplier;
        public GameObject WeaponOverride => weaponOverride;
        public GameObject ShieldOverride => shieldOverride;

        // Random pick, so a wave can mix enemy types once Munya has more than one.
        public GameObject RandomEnemyPrefab()
        {
            if (enemyPrefabs == null || enemyPrefabs.Length == 0) return null;
            return enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        }
    }
}