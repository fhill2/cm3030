using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Health;
using Game.Combat;

namespace Game.Core
{
    // Spawns a wave, tracks how many are left, and reports the clear.
    // Sets are interleaved when spawning: three grunts, then one from each
    // following set, repeating, so elites are spread through the wave.
    // Counting works off OnDeath rather than searching the scene each frame.
    // Sits on the GameManager.
    public class WaveSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Parent whose direct children are the spawn points.")]
        [SerializeField] private Transform spawnPointRoot;

        private Transform[] spawnPoints;

        [Header("Waves")]
        [SerializeField] private WaveConfig[] waves;

        [Header("Spawning")]
        [SerializeField] private float spawnRadius = 2f;   // scatter so they don't stack

        [Header("Hunt")]
        [Tooltip("Once at most this many enemies remain in the wave, every enemy hunts the player regardless of distance. Ends the player chasing a lone patrol around the castle at the tail end of a wave.")]
        [SerializeField] private int huntRemainingCount = 3;

        [Header("Debug")]
        [SerializeField] private bool logRemaining = true;

        [Header("Patrol Paths")]
        [Tooltip("Parent of patrol path groups. Each direct child is a path (e.g. Patrol1), and ITS children are the waypoints. Enemies cycle through paths in order.")]
        [SerializeField] private Transform patrolPathsRoot;

        private int patrolPathIndex;

        // How many enemies are already on each path this wave, keyed by path
        // index, so enemies sharing a path start at different waypoints.
        private readonly Dictionary<int, int> patrolPathAssignCounts = new Dictionary<int, int>();

        private readonly List<GameObject> liveEnemies = new List<GameObject>();

        // Read by the minimap to place enemy dots.
        public IReadOnlyList<GameObject> LiveEnemies => liveEnemies;

        // True on the last few enemies: they drop patrol and chase the player.
        // Static so the enemy states can read it without a reference here.
        public static bool HuntMode { get; private set; }

        private int currentWave;

        // Without this, killing the first enemy before the second spawns
        // leaves the list empty and fires a false wave clear.
        private bool spawningFinished;

        private bool waveAlreadyCleared;

        // NpcFSM is looked up by name so Core doesn't reference Game.Enemy.
        private static System.Type npcFsmType;

        private void Awake()
        {
            if (spawnPointRoot != null)
            {
                spawnPoints = new Transform[spawnPointRoot.childCount];
                for (int i = 0; i < spawnPointRoot.childCount; i++)
                    spawnPoints[i] = spawnPointRoot.GetChild(i);
            }

            npcFsmType = System.Type.GetType("Game.Enemy.NpcFSM, Assembly-CSharp");
        }

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

        private void HandleStateChanged(GameStateChangedArgs e)
        {
            currentWave = e.Wave;
            if (e.Current == GameStateId.WaveActive) StartCoroutine(SpawnWave());
        }

        private IEnumerator SpawnWave()
        {
            liveEnemies.Clear();
            patrolPathIndex = 0;
            patrolPathAssignCounts.Clear();
            spawningFinished = false;
            waveAlreadyCleared = false;
            HuntMode = false;

            WaveConfig config = ConfigForWave(currentWave);
            if (config == null)
            {
                Debug.LogWarning("[WaveSpawner] No wave config assigned.");
                yield break;
            }

            foreach (EnemySet set in BuildSpawnQueue(config))
            {
                SpawnOne(set);
                if (config.SpawnInterval > 0f)
                    yield return new WaitForSeconds(config.SpawnInterval);
            }

            spawningFinished = true;
            UpdateHuntMode();

            // Covers the player killing everything mid-spawn, or a wave
            // configured with no enemies.
            CheckWaveCleared();
        }

        // One entry per enemy, interleaved: three from the first set, then one
        // from each following set, until every count is spent.
        private static List<EnemySet> BuildSpawnQueue(WaveConfig config)
        {
            List<EnemySet> queue = new List<EnemySet>();
            EnemySet[] sets = config.Sets;
            if (sets == null) return queue;

            int[] remaining = new int[sets.Length];
            for (int i = 0; i < sets.Length; i++) remaining[i] = sets[i].Count;

            bool anyLeft = true;
            while (anyLeft)
            {
                anyLeft = false;

                for (int i = 0; i < 3 && remaining[0] > 0; i++)
                {
                    remaining[0]--;
                    queue.Add(sets[0]);
                }

                for (int s = 1; s < sets.Length; s++)
                {
                    if (remaining[s] > 0)
                    {
                        remaining[s]--;
                        queue.Add(sets[s]);
                    }
                }

                for (int i = 0; i < remaining.Length; i++)
                {
                    if (remaining[i] > 0) { anyLeft = true; break; }
                }
            }

            return queue;
        }

