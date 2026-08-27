using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Health;
using Game.Combat;

namespace Game.Core
{
    // Spawns a wave, tracks how many are left, tells the state machine
    // when the wave is cleared.
    //
    // A wave is one or more EnemySets. Sets are interleaved when spawning:
    // three from the first set (the grunts), then one from each following
    // set (the elites), repeating — so tough enemies are interspersed
    // through the wave instead of arriving in one clump.
    //
    // Equipment is picked per enemy from every weapon/shield prefab under
    // Resources/Equipment, filtered by the set's level range. Enemies with
    // no eligible equipment keep their prefab's defaults.
    //
    // Counting is done by listening to OnDeath rather than searching the
    // scene every frame. We keep a list of what we spawned so we only count
    // deaths that belong to this wave.
    //
    // Goes on the GameManager object.
    public class WaveSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Parent whose direct children are the spawn points.")]
        [SerializeField] private Transform spawnPointRoot;

        // Runtime cache of the spawn points, collected from spawnPointRoot in Awake.
        private Transform[] spawnPoints;

        [Header("Waves")]
        [SerializeField] private WaveConfig[] waves;

        [Header("Spawning")]
        [SerializeField] private float spawnRadius = 2f;   // scatter around the point so they don't stack

        [Header("Debug")]
        [SerializeField] private bool logRemaining = true;

        [Header("Patrol Paths")]
        [Tooltip("Parent of patrol path groups. Each direct child is a path (e.g. Patrol1), and ITS children are the waypoints. Enemies cycle through paths in order.")]
        [SerializeField] private Transform patrolPathsRoot;

        // Which patrol path to assign next (cycles through patrolPathsRoot's children).
        private int patrolPathIndex;

        // How many enemies have already been put on each path this wave, keyed
        // by path index. Used to stagger where on the loop each one starts, so
        // enemies sharing a path don't all walk toward waypoint 0 at once.
        private readonly Dictionary<int, int> patrolPathAssignCounts = new Dictionary<int, int>();

        // Enemies from the current wave that are still alive.
        private readonly List<GameObject> liveEnemies = new List<GameObject>();

        // Read-only view for anything that just needs to know who's alive
        // right now (e.g. the minimap, to place enemy dots).
        public IReadOnlyList<GameObject> LiveEnemies => liveEnemies;

        // Wave number captured from the GameStateChanged payload (GSM owns it).
        private int currentWave;

        // True once the spawn coroutine has finished producing the whole wave.
        // Without this, killing the first enemy before the second spawns leaves
        // the list empty and fires a false "wave cleared".
        private bool spawningFinished;

        // Stops the wave being reported clear more than once.
        private bool waveAlreadyCleared;

        private void Awake()
        {
            if (spawnPointRoot != null)
            {
                spawnPoints = new Transform[spawnPointRoot.childCount];
                for (int i = 0; i < spawnPointRoot.childCount; i++)
                    spawnPoints[i] = spawnPointRoot.GetChild(i);
            }
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

        // Capture the wave number from the payload, then spawn on WaveActive.
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

            WaveConfig config = ConfigForWave(currentWave);
            if (config == null)
            {
                Debug.LogWarning("[WaveSpawner] No wave config assigned.");
                yield break;
            }

            foreach (EnemySet set in BuildSpawnQueue(config))
            {
                SpawnOne(set);
                yield return new WaitForSeconds(config.SpawnInterval);
            }

            spawningFinished = true;

            // Covers the case where the player killed everything while we were
            // still spawning, or where the wave was configured with no enemies.
            CheckWaveCleared();
        }

        // Expands the wave's sets into one entry per enemy, interleaved:
        // three from the first set, then one from each following set,
        // repeating until every set's count is spent. Keeps at least one
        // elite per three grunts without any randomness.
        private static List<EnemySet> BuildSpawnQueue(WaveConfig config)
        {
            var queue = new List<EnemySet>();
            var sets = config.Sets;
            if (sets == null) return queue;

            var remaining = new int[sets.Length];
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

            // Scale difficulty by bumping health above whatever the prefab has.
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.ApplyHealthMultiplier(set.HealthMultiplier);
                if (set.GoldReward > 0) health.GoldReward = set.GoldReward;
            }

