#if UNITY_EDITOR
using UnityEngine;

// Draws the cull ring bounds in the Scene view so the trigger volume is visible while placing it.
public class CullRingGizmo : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
    }
}
#endif