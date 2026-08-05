using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Health;

namespace Game.Core
{
    // Spawns a wave, tracks how many are left, tells the state machine
    // when the wave is cleared.
    //
    // Counting is done by listening to OnDeath rather than searching the
    // scene every frame. We keep a list of what we spawned so we only count
    // deaths that belong to this wave.
    //
    // Goes on the GameManager object.
    public class WaveSpawner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameStateMachine stateMachine;
        [SerializeField] private Transform player;
        [SerializeField] private Transform[] spawnPoints;

        [Header("Waves")]
        [SerializeField] private WaveConfig[] waves;

        [Header("Spawning")]
        [SerializeField] private float spawnRadius = 2f;   // scatter around the point so they don't stack

        // Enemies from the current wave that are still alive.
        private readonly List<GameObject> liveEnemies = new List<GameObject>();

        private void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
            EventManager.OnGameStateChanged += HandleStateChanged;
        }

        private void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
            EventManager.OnGameStateChanged -= HandleStateChanged;
        }

        // Start spawning as soon as we enter WaveActive.
        private void HandleStateChanged(GameStateChangedArgs e)
        {
            if (e.Current == GameStateId.WaveActive) StartCoroutine(SpawnWave());
        }

        private IEnumerator SpawnWave()
        {
            liveEnemies.Clear();

            WaveConfig config = ConfigForWave(stateMachine.CurrentWave);
            if (config == null)
            {
                Debug.LogWarning("[WaveSpawner] No wave config assigned.");
                yield break;
            }

            for (int i = 0; i < config.EnemyCount; i++)
            {
                SpawnOne(config);
                yield return new WaitForSeconds(config.SpawnInterval);
            }
        }

        private void SpawnOne(WaveConfig config)
        {
            GameObject prefab = config.RandomEnemyPrefab();
            if (prefab == null)
            {
                Debug.LogWarning("[WaveSpawner] Wave config has no enemy prefabs.");
                return;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[WaveSpawner] No spawn points assigned.");
                return;
            }

            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject enemy = Instantiate(prefab, SpawnPositionNear(point), point.rotation);

            // Scale difficulty by bumping health above whatever the prefab has.
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null) health.ApplyHealthMultiplier(config.HealthMultiplier);

            // Point the placeholder movement at the player.
            ChaseTarget chase = enemy.GetComponent<ChaseTarget>();
            if (chase == null)
            {
                Debug.LogWarning("[WaveSpawner] Enemy prefab has no ChaseTarget, it won't move.");
            }
            else if (player == null)
            {
                Debug.LogWarning("[WaveSpawner] Player reference is empty, enemies won't move.");
            }
            else
            {
                chase.SetTarget(player);
            }

            liveEnemies.Add(enemy);
        }

        // Random point inside a circle around the spawn point, so several
        // enemies from the same point don't end up inside each other.
        private Vector3 SpawnPositionNear(Transform point)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            return point.position + new Vector3(offset.x, 0f, offset.y);
        }

        private void HandleDeath(DeathArgs e)
        {
            if (!liveEnemies.Remove(e.Entity)) return;   // not one of ours, ignore

            Debug.Log($"[WaveSpawner] {liveEnemies.Count} left in wave {stateMachine.CurrentWave}");

            if (liveEnemies.Count == 0) stateMachine.MoveToState(GameStateId.WaveComplete);
        }

        // Waves past the end of the array reuse the last one, so the game
        // doesn't stop dead once we run out of configs.
        private WaveConfig ConfigForWave(int waveNumber)
        {
            if (waves == null || waves.Length == 0) return null;

            int index = Mathf.Clamp(waveNumber - 1, 0, waves.Length - 1);
            return waves[index];
        }
    }
}