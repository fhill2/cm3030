using System.Collections.Generic;
using UnityEngine;
using Game.Health;

namespace Game.Core
{
    // Rolls for a tome when this enemy dies, and spawns the pickup where
    // the body fell.
    //
    // Goes on the enemy prefab. Spells whose tome is already held are
    // skipped, so late waves stop dropping duplicates.
    public class TomeDrop : MonoBehaviour
    {
        [Header("Drop")]
        [Tooltip("Chance this enemy drops a tome at all, 0 to 1.")]
        [Range(0f, 1f)]
        [SerializeField] private float dropChance = 0.15f;

        [Tooltip("Every spell that can drop. Which one you get is weighted by each spell's own Drop Weight, so level 3 tomes stay rare.")]
        [SerializeField] private SpellDef[] dropTable;

        [Tooltip("Pickup prefab spawned on a successful roll.")]
        [SerializeField] private GameObject tomePickupPrefab;

        [Tooltip("Height above the body the tome appears, so it doesn't sink into the ground.")]
        [SerializeField] private float dropHeight = 1f;

        private void OnEnable()
        {
            EventManager.OnDeath += HandleDeath;
        }

        private void OnDisable()
        {
            EventManager.OnDeath -= HandleDeath;
        }

        private void HandleDeath(DeathArgs e)
        {
            if (e.Entity == null) return;

            // The health component can sit on a child, so match on the root.
            if (e.Entity != gameObject &&
                e.Entity.transform.root != transform.root) return;

            TryDrop();
        }

        private void TryDrop()
        {
            if (tomePickupPrefab == null) return;
            if (dropTable == null || dropTable.Length == 0) return;
            if (Random.value > dropChance) return;

            SpellDef spell = PickWeighted();
            if (spell == null) return;   // player already has them all

            GameObject pickup = Instantiate(
                tomePickupPrefab,
                transform.position + Vector3.up * dropHeight,
                tomePickupPrefab.transform.rotation);

            TomePickup component = pickup.GetComponent<TomePickup>();
            if (component != null) component.Assign(spell);
        }

        // Picks one spell, weighted by each spell's DropWeight. Spells the
        // player already holds are left out, so late drops aren't wasted.
        //
        // The roll works by laying every candidate's weight end to end on a
        // line, picking a random point on it, and taking whichever one the
        // point lands in. Bigger weight, bigger stretch of the line.
        private SpellDef PickWeighted()
        {
            SpellBook book = FindFirstObjectByType<SpellBook>();

            var candidates = new List<SpellDef>();
            float totalWeight = 0f;

            foreach (SpellDef spell in dropTable)
            {
                if (spell == null) continue;
                if (spell.DropWeight <= 0f) continue;
                if (book != null && book.IsUnlocked(spell)) continue;

                candidates.Add(spell);
                totalWeight += spell.DropWeight;
            }

            if (candidates.Count == 0) return null;

            float roll = Random.Range(0f, totalWeight);
            foreach (SpellDef spell in candidates)
            {
                roll -= spell.DropWeight;
                if (roll <= 0f) return spell;
            }

            // Floating point can leave the roll a hair above zero on the last
            // entry, so fall back to it rather than returning nothing.
            return candidates[candidates.Count - 1];
        }
    }
}