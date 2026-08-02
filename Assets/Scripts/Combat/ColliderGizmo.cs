using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    /// Draws the attached collider in the Scene view (always visible) so hitboxes
    /// can be sized and positioned by eye. Add to any GameObject that has a
    /// BoxCollider / CapsuleCollider / SphereCollider. Set <see cref="color"/>
    /// per instance (e.g. red = weapon, green = body).
    /// </summary>
    public class ColliderGizmo : MonoBehaviour
    {
        [Tooltip("Wire colour for this collider in the Scene view.")]
        public Color color = Color.red;

        void OnDrawGizmos()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = color;
            Gizmos.matrix = transform.localToWorldMatrix;

            switch (col)
            {
                case BoxCollider b:
                    Gizmos.DrawWireCube(b.center, b.size);
                    break;
                case CapsuleCollider c:
                    DrawWireCapsule(c);
                    break;
                case SphereCollider s:
                    Gizmos.DrawWireSphere(s.center, s.radius);
                    break;
            }
        }

        // Approximates a capsule with two end spheres + side lines.
        static void DrawWireCapsule(CapsuleCollider c)
        {
            float radius = c.radius;
            float halfLen = Mathf.Max(0f, c.height * 0.5f - radius);

            Vector3 axis  = c.direction == 0 ? Vector3.right
                          : c.direction == 1 ? Vector3.up
                          : Vector3.forward;
            Vector3 top    = c.center + axis * halfLen;
            Vector3 bottom = c.center - axis * halfLen;

            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawWireSphere(bottom, radius);

            Vector3 perp1 = axis == Vector3.up    ? Vector3.right
                          : axis == Vector3.right ? Vector3.up
                          : Vector3.right;
            Vector3 perp2 = axis == Vector3.forward ? Vector3.up : Vector3.forward;

            Gizmos.DrawLine(top + perp1 * radius, bottom + perp1 * radius);
            Gizmos.DrawLine(top - perp1 * radius, bottom - perp1 * radius);
            Gizmos.DrawLine(top + perp2 * radius, bottom + perp2 * radius);
            Gizmos.DrawLine(top - perp2 * radius, bottom - perp2 * radius);
        }
    }
}
