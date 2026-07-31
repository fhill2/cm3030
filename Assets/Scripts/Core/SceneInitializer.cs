using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Ensures EventManagerScript events start clean when a scene loads.
    /// Place on any GameObject in the scene (runs before other Awake calls).
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class SceneInitializer : MonoBehaviour
    {
        void Awake()
        {
            EventManagerScript.ClearAll();
        }
    }
}
