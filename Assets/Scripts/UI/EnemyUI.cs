using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Health;

namespace Game.UI
{
    public class EnemyUI : MonoBehaviour
    {
        private const float BarWorldWidth  = 1.0f;
        private const float BarWorldHeight = 0.15f;
        private const float HeightOffset   = 2.0f;
        private const float PixelsPerUnit  = 100f;

        private HealthSystem health;
        private Image healthFill;
        private Transform canvasTransform;

        void Awake()
        {
            health = GetComponent<HealthSystem>();
            BuildUI();
        }

        void Start()
        {
            Refresh();
        }

        void OnEnable()
        {
            EventManager.OnDamage += HandleDamage;
            EventManager.OnDeath += HandleDeath;
        }

        void OnDisable()
        {
            EventManager.OnDamage -= HandleDamage;
            EventManager.OnDeath -= HandleDeath;
        }

        void LateUpdate()
        {
            // HandleDeath should already have removed the bar, but checking
            // IsAlive here too means it can't end up stuck over a corpse.
            if (health != null && !health.IsAlive)
            {
                HandleDeath(new DeathArgs(gameObject));
                return;
            }

            if (canvasTransform != null && Camera.main != null)
                canvasTransform.LookAt(Camera.main.transform);
        }

        void HandleDamage(DamageArgs e)
        {
            if (e.Target == gameObject) Refresh();
        }

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity != gameObject) return;

            // Looked up fresh rather than using the cached field, so every
            // Canvas under this enemy goes, not just the one Awake built.
            foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
                Destroy(canvas.gameObject);

            enabled = false;
        }

        void Refresh()
        {
            if (health == null) return;
            float ratio = health.MaxHealth > 0f
                ? health.CurrentHealth / health.MaxHealth : 0f;
            if (healthFill != null)
            {
                healthFill.rectTransform.anchorMax = Vector2.one;
                healthFill.rectTransform.anchorMin = new Vector2(1f - ratio, 0f);
            }
        }

        void BuildUI()
        {
            GameObject canvasGo = new GameObject("EnemyUICanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0, HeightOffset, 0);
            canvasTransform = canvasGo.transform;

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(BarWorldWidth * PixelsPerUnit,
                                               BarWorldHeight * PixelsPerUnit);
            canvasRect.localScale = Vector3.one / PixelsPerUnit;

            healthFill = CreateHealthBar(canvasGo.transform);
        }

        Image CreateHealthBar(Transform parent)
        {
            RectTransform background = UIBuilder.CreateStretchChild(parent, "HealthBar_BG");
            UIBuilder.AttachImage(background, new Color(0.1f, 0.1f, 0.1f, 0.8f));

            Image fill = UIBuilder.CreateBarFill(background, new Color(0.8f, 0.15f, 0.15f, 1f));
            fill.raycastTarget = false;
            return fill;
        }
    }
}