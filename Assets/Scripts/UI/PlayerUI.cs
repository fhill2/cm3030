using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Health;

namespace Game.UI
{
    /// <summary>
    /// Screen-space health bar for the player. Goes on the player root.
    /// Creates its own overlay canvas and refreshes on OnDamage/OnDeath.
    /// </summary>
    public class PlayerUI : MonoBehaviour
    {
        private const float BarWidth  = 400f;
        private const float BarHeight = 24f;
        private const float Margin    = 20f;

        private HealthSystem health;
        private Image healthFill;
        private Text  healthLabel;

        private static Font s_font;

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
            EventManager.OnDeath  += HandleDeath;
        }

        void OnDisable()
        {
            EventManager.OnDamage -= HandleDamage;
            EventManager.OnDeath  -= HandleDeath;
        }

        void HandleDamage(DamageArgs e)
        {
            if (e.Target == gameObject) Refresh();
        }

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity == gameObject) Refresh();
        }

        void Refresh()
        {
            if (health == null) return;
            float ratio = health.MaxHealth > 0f
                ? health.CurrentHealth / health.MaxHealth : 0f;
            if (healthFill != null)
                healthFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            if (healthLabel != null)
                healthLabel.text =
                    $"PLAYER  {health.CurrentHealth:0} / {health.MaxHealth:0}";
        }

        void BuildUI()
        {
            var canvasGo = new GameObject("PlayerUICanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform t = canvasGo.transform;

            healthFill = CreateHealthBar(t,
                new Vector2(0, 1), new Vector2(Margin, -Margin));
            healthLabel = CreateLabelText(t, "PLAYER  --- / ---",
                new Vector2(0, 1), new Vector2(Margin, -Margin - BarHeight - 4f));
        }

        Text CreateLabelText(Transform parent, string content, Vector2 anchor, Vector2 pos)
        {
            var bgRt = CreateRect(parent, anchor, anchor, pos,
                new Vector2(BarWidth, 28));
            var bgImg = bgRt.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(bgRt, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8, 2);
            textRt.offsetMax = new Vector2(-8, -2);
            var txt = textGo.AddComponent<Text>();
            txt.text = content;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = Color.white;
            txt.font = GetFont();
            txt.fontSize = 18;
            txt.raycastTarget = false;
            return txt;
        }

        Image CreateHealthBar(Transform parent, Vector2 anchor, Vector2 pos)
        {
            var bgRt = CreateRect(parent, anchor, anchor, pos, new Vector2(BarWidth, BarHeight));
            var bgImg = bgRt.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var fillGo = new GameObject("HealthBar_Fill");
            fillGo.transform.SetParent(bgRt, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = Color.white;
            return fillImg;
        }

        static RectTransform CreateRect(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject();
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        static Font GetFont()
        {
            if (s_font != null) return s_font;
            s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return s_font;
        }
    }
}
