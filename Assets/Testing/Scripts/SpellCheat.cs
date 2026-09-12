using UnityEngine;

namespace Game.Core
{
    // Test helper. Unlocks and grants every spell in the list on start, so
    // hotkeys 1-6 can be tested without picking up tomes first.
    // Goes on the GameManager.
    public class SpellCheat : MonoBehaviour
    {
        [SerializeField] private SpellBook spellBook;
        [SerializeField] private SpellDef[] spells;

        private void Start()
        {
            if (spellBook == null) spellBook = FindFirstObjectByType<SpellBook>();
            if (spellBook == null)
            {
                Debug.LogWarning("[SpellCheat] No SpellBook in the scene, spells not granted.");
                return;
            }

            int granted = 0;
            foreach (SpellDef spell in spells)
            {
                if (spell == null) continue;
                spellBook.Unlock(spell);
                spellBook.Grant(spell);
                granted++;
            }

            Debug.Log($"[SpellCheat] Granted {granted} spells, hotkeys 1-6 ready.");
        }
    }
}