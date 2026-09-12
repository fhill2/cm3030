using UnityEngine;
using Game.Audio;
using Game.Health;

namespace Game.Core
{
    // The tome on the ground. Walking into it unlocks the spell for purchase in the market.
    // Goes on the tome pickup prefab, which needs a trigger collider and a kinematic Rigidbody.
    public class TomePickup : MonoBehaviour
    {
        [Header("Contents")]
        [Tooltip("Set by TomeDrop at runtime. Assign here only for testing.")]
        [SerializeField] private SpellDef spell;

        [Header("Motion")]
        [Tooltip("Degrees per second the tome spins, so it reads as a pickup.")]
        [SerializeField] private float spinSpeed = 60f;

        [Tooltip("Height kept above the ground point the tome settles on.")]
        [SerializeField] private float groundOffset = 0.1f;

        [Header("Lifetime")]
        [Tooltip("Seconds before an uncollected tome disappears. 0 means it stays forever.")]
        [SerializeField] private float lifetime = 0f;

        [Header("Debug")]
        [Tooltip("Log every trigger entry, so a pickup that does nothing can be traced.")]
        [SerializeField] private bool logTriggers = true;

        [Header("Sound")]
        [Tooltip("One-shot clip under Resources/ played when the tome is picked up.")]
        [SerializeField] private string pickupClip = "Spells/learn/ESM_Magic_Game_Protection_Ward_Buff_Fantasy_Spell_Cast_Conjure_Craft_Mobile_App_Special_Click";

        public void Assign(SpellDef def)
        {
            spell = def;
        }

        private void Start()
        {
            Ground();
            gameObject.AddComponent<Halo>().Place(transform.position - Vector3.up * groundOffset);
            if (lifetime > 0f) Destroy(gameObject, lifetime);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        // Drops the tome onto the first surface below it, ignoring bodies so
        // it doesn't land on the corpse it came from.
        private void Ground()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;
            RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 50f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                if (IsBody(hit.collider)) continue;
                transform.position = hit.point + Vector3.up * groundOffset;
                return;
            }
        }

        private static bool IsBody(Collider collider)
        {
            if (collider.CompareTag("Player")) return true;
            return collider.GetComponentInParent<EnemyHealth>() != null;
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

            if (!string.IsNullOrEmpty(pickupClip))
            {
                AudioClip clip = Resources.Load<AudioClip>(pickupClip);
                if (clip != null)
                    OneShotAudio.Play2D(clip, transform.position);
                else
                    Debug.LogWarning($"[TomePickup] No clip at Resources/{pickupClip}");
            }

            Destroy(gameObject);
        }

        // The collider that enters can be a child of the player rather than
        // the tagged root, so check the whole branch.
        private bool IsPlayer(Collider other)
        {
            if (other.CompareTag("Player")) return true;
            return other.transform.root.CompareTag("Player");
        }
    }
}