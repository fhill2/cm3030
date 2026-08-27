using System;
using UnityEngine;

namespace Game.Core
{
    // One entry in a wave's horde: a prefab, how many of it spawn, and how
    // tough it is. Waves mix sets to intersperse elites with grunts — the
    // spawner weaves them together.
    [Serializable]
    public class EnemySet
    {
        [Tooltip("Enemy prefab this set spawns.")]
        [SerializeField] private GameObject prefab;

        [Tooltip("How many enemies from this set the wave contains.")]
        [SerializeField] private int count = 3;

        [Tooltip("Health multiplier applied on top of the prefab's health.")]
        [SerializeField] private float healthMultiplier = 1f;

        [Tooltip("Chance (0-1) these enemies try to block the player's swings. 0 keeps the prefab's own blockChance.")]
        [SerializeField, Range(0f, 1f)] private float blockChance = 0f;

        [Tooltip("Lowest equipment level these enemies can be equipped with.")]
        [SerializeField] private int equipmentLevelMin = 1;

        [Tooltip("Highest equipment level these enemies can be equipped with. 0 = no cap.")]
        [SerializeField] private int equipmentLevelMax = 1;

        [Tooltip("Gold paid when one of these enemies dies. 0 keeps the wallet's goldPerKill.")]
        [SerializeField] private int goldReward = 0;

        public GameObject Prefab => prefab;
        public int Count => count;
        public float HealthMultiplier => healthMultiplier;
        public float BlockChance => blockChance;
        public int EquipmentLevelMin => equipmentLevelMin;
        public int EquipmentLevelMax => equipmentLevelMax;
        public int GoldReward => goldReward;
    }

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
        [Tooltip("Enemy sets making up this wave. The spawner interleaves them: three from the first set, then one from each following set, repeating.")]
        [SerializeField] private EnemySet[] sets;

        [Tooltip("Seconds between each individual spawn.")]
        [SerializeField] private float spawnInterval = 0.5f;

        public EnemySet[] Sets => sets;
        public float SpawnInterval => spawnInterval;
    }
}