        private void SpawnOne(EnemySet set)
        {
            GameObject prefab = set.Prefab;
            if (prefab == null)
            {
                Debug.LogWarning("[WaveSpawner] Enemy set has no prefab.");
                return;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning("[WaveSpawner] No spawn points assigned.");
                return;
            }

            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject enemy = Instantiate(prefab, SpawnPositionNear(point), point.rotation);

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.ApplyHealthMultiplier(set.HealthMultiplier);
                if (set.GoldReward > 0) health.GoldReward = set.GoldReward;
            }

            Equipment equipment = enemy.GetComponent<Equipment>();
            if (equipment != null)
            {
                GameObject weapon = EquipmentCatalog.PickWeapon(set.EquipmentLevelMin, set.EquipmentLevelMax);
                if (weapon != null) equipment.WeaponPrefab = weapon;

                GameObject shield = EquipmentCatalog.PickShield(set.EquipmentLevelMin, set.EquipmentLevelMax);
                if (shield != null) equipment.ShieldPrefab = shield;
            }

            AssignPatrolPath(enemy);

            // 0 means leave the prefab's own blockChance alone rather than
            // forcing blocking off.
            if (set.BlockChance > 0f) ApplyBlockChance(enemy, set.BlockChance);

            liveEnemies.Add(enemy);
        }

        private Vector3 SpawnPositionNear(Transform point)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            return point.position + new Vector3(offset.x, 0f, offset.y);
        }

        // Gives the enemy one patrol path's waypoints, cycling through the
        // paths. Once there are more enemies than paths, each extra enemy on a
        // path starts further round the loop so they don't bunch up.
        private void AssignPatrolPath(GameObject enemy)
        {
            if (patrolPathsRoot == null || patrolPathsRoot.childCount == 0) return;
            if (npcFsmType == null) return;

            Component fsm = enemy.GetComponent(npcFsmType);
            if (fsm == null) return;

            int pathIndex = patrolPathIndex % patrolPathsRoot.childCount;
            Transform path = patrolPathsRoot.GetChild(pathIndex);
            patrolPathIndex++;

            List<Transform> waypoints = npcFsmType.GetField("patrolTargets")?.GetValue(fsm) as List<Transform>;
            if (waypoints == null) return;

            waypoints.Clear();
            foreach (Transform waypoint in path)
                waypoints.Add(waypoint);

            int assignedSoFar = patrolPathAssignCounts.TryGetValue(pathIndex, out int count) ? count : 0;
            patrolPathAssignCounts[pathIndex] = assignedSoFar + 1;

            if (waypoints.Count > 0)
            {
                int startIndex = assignedSoFar % waypoints.Count;
                npcFsmType.GetField("targetIndex")?.SetValue(fsm, startIndex);
            }
        }

        private void ApplyBlockChance(GameObject enemy, float blockChance)
        {
            if (npcFsmType == null) return;

            Component fsm = enemy.GetComponent(npcFsmType);
            if (fsm == null) return;

            npcFsmType.GetField("blockChance")?.SetValue(fsm, blockChance);
        }

        // The spawningFinished gate stops a mid-spawn dip below the threshold
        // triggering the hunt early.
        private void UpdateHuntMode()
        {
            HuntMode = spawningFinished
                       && liveEnemies.Count > 0
                       && liveEnemies.Count <= huntRemainingCount;
        }

        private void HandleDeath(DeathArgs e)
        {
            if (e.Entity == null) return;

            if (!RemoveFromWave(e.Entity)) return;   // not one of ours

            UpdateHuntMode();

            if (logRemaining)
            {
                Debug.Log($"[WaveSpawner] {liveEnemies.Count} left in wave {currentWave}");
            }

            CheckWaveCleared();
        }

        // The health component reporting the death can sit on a child of the
        // spawned object, so try the root before giving up.
        private bool RemoveFromWave(GameObject dead)
        {
            if (liveEnemies.Remove(dead)) return true;

            GameObject root = dead.transform.root.gameObject;
            return liveEnemies.Remove(root);
        }

        private void CheckWaveCleared()
        {
            if (waveAlreadyCleared) return;
            if (!spawningFinished) return;
            if (liveEnemies.Count > 0) return;

            waveAlreadyCleared = true;
            EventManager.RaiseWaveCleared(new WaveClearedArgs(currentWave));
        }

        // Waves past the end of the array reuse the last one.
        private WaveConfig ConfigForWave(int waveNumber)
        {
            if (waves == null || waves.Length == 0) return null;

            int index = Mathf.Clamp(waveNumber - 1, 0, waves.Length - 1);
            return waves[index];
        }
    }
}