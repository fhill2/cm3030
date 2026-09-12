using UnityEngine;

namespace Game.Combat
{
    // Simple falling for dropped gear. Not a Rigidbody, so a thrown axe can't
    // shove the player or pile up against other items.
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

            float deltaTime = Time.deltaTime;
            velocity.y += gravity * deltaTime;

            transform.position += velocity * deltaTime;
            transform.rotation = Quaternion.Euler(angularVelocity * deltaTime) * transform.rotation;

            if (selfRighting)
                transform.rotation = Quaternion.Slerp(transform.rotation, FlatRotation(), rightingSpeed * deltaTime);

            if (velocity.y < 0f && FindGround(out RaycastHit hit))
                Land(hit);
        }

        // Looks for ground below, skipping the item itself, the actor it came
        // from, other falling gear, and any character.
        private bool FindGround(out RaycastHit result)
        {
            result = default;

            float bottom = transform.position.y - WorldBounds().min.y;
            float castDistance = bottom + Mathf.Abs(velocity.y) * Time.deltaTime + CastSkin;

            RaycastHit[] hits = Physics.RaycastAll(transform.position, Vector3.down, castDistance, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform)) continue;
                if (ignoreRoot != null && hit.collider.transform.IsChildOf(ignoreRoot)) continue;
                if (hit.collider.GetComponentInParent<Gravity>() != null) continue;

                Transform root = hit.collider.transform.root;
                if (root.CompareTag("Player") || root.CompareTag("Enemy")) continue;

                result = hit;
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

            // Nudge so the item's lowest point sits just above the surface,
            // rather than its origin.
            float shift = hit.point.y + GroundOffset - WorldBounds().min.y;
            if (shift > 0.0001f || shift < -0.0001f)
                transform.position += Vector3.up * shift;
        }

        private Bounds WorldBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            Bounds bounds = new Bounds(transform.position, Vector3.zero);
            bool hasBounds = false;

            foreach (Renderer renderer in renderers)
            {
                if (!hasBounds) { bounds = renderer.bounds; hasBounds = true; }
                else bounds.Encapsulate(renderer.bounds);
            }

            return bounds;
        }

        // Rotation that puts the item's flattest axis pointing up.
        private Quaternion FlatRotation()
        {
            Vector3 axis = transform.rotation * flatLocalAxis;
            if (Vector3.Dot(axis, Vector3.up) < 0f) axis = -axis;
            return Quaternion.FromToRotation(axis, Vector3.up) * transform.rotation;
        }

        // Measures the item's own bounding box and takes its shortest side as
        // the axis to lay flat, so a sword lands on its side and a shield on
        // its face without either being told which is which.
        private Vector3 DeriveFlatAxis()
        {
            Matrix4x4 toRootLocal = transform.worldToLocalMatrix;

            bool hasPoints = false;
            Vector3 min = Vector3.positiveInfinity;
            Vector3 max = Vector3.negativeInfinity;

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                Matrix4x4 toRoot = toRootLocal * renderer.transform.localToWorldMatrix;
                Bounds localBounds = renderer.localBounds;

                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = toRoot.MultiplyPoint3x4(new Vector3(
                        (corner & 1) == 0 ? localBounds.min.x : localBounds.max.x,
                        (corner & 2) == 0 ? localBounds.min.y : localBounds.max.y,
                        (corner & 4) == 0 ? localBounds.min.z : localBounds.max.z));

                    min = Vector3.Min(min, point);
                    max = Vector3.Max(max, point);
                    hasPoints = true;
                }
            }

            if (!hasPoints) return Vector3.up;

            Vector3 size = max - min;
            if (size.x <= size.y && size.x <= size.z) return Vector3.right;
            if (size.y <= size.z) return Vector3.up;
            return Vector3.forward;
        }
    }
}