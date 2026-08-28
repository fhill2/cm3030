using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Audio;
using Game.Combat;
using Game.Health;
using Game.Movement;
using Game.Shared;

namespace Game.Core
{
    public class PotionBelt : MonoBehaviour
    {
        [Header("Stock")]
        [Tooltip("Health potions in the belt at the start of a run.")]
        [SerializeField] private int startingHealth = 0;
        [Tooltip("Stamina potions in the belt at the start of a run.")]
        [SerializeField] private int startingStamina = 0;

        [Header("Restore")]
        [Tooltip("Health restored by one health potion.")]
        [SerializeField] private float healthRestore = 50f;
        [Tooltip("Stamina restored by one stamina potion.")]
        [SerializeField] private float staminaRestore = 50f;

        [Header("Cooldown")]
        [Tooltip("Seconds after drinking before the same potion type can be used again.")]
        [SerializeField] private float cooldown = 30f;

        [Header("Sound")]
        [Tooltip("Cork pop clip under Resources/ played when a potion is used.")]
        [SerializeField] private string openClip = "Drink/ESM_VCSFX_FX_foley_one_shot_bottle_cork_drink_pop_open_nearfield_04";
        [Tooltip("Drinking clip under Resources/ played when a potion is used.")]
        [SerializeField] private string drinkClip = "Drink/ESM_PG_fx_foley_item_action_pirate_mug_drinking_water_beer_wet_03";

        private HealthSystem health;
        private StaminaSystem stamina;
        private PlayerMovement movement;
        private Animator animator;

        private int healthCount;
        private int staminaCount;
        private float healthReadyAt;
        private float staminaReadyAt;

        public int HealthCount => healthCount;
        public int StaminaCount => staminaCount;
        public float HealthCooldownRemaining => Mathf.Max(0f, healthReadyAt - Time.time);
        public float StaminaCooldownRemaining => Mathf.Max(0f, staminaReadyAt - Time.time);
        public float CooldownDuration => cooldown;

        private void Awake()
        {
            health = GetComponent<HealthSystem>();
            stamina = GetComponent<StaminaSystem>();
            movement = GetComponent<PlayerMovement>();
            animator = GetComponentInChildren<Animator>();
            healthCount = startingHealth;
            staminaCount = startingStamina;
        }

        public void AddHealth() => healthCount++;

        public void AddStamina() => staminaCount++;

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (movement != null && !movement.ControlEnabled) return;

            if (kb.tKey.wasPressedThisFrame) TryUseHealth();
            if (kb.gKey.wasPressedThisFrame) TryUseStamina();
        }

        private void TryUseHealth()
        {
            if (healthCount <= 0) return;
            if (HealthCooldownRemaining > 0f) return;
            if (stamina != null && !stamina.CanAct) return;

            healthCount--;
            healthReadyAt = Time.time + cooldown;

            if (health != null) health.Heal(healthRestore);
            if (animator != null)
            {
                Debug.Log("[PotionBelt] Drink trigger sent (health)");
                animator.SetTrigger(AnimParams.Drink);
            }
            PlayDrinkSound();
        }

        private void TryUseStamina()
        {
            if (staminaCount <= 0) return;
            if (StaminaCooldownRemaining > 0f) return;
            if (stamina != null && !stamina.CanAct) return;

            staminaCount--;
            staminaReadyAt = Time.time + cooldown;

            if (stamina != null) stamina.Restore(staminaRestore);
            if (animator != null)
            {
                Debug.Log("[PotionBelt] Drink trigger sent (stamina)");
                animator.SetTrigger(AnimParams.Drink);
            }
            PlayDrinkSound();
        }

        private void PlayDrinkSound()
        {
            AudioClip pop = LoadClip(openClip);
            AudioClip drink = LoadClip(drinkClip);

            if (pop != null && drink != null)
                StartCoroutine(DrinkRoutine(pop, drink));
            else
            {
                if (pop != null) OneShotAudio.Play2D(pop, transform.position);
                if (drink != null) OneShotAudio.Play2D(drink, transform.position);
            }
        }

        private IEnumerator DrinkRoutine(AudioClip pop, AudioClip drink)
        {
            OneShotAudio.Play2D(pop, transform.position);
            yield return new WaitForSeconds(pop.length);
            OneShotAudio.Play2D(drink, transform.position);
        }

        private static AudioClip LoadClip(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip == null) Debug.LogWarning($"[PotionBelt] No clip at Resources/{path}");
            return clip;
        }
    }
}