            var equipment = enemy.GetComponent<Equipment>();
            if (equipment != null)
            {
                GameObject weapon = EquipmentCatalog.PickWeapon(set.EquipmentLevelMin, set.EquipmentLevelMax);
                if (weapon != null) equipment.WeaponPrefab = weapon;

                GameObject shield = EquipmentCatalog.PickShield(set.EquipmentLevelMin, set.EquipmentLevelMax);
                if (shield != null) equipment.ShieldPrefab = shield;
            }

            // Assign an individual patrol path to this enemy.
            AssignPatrolPath(enemy);

            // Scale difficulty by overriding how often these enemies try
            // to block. 0 (the set default) means "leave the prefab's own
            // blockChance alone" rather than forcing blocking off entirely.
            if (set.BlockChance > 0f) ApplyBlockChance(enemy, set.BlockChance);

            liveEnemies.Add(enemy);
        }

        // Random point inside a circle around the spawn point, so several
        // enemies from the same point don't end up inside each other.
        private Vector3 SpawnPositionNear(Transform point)
        {
            Vector2 offset = Random.insideUnitCircle * spawnRadius;
            return point.position + new Vector3(offset.x, 0f, offset.y);
        }

        // Assigns the next patrol path's waypoints to the enemy's NpcFSM.
        // Cycles through patrolPathsRoot's children: enemy 1 → Patrol1, enemy 2 → Patrol2, etc.
        //
        // Once enemy count outgrows the number of paths, the cycle wraps and a
        // path ends up with more than one enemy on it. Rather than start every
        // one of them at waypoint 0 — which walks them all into the same spot
        // at the same time — each additional enemy on a path starts further
        // around the loop, so they're spread out from the moment they spawn.
        private void AssignPatrolPath(GameObject enemy)
        {
            if (patrolPathsRoot == null || patrolPathsRoot.childCount == 0) return;

            var fsmType = System.Type.GetType("Game.Enemy.NpcFSM, Assembly-CSharp");
            if (fsmType == null) return;
            var fsm = enemy.GetComponent(fsmType);
            if (fsm == null) return;

            int pathIndex = patrolPathIndex % patrolPathsRoot.childCount;
            Transform path = patrolPathsRoot.GetChild(pathIndex);
            patrolPathIndex++;

            var list = fsmType.GetField("patrolTargets")?.GetValue(fsm) as List<Transform>;
            if (list == null) return;

            list.Clear();
            foreach (Transform wp in path)
                list.Add(wp);

            int assignedSoFar = patrolPathAssignCounts.TryGetValue(pathIndex, out int count) ? count : 0;
            patrolPathAssignCounts[pathIndex] = assignedSoFar + 1;

            if (list.Count > 0)
            {
                int startIndex = assignedSoFar % list.Count;
                fsmType.GetField("targetIndex")?.SetValue(fsm, startIndex);
            }
        }

        // Same reflection approach as AssignPatrolPath — avoids a hard dependency
        // from Core -> Enemy across the assembly boundary.
        private void ApplyBlockChance(GameObject enemy, float blockChance)
        {
            var fsmType = System.Type.GetType("Game.Enemy.NpcFSM, Assembly-CSharp");
            if (fsmType == null) return;
            var fsm = enemy.GetComponent(fsmType);
            if (fsm == null) return;

            fsmType.GetField("blockChance")?.SetValue(fsm, blockChance);
        }

        private void HandleDeath(DeathArgs e)
        {
            if (e.Entity == null) return;

            if (!RemoveFromWave(e.Entity)) return;   // not one of ours, ignore

            if (logRemaining)
            {
                Debug.Log($"[WaveSpawner] {liveEnemies.Count} left in wave {currentWave}");
            }

            CheckWaveCleared();
        }

        // The health component that reports the death may sit on a child of the
        // object we spawned, so fall back to matching on the root before giving up.
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
