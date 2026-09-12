using System.Collections.Generic;
using UnityEngine;

namespace Game.Core
{
    // Tracks which tomes have dropped and which spells have been bought.
    // A tome only unlocks the item for sale; buying it grants the spell.
    // Sits on the GameManager.
    public class SpellBook : MonoBehaviour
    {
        [Header("Debug")]
        [SerializeField] private bool logChanges = true;

        // Tome dropped, so the spell can appear in the market.
        private readonly HashSet<SpellDef> unlocked = new HashSet<SpellDef>();

        // Bought and castable.
        private readonly HashSet<SpellDef> owned = new HashSet<SpellDef>();

        public bool IsUnlocked(SpellDef spell)
        {
            return spell != null && unlocked.Contains(spell);
        }

        public bool IsOwned(SpellDef spell)
        {
            return spell != null && owned.Contains(spell);
        }

        // Returns false if it was already unlocked, so the pickup can tell
        // the player it was a duplicate.
        public bool Unlock(SpellDef spell)
        {
            if (spell == null) return false;
            if (!unlocked.Add(spell)) return false;

            if (logChanges) Debug.Log($"[SpellBook] Tome found: {spell.DisplayName}");
            EventManager.RaiseTomeFound(new TomeFoundArgs(spell));
            EventManager.RaiseShopChanged();
            return true;
        }

        // Called by the shop when a spell item is bought.
        public void Grant(SpellDef spell)
        {
            if (spell == null) return;
            owned.Add(spell);

            if (logChanges) Debug.Log($"[SpellBook] Learned {spell.DisplayName}");
        }

        // Strongest owned spell of a school. Higher levels replace lower ones
        // rather than sitting alongside them.
        public SpellDef BestOwned(SpellSchool school)
        {
            SpellDef best = null;
            foreach (SpellDef spell in owned)
            {
                if (spell.School != school) continue;
                if (best == null || spell.Level > best.Level) best = spell;
            }
            return best;
        }

        public void ResetForNewRun()
        {
            unlocked.Clear();
            owned.Clear();
        }
    }
}