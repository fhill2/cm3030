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
            var canvasGo = new GameObject("EnemyUICanvas");
            canvasGo.transform.SetParent(transform, false);
            canvasGo.transform.localPosition = new Vector3(0, HeightOffset, 0);
            canvasTransform = canvasGo.transform;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            var canvasRt = canvasGo.GetComponent<RectTransform>();
            canvasRt.sizeDelta = new Vector2(BarWorldWidth * PixelsPerUnit,
                                             BarWorldHeight * PixelsPerUnit);
            canvasRt.localScale = Vector3.one / PixelsPerUnit;

            Transform t = canvasGo.transform;
            healthFill = CreateHealthBar(t);
        }

        Image CreateHealthBar(Transform parent)
        {
            var bgRt = UIBuilder.CreateStretchChild(parent, "HealthBar_BG");
            UIBuilder.AttachImage(bgRt, new Color(0.1f, 0.1f, 0.1f, 0.8f));

            var fillImg = UIBuilder.CreateBarFill(bgRt, new Color(0.8f, 0.15f, 0.15f, 1f));
            fillImg.raycastTarget = false;
            return fillImg;
        }
    }
}
