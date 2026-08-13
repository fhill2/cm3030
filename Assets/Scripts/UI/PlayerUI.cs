using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Game.Core;
using Game.Health;

namespace Game.UI
{
    /// <summary>
    /// Screen-space HUD for the player: a health bar top-left, a stamina bar
    /// beneath it (see PlayerStamina — placeholder until row 10 lands), an
    /// enemies-remaining counter top-right, and the death screen. Goes on the
    /// player root. Creates its own overlay canvas; health/enemy count refresh
    /// on events, stamina refreshes every frame since it changes continuously.
    /// </summary>
    public class PlayerUI : MonoBehaviour
    {
        private const float BarWidth     = 400f;
        private const float BarHeight    = 24f;
        private const float Margin       = 20f;
        private const float CounterWidth = 280f;

        [Header("Death Screen")]
        [Tooltip("Seconds to fade the screen to black once the player dies.")]
        [SerializeField] private float fadeDuration = 3f;
        [Tooltip("Seconds for the message to fade up once the screen is fully black.")]
        [SerializeField] private float messageFadeDuration = 1.2f;
        [Tooltip("Shown once the screen is fully black.")]
        [SerializeField] private string deathMessage = "Camelot has Fallen";
        [SerializeField] private int messageFontSize = 72;

        private const float StaminaBarHeight = 16f;
        private static readonly Color StaminaColor = new Color(0.95f, 0.8f, 0.25f, 1f);

        private HealthSystem health;
        private Image healthFill;
        private Text  healthLabel;
        private Text  enemyLabel;
        private Image fadeOverlay;
        private Text  deathText;
        private Coroutine deathRoutine;

        // PLACEHOLDER (sheet row 18) — GetComponent returns null until Alessio's
        // stamina system (row 10) lands or PlayerStamina is added to the player
        // prefab; the bar just shows its default text until then.
        private PlayerStamina stamina;
        private Image staminaFill;
        private Text  staminaLabel;

        private static Font s_font;

        void Awake()
        {
            health = GetComponent<HealthSystem>();
            stamina = GetComponent<PlayerStamina>();
            BuildUI();
        }

        void Start()
        {
            Refresh();
            RefreshEnemyCount();
            RefreshStamina();
        }

        // Stamina isn't event-driven like health (it changes continuously
        // while blocking/regenerating), so it needs a per-frame poll.
        void Update()
        {
            RefreshStamina();
        }

        void OnEnable()
        {
            EventManager.OnDamage += HandleDamage;
            EventManager.OnDeath  += HandleDeath;
            EnemyHealth.OnAliveCountChanged += RefreshEnemyCount;
        }

        void OnDisable()
        {
            EventManager.OnDamage -= HandleDamage;
            EventManager.OnDeath  -= HandleDeath;
            EnemyHealth.OnAliveCountChanged -= RefreshEnemyCount;
        }

        void HandleDamage(DamageArgs e)
        {
            if (e.Target == gameObject) Refresh();
        }

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity != gameObject) return;

            Refresh();

