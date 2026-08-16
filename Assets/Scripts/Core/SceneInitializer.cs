using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Ensures EventManager events (and other static, scene-lifetime state)
    /// start clean when a scene loads. Place on any GameObject in the scene
    /// (runs before other Awake calls).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SceneInitializer : MonoBehaviour
    {
        void Awake()
        {
            EventManager.ClearAll();
            ClearAttackSlots();
        }

        // Same reflection approach WaveSpawner uses to reach into Game.Enemy —
        // keeps Core from taking a hard dependency on Enemy across the
        // (currently informal) assembly boundary.
        private static void ClearAttackSlots()
        {
            var type = System.Type.GetType("Game.Enemy.AttackSlotManager, Assembly-CSharp");
            type?.GetMethod("ClearAll")?.Invoke(null, null);
        }
    }
}
