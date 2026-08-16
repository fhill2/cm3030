using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Health;

namespace Game.UI
{
    /// <summary>
    /// World-space health bar floating above an enemy. Goes on the enemy root.
    /// Creates a small world-space canvas at +2m Y, billboards toward the camera,
    /// and refreshes on OnDamage filtered to this enemy.
    /// </summary>
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

            if (canvasTransform != null)
                canvasTransform.gameObject.SetActive(false);

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
            // World-space canvas positioned above the enemy.
            var canvasGo = new GameObject("EnemyUICanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0, HeightOffset, 0);
            canvasTransform = canvasGo.transform;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            // Size canvas in pixels, then scale down to world units so that
            // child offsets (e.g. 2px margins) are proportional, not collapsing.
            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(BarWorldWidth * PixelsPerUnit,
                                             BarWorldHeight * PixelsPerUnit);
            canvasRt.localScale = Vector3.one / PixelsPerUnit;

            Transform t = canvasGo.transform;
            healthFill = CreateHealthBar(t);
        }

        Image CreateHealthBar(Transform parent)
        {
            // Background
            var bgGo = new GameObject("HealthBar_BG");
            bgGo.transform.SetParent(parent, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);

            // Fill
            var fillGo = new GameObject("HealthBar_Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.8f, 0.15f, 0.15f, 1f); // red
            fillImg.raycastTarget = false;
            return fillImg;
        }
    }
}