            // Guard against a second death event re-running the sequence and
            // flashing the screen back from black.
            if (deathRoutine == null) deathRoutine = StartCoroutine(DeathSequence());
        }

        // Black out over the camera's climb, then bring the message up once the screen has settled
        IEnumerator DeathSequence()
        {
            yield return FadeTo(fadeOverlay, 1f, fadeDuration);
            yield return FadeTo(deathText, 1f, messageFadeDuration);
        }

        static IEnumerator FadeTo(Graphic target, float to, float duration)
        {
            if (target == null) yield break;

            float from = target.color.a;
            float span = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            while (elapsed < span)
            {
                elapsed += Time.deltaTime;
                SetAlpha(target, Mathf.Lerp(from, to, elapsed / span));
                yield return null;
            }
            SetAlpha(target, to);
        }

        static void SetAlpha(Graphic target, float alpha)
        {
            Color c = target.color;
            c.a = alpha;
            target.color = c;
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

        void RefreshEnemyCount()
        {
            if (enemyLabel != null)
                enemyLabel.text = $"ENEMIES REMAINING  {EnemyHealth.AliveCount}";
        }

        void RefreshStamina()
        {
            if (stamina == null) return;
            float ratio = stamina.MaxStamina > 0f
                ? stamina.CurrentStamina / stamina.MaxStamina : 0f;
            if (staminaFill != null)
                staminaFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
            if (staminaLabel != null)
                staminaLabel.text =
                    $"STAMINA  {stamina.CurrentStamina:0} / {stamina.MaxStamina:0}";
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

            healthFill = CreateBar(t, new Vector2(0, 1), new Vector2(Margin, -Margin),
                BarWidth, BarHeight, Color.white);
            healthLabel = CreateLabelText(t, "PLAYER  --- / ---",
                new Vector2(0, 1), new Vector2(Margin, -Margin - BarHeight - 4f),
                BarWidth, TextAnchor.MiddleLeft);

            // Stamina bar stacks directly under the health label. PLACEHOLDER
            // (row 18) — stays populated with default text until PlayerStamina
            // is on the player (see that script for why it's a stand-in).
            float staminaY = -Margin - BarHeight - 4f - 28f - 6f;
            staminaFill = CreateBar(t, new Vector2(0, 1), new Vector2(Margin, staminaY),
                BarWidth, StaminaBarHeight, StaminaColor);
            staminaLabel = CreateLabelText(t, "STAMINA  --- / ---",
                new Vector2(0, 1), new Vector2(Margin, staminaY - StaminaBarHeight - 4f),
                BarWidth, TextAnchor.MiddleLeft);

            // Top-right corner. CreateRect pins the pivot to the anchor, so a
            // (1,1) anchor with a negative offset hangs the box inward from the corner and it stays put at any resolution
            enemyLabel = CreateLabelText(t, "ENEMIES REMAINING  --",
                new Vector2(1, 1), new Vector2(-Margin, -Margin),
                CounterWidth, TextAnchor.MiddleRight);

            // Built last on purpose: within a canvas, later siblings draw top, so this covers the health bar and counter when it fades in
            BuildDeathScreen(t);
        }

        void BuildDeathScreen(Transform parent)
        {
            // Full-screen black sheet, invisible until death.
            var fadeGo = new GameObject("DeathFade");
            fadeGo.transform.SetParent(parent, false);
            var fadeRt = fadeGo.AddComponent<RectTransform>();
            fadeRt.anchorMin = Vector2.zero;
            fadeRt.anchorMax = Vector2.one;
            fadeRt.offsetMin = Vector2.zero;
            fadeRt.offsetMax = Vector2.zero;
            fadeOverlay = fadeGo.AddComponent<Image>();
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            // It spans the screen from the first frame, so it must never
            // swallow clicks while it's still transparent.
            fadeOverlay.raycastTarget = false;

            // Parented to the sheet so it always draws above the black.
            var textGo = new GameObject("DeathMessage");
            textGo.transform.SetParent(fadeGo.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            deathText = textGo.AddComponent<Text>();
            deathText.text = deathMessage;
            deathText.alignment = TextAnchor.MiddleCenter;
            deathText.font = GetFont();
            deathText.fontSize = messageFontSize;
            // Red, but starting fully transparent
            deathText.color = new Color(1f, 0f, 0f, 0f);
            deathText.raycastTarget = false;
        }

        Text CreateLabelText(Transform parent, string content, Vector2 anchor, Vector2 pos,
            float width, TextAnchor alignment)
        {
            var bgRt = CreateRect(parent, anchor, anchor, pos,
                new Vector2(width, 28));
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
            txt.alignment = alignment;
            txt.color = Color.white;
            txt.font = GetFont();
            txt.fontSize = 18;
            txt.raycastTarget = false;
            return txt;
        }

        // Generic filled bar: a dark background with an inset fill image whose
        // anchorMax.x drives the percentage. Used for both health and stamina.
        Image CreateBar(Transform parent, Vector2 anchor, Vector2 pos, float width, float height, Color fillColor)
        {
            var bgRt = CreateRect(parent, anchor, anchor, pos, new Vector2(width, height));
            var bgImg = bgRt.gameObject.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var fillGo = new GameObject("Bar_Fill");
            fillGo.transform.SetParent(bgRt, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = fillColor;
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
