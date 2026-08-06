using UnityEngine;

namespace Game.Core
{
    // Placeholder movement so the test cubes walk at the player.
    // Munya's enemy AI replaces this — it's only here so the spawner
    // has something visible to do.
    public class ChaseTarget : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private float stopDistance = 1.5f;
        [SerializeField] private bool logTarget = true;   // temporary, for debugging

        private Transform target;

        // Called by the spawner right after Instantiate.
        public void SetTarget(Transform t)
        {
            target = t;
        }

        private void Update()
        {
            if (target == null) return;

            if (logTarget) Debug.Log($"{name} chasing {target.name} at {target.position}");

            // Flatten to the XZ plane so the cube doesn't try to fly up at the player.
            Vector3 toTarget = target.position - transform.position;
            toTarget.y = 0f;

            if (toTarget.magnitude <= stopDistance) return;

            transform.position += toTarget.normalized * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(toTarget);
        }
    }
}