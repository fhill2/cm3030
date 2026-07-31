using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Game.Core;
using Game.Health;
using Game.Shared;
using Game.Audio;

namespace Game.UI
{
    /// <summary>
    /// Combat demo/recording harness.
    /// Six keyboard keys (1-6) drive player and enemy combat actions.
    /// Two health bars react to OnDamage/OnDeath events in real time.
    ///
    /// Keys:  1 Player Hit   2 Player Damage   3 Player Death
    ///        4 Enemy  Hit   5 Enemy  Damage   6 Enemy  Death
    ///
    /// "Hit" plays an effort grunt + fires the Attack animation trigger.
    /// "Damage" calls TakeDamage for 20% of max HP.
    /// "Death" calls Kill().
    /// </summary>
    public class CombatDemoController : MonoBehaviour
    {
        private const float ButtonHeight = 44f;
        private const float Spacing      = 10f;
        private const float Margin       = 20f;
        private const float BarWidth     = 400f;
        private const float BarHeight    = 24f;
        private const float DamageRatio  = 0.2f;

        [Header("Actors")]
        [SerializeField] private HealthSystem playerHealth;
        [SerializeField] private HealthSystem enemyHealth;
        [SerializeField] private Animator    playerAnimator;
        [SerializeField] private Animator    enemyAnimator;
        [SerializeField] private ActorAudio  playerAudio;
        [SerializeField] private ActorAudio  enemyAudio;

        private Image playerHealthFill;
        private Image enemyHealthFill;
        private Text  playerHealthLabel;
        private Text  enemyHealthLabel;

        private static Font s_font;

        // ── Lifecycle ───────────────────────────────────────────

        void Awake()
        {
            EnsureEventSystem();
            ResolvePlayerReferences();
            BuildUI();
            RefreshAll();
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

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit1Key.wasPressedThisFrame) PlayerHit();
            if (kb.digit2Key.wasPressedThisFrame) PlayerDamage();
            if (kb.digit3Key.wasPressedThisFrame) PlayerDeath();
            if (kb.digit4Key.wasPressedThisFrame) EnemyHit();
            if (kb.digit5Key.wasPressedThisFrame) EnemyDamage();
            if (kb.digit6Key.wasPressedThisFrame) EnemyDeath();
        }

        // ── Reference resolution ────────────────────────────────

        void ResolvePlayerReferences()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;

