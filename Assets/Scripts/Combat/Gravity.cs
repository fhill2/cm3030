using UnityEngine;

namespace Game.Combat
{
    public class Gravity : MonoBehaviour
    {
        private const float GroundOffset = 0.01f;
        private const float CastSkin = 0.05f;

        [Tooltip("Downward acceleration while airborne (m/s^2).")]
        [SerializeField] private float gravity = -20f;

        [Tooltip("Rotate toward the flat resting orientation while falling, so the item lands face-down instead of on its edge.")]
        [SerializeField] private bool selfRighting = false;

        public bool SelfRighting { get => selfRighting; set => selfRighting = value; }

        [Tooltip("How quickly the item rights itself while falling.")]
        [SerializeField] private float rightingSpeed = 3f;

        private Vector3 velocity;
        private Vector3 angularVelocity;
        private bool falling;
        private Transform ignoreRoot;
        private Vector3 flatLocalAxis = Vector3.up;

        public bool IsFalling => falling;

        public void Launch(Vector3 launchVelocity, Vector3 spinDegreesPerSecond, Transform ignore)
        {
            velocity = launchVelocity;
            angularVelocity = spinDegreesPerSecond;
            ignoreRoot = ignore;
            flatLocalAxis = DeriveFlatAxis();
            falling = true;
        }

        void Update()
        {
            if (!falling) return;

            float dt = Time.deltaTime;
            velocity.y += gravity * dt;

            transform.position += velocity * dt;
            transform.rotation = Quaternion.Euler(angularVelocity * dt) * transform.rotation;

            if (selfRighting)
                transform.rotation = Quaternion.Slerp(transform.rotation, FlatRotation(), rightingSpeed * dt);

            if (velocity.y < 0f && FindGround(out RaycastHit hit))
                Land(hit);
        }

        private bool FindGround(out RaycastHit result)
        {
            result = default;

            float bottom = transform.position.y - WorldBounds().min.y;
            float cast = bottom + Mathf.Abs(velocity.y) * Time.deltaTime + CastSkin;

            var hits = Physics.RaycastAll(transform.position, Vector3.down, cast, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (h.collider.transform.IsChildOf(transform)) continue;
                if (ignoreRoot != null && h.collider.transform.IsChildOf(ignoreRoot)) continue;
                if (h.collider.GetComponentInParent<Gravity>() != null) continue;

                Transform root = h.collider.transform.root;
                if (root.CompareTag("Player") || root.CompareTag("Enemy")) continue;

                result = h;
                return true;
            }

            return false;
        }

        private void Land(RaycastHit hit)
        {
            falling = false;
            velocity = Vector3.zero;
            angularVelocity = Vector3.zero;

            if (selfRighting) transform.rotation = FlatRotation();

            float shift = hit.point.y + GroundOffset - WorldBounds().min.y;
            if (shift > 0.0001f || shift < -0.0001f)
                transform.position += Vector3.up * shift;
        }

        private Bounds WorldBounds()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool any = false;

            foreach (var r in renderers)
            {
                if (!any) { bounds = r.bounds; any = true; }
                else bounds.Encapsulate(r.bounds);
            }

            return bounds;
        }

        private Quaternion FlatRotation()
        {
            Vector3 axis = transform.rotation * flatLocalAxis;
            if (Vector3.Dot(axis, Vector3.up) < 0f) axis = -axis;
            return Quaternion.FromToRotation(axis, Vector3.up) * transform.rotation;
        }

        private Vector3 DeriveFlatAxis()
        {
            Matrix4x4 toRootLocal = transform.worldToLocalMatrix;

            bool any = false;
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            foreach (var r in GetComponentsInChildren<Renderer>())
            {
                Matrix4x4 toRoot = toRootLocal * r.transform.localToWorldMatrix;
                Bounds lb = r.localBounds;

                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 p = toRoot.MultiplyPoint3x4(new Vector3(
                        (corner & 1) == 0 ? lb.min.x : lb.max.x,
                        (corner & 2) == 0 ? lb.min.y : lb.max.y,
                        (corner & 4) == 0 ? lb.min.z : lb.max.z));

                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                    any = true;
                }
            }

            if (!any) return Vector3.up;

            Vector3 size = max - min;
            if (size.x <= size.y && size.x <= size.z) return Vector3.right;
            if (size.y <= size.z) return Vector3.up;
            return Vector3.forward;
        }
    }
}
