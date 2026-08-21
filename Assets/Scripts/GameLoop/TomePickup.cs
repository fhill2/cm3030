using UnityEngine;

namespace Game.Core
{
    // The tome lying on the ground. Walking into it unlocks the spell for
    // purchase in the market.
    //
    // Goes on the tome pickup prefab, which needs a trigger collider.
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
            if (!other.CompareTag("Player")) return;

            SpellBook book = FindFirstObjectByType<SpellBook>();
            if (book == null) return;

            book.Unlock(spell);
            Destroy(gameObject);
        }
    }
}