            if (playerHealth   == null) playerHealth   = player.GetComponent<HealthSystem>();
            if (playerAnimator == null) playerAnimator = player.GetComponentInChildren<Animator>();
            if (playerAudio    == null) playerAudio    = player.GetComponent<ActorAudio>();
        }

        // ── Event handlers ──────────────────────────────────────

        void HandleDamage(DamageArgs e)
        {
            if (e.Target == playerHealth?.gameObject) RefreshPlayer();
            if (e.Target == enemyHealth?.gameObject)  RefreshEnemy();
        }

        void HandleDeath(DeathArgs e)
        {
            if (e.Entity == playerHealth?.gameObject) RefreshPlayer();
            if (e.Entity == enemyHealth?.gameObject)  RefreshEnemy();
        }

        // ── Actions (keys 1-6) ──────────────────────────────────

        void PlayerHit()
        {
            if (playerAnimator != null) playerAnimator.SetTrigger(AnimParams.Attack);
            playerAudio?.PlayEffort();
        }

        void PlayerDamage()
        {
            playerHealth?.TakeDamage(playerHealth.MaxHealth * DamageRatio,
                                     DamageType.Heavy, gameObject);
        }

        void PlayerDeath() => playerHealth?.Kill();

        void EnemyHit()
        {
            if (enemyAnimator != null) enemyAnimator.SetTrigger(AnimParams.Attack);
            enemyAudio?.PlayEffort();
        }

        void EnemyDamage()
        {
            enemyHealth?.TakeDamage(enemyHealth.MaxHealth * DamageRatio,
                                    DamageType.Heavy, gameObject);
        }

        void EnemyDeath() => enemyHealth?.Kill();

        // ── Health bar refresh ──────────────────────────────────

        void RefreshAll() { RefreshPlayer(); RefreshEnemy(); }

        void RefreshPlayer()
        {
            if (playerHealth == null) return;
            float ratio = playerHealth.MaxHealth > 0f
                ? playerHealth.CurrentHealth / playerHealth.MaxHealth : 0f;
            if (playerHealthFill  != null) playerHealthFill.fillAmount = ratio;
            if (playerHealthLabel != null)
                playerHealthLabel.text =
                    $"PLAYER  {playerHealth.CurrentHealth:0} / {playerHealth.MaxHealth:0}";
        }

        void RefreshEnemy()
        {
            if (enemyHealth == null) return;
            float ratio = enemyHealth.MaxHealth > 0f
                ? enemyHealth.CurrentHealth / enemyHealth.MaxHealth : 0f;
            if (enemyHealthFill  != null) enemyHealthFill.fillAmount = ratio;
            if (enemyHealthLabel != null)
                enemyHealthLabel.text =
                    $"ENEMY  {enemyHealth.CurrentHealth:0} / {enemyHealth.MaxHealth:0}";
        }

        // ── UI construction ─────────────────────────────────────

        void BuildUI()
        {
            var canvasGo = new GameObject("CombatDemoCanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            Transform t = canvasGo.transform;

            // ── Player health bar (top-left) ────────────────────
            playerHealthFill = CreateHealthBar(t,
                new Vector2(0, 1), new Vector2(Margin, -Margin),
                Color.white);
            playerHealthLabel = CreateLabelText(t, "PLAYER  --- / ---",
                new Vector2(0, 1), new Vector2(Margin, -Margin - BarHeight - 4f),
                TextAnchor.MiddleLeft, 18, Color.white);

            // ── Enemy health bar (top-right) ────────────────────
            enemyHealthFill = CreateHealthBar(t,
                new Vector2(1, 1), new Vector2(-Margin, -Margin),
                Color.white);
            enemyHealthLabel = CreateLabelText(t, "ENEMY  --- / ---",
                new Vector2(1, 1), new Vector2(-Margin, -Margin - BarHeight - 4f),
                TextAnchor.MiddleRight, 18, Color.white);

            // ── Buttons (full width, below health bars) ─────────
            float refW = scaler.referenceResolution.x;
            float btnW = (refW - Margin * 2 - Spacing * 2) / 3f;

            float y = -Margin - BarHeight - 50f;

            CreateText(t, "COMBAT DEMO  [Keys 1-6]",
                new Vector2(0.5f, 1), new Vector2(0, y),
                TextAnchor.MiddleCenter, 22, Color.white);
            y -= 36f;

            CreateButtonRow(t, y, btnW,
                ("1 Player Hit", PlayerHit),
                ("2 Player Damage", PlayerDamage),
                ("3 Player Death", PlayerDeath));
            y -= ButtonHeight + Spacing;

            CreateButtonRow(t, y, btnW,
                ("4 Enemy Hit", EnemyHit),
                ("5 Enemy Damage", EnemyDamage),
                ("6 Enemy Death", EnemyDeath));
        }

        // ── UI helpers ──────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        void CreateButtonRow(Transform parent, float y, float btnW,
            params (string label, Action action)[] buttons)
        {
            float totalWidth = btnW * buttons.Length + Spacing * (buttons.Length - 1);
            float startX = -totalWidth * 0.5f + btnW * 0.5f;
            for (int i = 0; i < buttons.Length; i++)
            {
                var (label, action) = buttons[i];
                CreateButton(parent, label,
                    new Vector2(startX + (btnW + Spacing) * i, y), btnW, action);
            }
        }

        void CreateButton(Transform parent, string label, Vector2 pos, float btnW, Action onClick)
        {
            var rt = CreateRect(parent, new Vector2(0.5f, 1), new Vector2(0.5f, 1),
                                pos, new Vector2(btnW, ButtonHeight));
            rt.gameObject.name = $"Btn_{label}";

            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f);
            colors.pressedColor     = new Color(0.1f, 0.5f, 0.1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => onClick());

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(rt, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var txt = labelGo.AddComponent<Text>();
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = GetFont();
            txt.fontSize = 16;
            txt.raycastTarget = false;
        }

        Text CreateText(Transform parent, string content,
            Vector2 anchor, Vector2 pos, TextAnchor alignment,
            int fontSize, Color color)
        {
            var rt = CreateRect(parent, anchor, anchor, pos, new Vector2(BarWidth, fontSize + 8));
            var txt = rt.gameObject.AddComponent<Text>();
            txt.text = content;
            txt.alignment = alignment;
            txt.color = color;
            txt.font = GetFont();
            txt.fontSize = fontSize;
            txt.raycastTarget = false;
            return txt;
        }

        Text CreateLabelText(Transform parent, string content,
            Vector2 anchor, Vector2 pos, TextAnchor alignment,
            int fontSize, Color color)
        {
            var bgRt = CreateRect(parent, anchor, anchor, pos,
                new Vector2(BarWidth, fontSize + 10));
            bgRt.gameObject.name = "Label_BG";
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
            txt.color = color;
            txt.font = GetFont();
            txt.fontSize = fontSize;
            txt.raycastTarget = false;
            return txt;
        }

        Image CreateHealthBar(Transform parent, Vector2 anchor, Vector2 pos, Color fillColor)
        {
            var bgRt = CreateRect(parent, anchor, anchor, pos, new Vector2(BarWidth, BarHeight));
            bgRt.gameObject.name = "HealthBar_BG";
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
            fillImg.color = fillColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;
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
