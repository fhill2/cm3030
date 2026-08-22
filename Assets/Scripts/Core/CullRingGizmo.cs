#if UNITY_EDITOR
using UnityEngine;

public class CullRingGizmo : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
        Vector3 half = Vector3.one * 0.5f;
        Vector3 c = Vector3.zero;
        Gizmos.DrawLine(c + new Vector3(-half.x, 0, -half.z), c + new Vector3(half.x, 0, -half.z));
        Gizmos.DrawLine(c + new Vector3(half.x, 0, -half.z), c + new Vector3(half.x, 0, half.z));
        Gizmos.DrawLine(c + new Vector3(half.x, 0, half.z), c + new Vector3(-half.x, 0, half.z));
        Gizmos.DrawLine(c + new Vector3(-half.x, 0, half.z), c + new Vector3(-half.x, 0, -half.z));
    }
}
#endif
