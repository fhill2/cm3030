using UnityEngine;

namespace Game.Core
{
    // The tome lying on the ground. Walking into it unlocks the spell for
    // purchase in the market.
    //
    // Goes on the tome pickup prefab, which needs a trigger collider and a
    // kinematic Rigidbody.
    public class TomePickup : MonoBehaviour
    {
        [Header("Contents")]
        [Tooltip("Set by TomeDrop at runtime. Assign here only for testing.")]
        [SerializeField] private SpellDef spell;

        [Header("Motion")]
        [Tooltip("Degrees per second the tome spins, so it reads as a pickup.")]
        [SerializeField] private float spinSpeed = 60f;
        [SerializeField] private float bobHeight = 0.2f;
        [SerializeField] private float bobSpeed = 2f;

        [Header("Lifetime")]
        [Tooltip("Seconds before an uncollected tome disappears. 0 means it stays forever.")]
        [SerializeField] private float lifetime = 0f;

        [Header("Debug")]
        [Tooltip("Log every trigger entry, so a pickup that does nothing can be traced.")]
        [SerializeField] private bool logTriggers = true;

        private Vector3 restPosition;

        public void Assign(SpellDef def)
        {
            spell = def;
        }

        private void Start()
        {
            restPosition = transform.position;
            if (lifetime > 0f) Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            transform.position = restPosition +
                Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (logTriggers)
                Debug.Log($"[TomePickup] Triggered by {other.name}, tag {other.tag}, root {other.transform.root.name} tagged {other.transform.root.tag}");

            if (!IsPlayer(other)) return;

            SpellBook book = FindFirstObjectByType<SpellBook>();
            if (book == null)
            {
                Debug.LogWarning("[TomePickup] No SpellBook in the scene, tome not collected.");
                return;
            }

            if (spell == null)
            {
                Debug.LogWarning("[TomePickup] No spell assigned to this tome.");
                return;
            }

            book.Unlock(spell);
            Destroy(gameObject);
        }

        // The collider that enters can be a child of the player rather than the
        // tagged root, so check the whole branch rather than just the collider.
        private bool IsPlayer(Collider other)
        {
            if (other.CompareTag("Player")) return true;
            return other.transform.root.CompareTag("Player");
        }
    }
}