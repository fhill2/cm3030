using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Game.Core;
using Game.Combat;
using Game.Health;

namespace Game.UI
{
    /// <summary>
    /// Screen-space HUD for the player: a health bar top-left with a stamina bar
    /// beneath it, a run-status column top-right (gold, wave, enemies
    /// remaining), and the death screen with a restart prompt. Goes on the
    /// player root. Creates its own overlay canvas; health, gold, wave and enemy
    /// count all refresh on events, stamina refreshes every frame since it
    /// changes continuously.
    /// </summary>
    public class PlayerUI : MonoBehaviour
    {
        private const float BarWidth     = 400f;
        private const float BarHeight    = 24f;
        private const float Margin       = 20f;
        private const float CounterWidth = 280f;

        // One row of the top-right run-status column 
        // CreateLabelText builds 28px-tall boxes, so a row is that plus a gap
        private const float LabelHeight = 28f;
        private const float RowGap      = 4f;
        private const float RowStep     = LabelHeight + RowGap;

        [Header("Death Screen")]
        [Tooltip("Seconds to fade the screen to black once the player dies.")]
        [SerializeField] private float fadeDuration = 3f;
        [Tooltip("Seconds for the message to fade up once the screen is fully black.")]
        [SerializeField] private float messageFadeDuration = 1.2f;
        [Tooltip("Shown once the screen is fully black.")]
        [SerializeField] private string deathMessage = "Camelot has Fallen";
        [SerializeField] private int messageFontSize = 72;

        [Header("Restart")]
        [Tooltip("Shown under the death message once restarting is allowed.")]
        [SerializeField] private string restartMessage = "Press R to start over";
        [SerializeField] private int restartFontSize = 28;

