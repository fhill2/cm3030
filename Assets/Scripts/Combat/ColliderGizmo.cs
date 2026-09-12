using UnityEngine;

namespace Game.Combat
{
    // Draws the attached collider in the Scene view so hitboxes can be sized
    // by eye. Set the colour per instance, e.g. red for a weapon.
    public class ColliderGizmo : MonoBehaviour
    {
        [Tooltip("Wire colour for this collider in the Scene view.")]
        public Color color = Color.red;

        void OnDrawGizmos()
        {
            Collider attachedCollider = GetComponent<Collider>();
            if (attachedCollider == null) return;

            Gizmos.color = color;
            Gizmos.matrix = transform.localToWorldMatrix;

            switch (attachedCollider)
            {
                case BoxCollider box:
                    Gizmos.DrawWireCube(box.center, box.size);
                    break;
                case CapsuleCollider capsule:
                    DrawWireCapsule(capsule);
                    break;
                case SphereCollider sphere:
                    Gizmos.DrawWireSphere(sphere.center, sphere.radius);
                    break;
            }
        }

        // Approximated with two end spheres and four side lines.
        static void DrawWireCapsule(CapsuleCollider capsule)
        {
            float radius = capsule.radius;
            float halfLength = Mathf.Max(0f, capsule.height * 0.5f - radius);

            Vector3 axis  = capsule.direction == 0 ? Vector3.right
                          : capsule.direction == 1 ? Vector3.up
                          : Vector3.forward;
            Vector3 top    = capsule.center + axis * halfLength;
            Vector3 bottom = capsule.center - axis * halfLength;

            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawWireSphere(bottom, radius);

            Vector3 sideAxis = axis == Vector3.up    ? Vector3.right
                             : axis == Vector3.right ? Vector3.up
                             : Vector3.right;
            Vector3 otherSideAxis = axis == Vector3.forward ? Vector3.up : Vector3.forward;

            Gizmos.DrawLine(top + sideAxis * radius, bottom + sideAxis * radius);
            Gizmos.DrawLine(top - sideAxis * radius, bottom - sideAxis * radius);
            Gizmos.DrawLine(top + otherSideAxis * radius, bottom + otherSideAxis * radius);
            Gizmos.DrawLine(top - otherSideAxis * radius, bottom - otherSideAxis * radius);
        }
    }
}