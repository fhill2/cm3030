using UnityEngine;
using UnityEngine.InputSystem;
using Game.Combat;
using Game.Health;

namespace Game.Core
{
    // Reads the spell hotkeys and launches projectiles.
    //
    // Key 1 casts the strongest owned fire spell, key 2 the strongest ice.
    // Nothing happens if that school hasn't been bought yet.
    //
    // Aim comes from the camera rather than the player's facing, since the
    // camera is what the player is actually looking down.
    //
    // Goes on the player.
    public class SpellCaster : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Leave empty to find the SpellBook on the GameManager.")]
        [SerializeField] private SpellBook spellBook;

        [Tooltip("Leave empty to use Camera.main. Spells fly where the camera looks.")]
        [SerializeField] private Transform aimSource;

        [Header("Origin")]
        [Tooltip("Height above the player's origin the projectile leaves from.")]
        [SerializeField] private float castHeight = 1.4f;

        [Tooltip("How far in front of the chest it spawns, so it clears the player's own body.")]
        [SerializeField] private float castForward = 0.6f;

        [Header("Debug")]
        [SerializeField] private bool logCasts = true;

        private StaminaSystem stamina;
        private bool isDead;

        // Cooldowns are per school, so casting fire doesn't lock out ice.
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
            if (Keyboard.current == null) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame) TryCast(SpellSchool.Fire);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) TryCast(SpellSchool.Ice);
        }

        private void TryCast(SpellSchool school)
        {
            if (spellBook == null) return;

            SpellDef spell = spellBook.BestOwned(school);
            if (spell == null) return;   // not bought yet

            if (Time.time < ReadyTimeFor(school)) return;

            // Same order as PlayerAttack: check the cast can happen before
            // charging for it, so a wasted keypress costs nothing.
            if (stamina != null && !stamina.TrySpendSpell(spell.StaminaCost)) return;

            Fire(spell);
            SetReadyTime(school, Time.time + spell.Cooldown);
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