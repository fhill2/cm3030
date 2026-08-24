using UnityEngine;
using UnityEngine.InputSystem;
using Game.Combat;
using Game.Health;

namespace Game.Core
{
    // Reads the spell hotkeys and launches projectiles.
    //
    // Keys 1-3 cast fire at levels 1-3, keys 4-6 cast ice at levels 1-3.
    // A key does nothing until that spell has been bought in the market, so
    // the player can also choose a weaker, cheaper spell rather than always
    // firing their strongest.
    //
    // Aim comes from the camera rather than the player's facing, since the
    // camera is what the player is actually looking down.
    //
    // Goes on the player.
    public class SpellCaster : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Leave empty to find the SpellBook in the scene.")]
        [SerializeField] private SpellBook spellBook;

        [Tooltip("Leave empty to use Camera.main. Spells fly where the camera looks.")]
        [SerializeField] private Transform aimSource;

        [Header("Hotkeys")]
        [Tooltip("Six spells in key order: 1, 2, 3, 4, 5, 6. Fire 1-3 then ice 1-3.")]
        [SerializeField] private SpellDef[] hotkeySpells = new SpellDef[6];

        [Header("Origin")]
        [Tooltip("Height above the player's origin the projectile leaves from.")]
        [SerializeField] private float castHeight = 1.4f;

        [Tooltip("How far in front of the chest it spawns, so it clears the player's own body.")]
        [SerializeField] private float castForward = 0.6f;

        [Header("Debug")]
        [SerializeField] private bool logCasts = true;

        private StaminaSystem stamina;
        private bool isDead;

        // One cooldown per school, so casting fire doesn't lock out ice.
        private float fireReadyAt;
        private float iceReadyAt;

        private void Awake()
        {
            stamina = GetComponent<StaminaSystem>();

            if (spellBook == null) spellBook = FindFirstObjectByType<SpellBook>();
            if (aimSource == null && Camera.main != null) aimSource = Camera.main.transform;
        }

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
            if (e.Entity != gameObject) return;
            isDead = true;
        }

        private void Update()
        {
            if (isDead) return;
            if (PlayerInputLock.InputLocked) return;

            Keyboard kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) TryCast(0);
            if (kb.digit2Key.wasPressedThisFrame) TryCast(1);
            if (kb.digit3Key.wasPressedThisFrame) TryCast(2);
            if (kb.digit4Key.wasPressedThisFrame) TryCast(3);
            if (kb.digit5Key.wasPressedThisFrame) TryCast(4);
            if (kb.digit6Key.wasPressedThisFrame) TryCast(5);
        }

        private void TryCast(int slot)
        {
            if (hotkeySpells == null || slot < 0 || slot >= hotkeySpells.Length) return;

            SpellDef spell = hotkeySpells[slot];
            if (spell == null) return;

            // Not bought yet, so the key does nothing.
            if (spellBook == null || !spellBook.IsOwned(spell)) return;

            if (Time.time < ReadyTimeFor(spell.School)) return;

            // Same order as PlayerAttack: check the cast can happen before
            // charging for it, so a wasted keypress costs nothing.
            if (stamina != null && !stamina.TrySpendSpell(spell.StaminaCost)) return;

            Fire(spell);
            SetReadyTime(spell.School, Time.time + spell.Cooldown);
        }

        private void Fire(SpellDef spell)
        {
            if (spell.ProjectilePrefab == null)
            {
                Debug.LogWarning($"[SpellCaster] {spell.DisplayName} has no projectile prefab.");
                return;
            }

            Vector3 direction = aimSource != null ? aimSource.forward : transform.forward;
            Vector3 origin = transform.position
                             + Vector3.up * castHeight
                             + direction * castForward;

            GameObject projectile = Instantiate(
                spell.ProjectilePrefab, origin, Quaternion.LookRotation(direction));

            SpellProjectile component = projectile.GetComponent<SpellProjectile>();
            if (component != null)
            {
                component.Launch(spell.Damage, spell.ProjectileSpeed,
                                 spell.ProjectileLifetime, gameObject);
            }

            if (logCasts) Debug.Log($"[SpellCaster] Cast {spell.DisplayName}");
        }

        private float ReadyTimeFor(SpellSchool school)
        {
            return school == SpellSchool.Fire ? fireReadyAt : iceReadyAt;
        }

        private void SetReadyTime(SpellSchool school, float time)
        {
            if (school == SpellSchool.Fire) fireReadyAt = time;
            else iceReadyAt = time;
        }
    }
}