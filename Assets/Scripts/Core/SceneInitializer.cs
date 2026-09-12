using UnityEngine;

namespace Game.Core
{
    // Resets static state when the scene loads, otherwise old subscribers
    // survive between play sessions in the editor.
    // Runs before other Awake calls.
    [DefaultExecutionOrder(-100)]
    public class SceneInitializer : MonoBehaviour
    {
        void Awake()
        {
            EventManager.ClearAll();
            ClearAttackSlots();
        }

        // Found by name instead of called directly, so Core doesn't have to
        // reference Game.Enemy. WaveSpawner does the same.
        private static void ClearAttackSlots()
        {
            System.Type slotManagerType = System.Type.GetType("Game.Enemy.AttackSlotManager, Assembly-CSharp");
            slotManagerType?.GetMethod("ClearAll")?.Invoke(null, null);
        }
    }
}