        [Header("Volume")]
        [Tooltip("Master volume 0-1, applied to AudioListener.volume on startup and adjusted with the -/+ hotkeys.")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.6f;
        [Tooltip("How much each -/+ key press changes the volume.")]
        [SerializeField] private float volumeStep = 0.1f;

        private const string VolumePrefKey = "playerui.volume";

        private const float StaminaBarHeight = 16f;
        private static readonly Color StaminaColor = new Color(0.95f, 0.8f, 0.25f, 1f);
        private static readonly Color StunnedColor = new Color(0.8f, 0.25f, 0.2f, 1f);

        private HealthSystem health;
        private Image healthFill;
        private Text  healthLabel;
        private Text  enemyLabel;
        private Text  goldLabel;
        private Text  waveLabel;
        private Image fadeOverlay;
        private Text  deathText;
        private Text  restartText;
        private Coroutine deathRoutine;

        // Alessio's stamina system (sheet row 10), which replaced the
        // PlayerStamina placeholder. Null-safe: the bar just shows its default
        // text if the component isn't on the player.
        private StaminaSystem stamina;
        private Image staminaFill;
        private Text  staminaLabel;

      
        private RunController runController;

        // Both live on the GameManager, not the player, so they're resolved in
        // Start. Only used to seed the opening values — after that gold and wave
        // arrive on the event bus.
        private PlayerWallet wallet;
        private GameStateMachine stateMachine;

        private Image volumeFill;

        // The whole HUD canvas, so it can be hidden wholesale on the start screen.
        private GameObject hudRoot;

        private static Font s_font;
        private static Sprite s_speaker;

        void Awake()
        {
            health = GetComponent<HealthSystem>();
            stamina = GetComponent<StaminaSystem>();

            volume = PlayerPrefs.GetFloat(VolumePrefKey, volume);
            AudioListener.volume = volume;

            BuildUI();
        }

        void Start()
        {
            runController = FindFirstObjectByType<RunController>();
            wallet        = FindFirstObjectByType<PlayerWallet>();
            stateMachine  = FindFirstObjectByType<GameStateMachine>();

            Refresh();
            RefreshEnemyCount();
            RefreshStamina();

            SetGold(wallet != null ? wallet.Gold : 0);
            SetWave(stateMachine != null ? stateMachine.CurrentWave : 0);
        }

        // Stamina isn't event-driven like health (it changes continuously
        // while blocking, sprinting and regenerating), so it needs a per-frame poll.
        void Update()
        {
            RefreshStamina();
            RefreshRestartPrompt();
            PollVolumeHotkeys();
        }

        void OnEnable()
        {
            EventManager.OnDamage += HandleDamage;
            EventManager.OnDeath  += HandleDeath;
            EventManager.OnGoldChanged += HandleGoldChanged;
            EventManager.OnGameStateChanged += HandleGameStateChanged;
            EnemyHealth.OnAliveCountChanged += RefreshEnemyCount;
        }

        void OnDisable()
        {
            EventManager.OnDamage -= HandleDamage;
            EventManager.OnDeath  -= HandleDeath;
            EventManager.OnGoldChanged -= HandleGoldChanged;
            EventManager.OnGameStateChanged -= HandleGameStateChanged;
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

        // Black out over the camera's climb then bring the message up once the screen has settled
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

        void HandleGoldChanged(GoldChangedArgs e) => SetGold(e.Total);

        // Wave only advances on entering WaveActive, but the payload carries the
        // number on every transition, so reading it here keeps the label right
        // through the shop and wave-complete states too.
        void HandleGameStateChanged(GameStateChangedArgs e)
        {
            SetWave(e.Wave);

            // The HUD has nothing to say on the start screen, and it would sit
            // on top of the title. Hidden in Menu only — GameOver has to keep it
            // alive so the death fade and message can play.
            if (hudRoot != null) hudRoot.SetActive(e.Current != GameStateId.Menu);
        }

        void SetGold(int total)
        {
            if (goldLabel != null) goldLabel.text = $"GOLD  {total}";
        }

        void SetWave(int wave)
        {
            if (waveLabel != null) waveLabel.text = $"WAVE  {wave}";
        }

        void RefreshStamina()
        {
            if (stamina == null) return;

            float ratio = stamina.Max > 0f ? stamina.Current / stamina.Max : 0f;

            if (staminaFill != null)
            {
                staminaFill.rectTransform.anchorMax = new Vector2(ratio, 1f);
                // Turn the bar red while stunned, so it's obvious why nothing
                // is responding.
                staminaFill.color = stamina.IsStunned ? StunnedColor : StaminaColor;
            }

            if (staminaLabel != null)
            {
                staminaLabel.text = stamina.IsStunned
                    ? "STAMINA  EXHAUSTED"
                    : $"STAMINA  {stamina.Current:0} / {stamina.Max:0}";
            }
        }

        // Only shown once the run is over and the restart delay has passed.
        void RefreshRestartPrompt()
        {
            if (restartText == null) return;

            bool show = runController != null && runController.CanRestart;
            SetAlpha(restartText, show ? 1f : 0f);
        }

        void PollVolumeHotkeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.minusKey.wasPressedThisFrame || kb.numpadMinusKey.wasPressedThisFrame)
                ApplyVolume(volume - volumeStep);

            if (kb.equalsKey.wasPressedThisFrame || kb.numpadPlusKey.wasPressedThisFrame)
                ApplyVolume(volume + volumeStep);
        }

        void ApplyVolume(float value)
        {
            volume = Mathf.Clamp01(value);
            AudioListener.volume = volume;

            if (volumeFill != null)
                volumeFill.rectTransform.anchorMax = new Vector2(volume, 1f);

            PlayerPrefs.SetFloat(VolumePrefKey, volume);
            PlayerPrefs.Save();
        }

        void BuildVolumeControl(Transform parent)
        {
            var iconRt = CreateRect(parent, new Vector2(0, 0), new Vector2(0, 0),
                new Vector2(Margin, Margin), new Vector2(32f, 32f));
            var icon = iconRt.gameObject.AddComponent<Image>();
            icon.sprite = GetSpeakerSprite();
            icon.raycastTarget = false;

            CreateGlyph(parent, "-", new Vector2(Margin + 38f, Margin + 2f));
            volumeFill = CreateBar(parent, new Vector2(0, 0),
                new Vector2(Margin + 54f, Margin + 11f), 140f, 10f, Color.white);
            volumeFill.raycastTarget = false;
            volumeFill.rectTransform.anchorMax = new Vector2(volume, 1f);
            CreateGlyph(parent, "+", new Vector2(Margin + 198f, Margin + 2f));

            CreatePlainText(parent, "- / + adjust volume",
                new Vector2(Margin, Margin + 38f), 214f);
        }

        Text CreateGlyph(Transform parent, string content, Vector2 pos)
        {
            var rt = CreateRect(parent, new Vector2(0, 0), new Vector2(0, 0), pos, new Vector2(16f, 28f));
            var txt = rt.gameObject.AddComponent<Text>();
            txt.text = content;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = GetFont();
            txt.fontSize = 22;
            txt.raycastTarget = false;
            return txt;
        }

        Text CreatePlainText(Transform parent, string content, Vector2 pos, float width)
        {
            var rt = CreateRect(parent, new Vector2(0, 0), new Vector2(0, 0), pos, new Vector2(width, 16f));
            var txt = rt.gameObject.AddComponent<Text>();
            txt.text = content;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = new Color(1f, 1f, 1f, 0.6f);
            txt.font = GetFont();
            txt.fontSize = 12;
            txt.raycastTarget = false;
            return txt;
        }

        static Sprite GetSpeakerSprite()
        {
            if (s_speaker != null) return s_speaker;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color32(0, 0, 0, 0);
            var white = new Color32(235, 235, 235, 255);

            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, clear);

            for (int y = 12; y <= 19; y++)
                for (int x = 4; x <= 11; x++)
                    tex.SetPixel(x, y, white);

            for (int x = 11; x <= 20; x++)
            {
                float t = (x - 11) / 9f;
                int half = Mathf.RoundToInt(4f + t * 6f);
                for (int y = 16 - half; y <= 15 + half; y++)
                    tex.SetPixel(x, y, white);
            }

            for (int wave = 0; wave < 2; wave++)
            {
                int r = 5 + wave * 4;
                for (int x = 22; x < size; x++)
                {
                    int dx = x - 21;
                    if (dx > r) break;
                    float dy = Mathf.Sqrt(r * r - dx * dx);
                    tex.SetPixel(x, Mathf.RoundToInt(15.5f - dy), white);
                    tex.SetPixel(x, Mathf.RoundToInt(15.5f + dy), white);
                }
            }

            tex.Apply();
            s_speaker = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 32f);
            return s_speaker;
        }

        void BuildUI()
        {
            var canvasGo = new GameObject("PlayerUICanvas");
            hudRoot = canvasGo;
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

            // Stamina bar stacks directly under the health label
            float staminaY = -Margin - BarHeight - 4f - 28f - 6f;
            staminaFill = CreateBar(t, new Vector2(0, 1), new Vector2(Margin, staminaY),
                BarWidth, StaminaBarHeight, StaminaColor);
            staminaLabel = CreateLabelText(t, "STAMINA  --- / ---",
                new Vector2(0, 1), new Vector2(Margin, staminaY - StaminaBarHeight - 4f),
                BarWidth, TextAnchor.MiddleLeft);

            // Run status stacks down the top-right corner: gold, wave, enemies.
            // CreateRect pins the pivot to the anchor, so a (1,1) anchor with a
            // negative offset hangs each row inward from the corner and the
            // whole column holds its place at any resolution
            Vector2 topRight = new Vector2(1, 1);
            goldLabel = CreateLabelText(t, "GOLD  --",
                topRight, new Vector2(-Margin, -Margin),
                CounterWidth, TextAnchor.MiddleRight);
            waveLabel = CreateLabelText(t, "WAVE  --",
                topRight, new Vector2(-Margin, -Margin - RowStep),
                CounterWidth, TextAnchor.MiddleRight);
            enemyLabel = CreateLabelText(t, "ENEMIES REMAINING  --",
                topRight, new Vector2(-Margin, -Margin - RowStep * 2f),
                CounterWidth, TextAnchor.MiddleRight);

            BuildVolumeControl(t);

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

            // Restart prompt, sitting below the death message. Offset downward
            // so the two don't overlap in the middle of the screen.
            var restartGo = new GameObject("RestartPrompt");
            restartGo.transform.SetParent(fadeGo.transform, false);
            var restartRt = restartGo.AddComponent<RectTransform>();
            restartRt.anchorMin = Vector2.zero;
            restartRt.anchorMax = Vector2.one;
            restartRt.offsetMin = new Vector2(0f, -180f);
            restartRt.offsetMax = new Vector2(0f, -180f);
            restartText = restartGo.AddComponent<Text>();
            restartText.text = restartMessage;
            restartText.alignment = TextAnchor.MiddleCenter;
            restartText.font = GetFont();
            restartText.fontSize = restartFontSize;
            restartText.color = new Color(1f, 1f, 1f, 0f);
            restartText.raycastTarget = false;
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