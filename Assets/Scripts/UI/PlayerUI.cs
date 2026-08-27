using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Game.Core;
using Game.Combat;
using Game.Health;

namespace Game.UI
{
    public class PlayerUI : MonoBehaviour
    {
        private const float BarWidth     = 400f;
        private const float BarHeight    = 24f;
        private const float Margin       = 20f;
        private const float CounterWidth = 280f;

        private const float LabelHeight = 28f;
        private const float RowGap      = 4f;
        private const float RowStep     = LabelHeight + RowGap;

        [Header("Death Screen")]
        [Tooltip("Seconds to fade the screen to black once the player dies.")]
        [SerializeField] private float fadeDuration = 3f;
        [Tooltip("Seconds for the message to fade up once the screen is fully black.")]
        [SerializeField] private float messageFadeDuration = 1.2f;
        [Tooltip("Artwork shown once the screen is fully black. Leave empty to load Resources/UI/End_Screen automatically. Falls back to the text message below if neither is found.")]
        [SerializeField] private Sprite endScreen;
        [Tooltip("End screen height in reference pixels (the canvas reference is 1920x1080). Width follows the image's own aspect ratio.")]
        [SerializeField] private float endScreenHeight = 600f;
        [Tooltip("Only used if no end screen sprite can be found.")]
        [SerializeField] private string deathMessage = "Camelot has Fallen";
        [SerializeField] private int messageFontSize = 72;

        // Loaded by name so no dragging is needed, same as MenuUI's logo
        private const string EndScreenResourcePath = "UI/End_Screen";

        // Gap between the bottom of the end-screen artwork and the restart prompt.
        private const float RestartGap = 48f;
        // Drop used when falling back to the text message, which is far shorter
        // than the artwork and so needs much less clearance
        private const float TextPromptOffset = 180f;
        [Header("Theme")]
        [Tooltip("Leave empty for the built-in default theme.")]
        [SerializeField] private UITheme theme;

        [Header("Icons")]
        [Tooltip("Speaker icon for the volume control. Leave empty to load Resources/UI/speaker.")]
        [SerializeField] private Sprite speakerIcon;

        private const string SpeakerResourcePath = "UI/speaker";

        [Header("Restart")]
        [Tooltip("Shown under the death message once restarting is allowed.")]
        [SerializeField] private string restartMessage = "Press R to start over";
        [SerializeField] private int restartFontSize = 28;

        [Header("Volume")]
        [Tooltip("Master volume 0-1, applied to AudioListener.volume on startup and adjusted with the -/+ hotkeys.")]
        [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
        [Tooltip("How much each -/+ key press changes the volume.")]
        [SerializeField] private float volumeStep = 0.1f;

        private const float StaminaBarHeight = 16f;
        private static readonly Color StaminaColor = new Color(0.95f, 0.8f, 0.25f, 1f);

        private HealthSystem health;
        private Image healthFill;
        private Text  healthLabel;
        private Text  enemyLabel;
        private Text  goldLabel;
        private Text  waveLabel;
        private Image fadeOverlay;
        private Text  deathText;
        // Whichever of the end-screen image or the text message got built
        private Graphic deathVisual;
        private Text  restartText;
        private Coroutine deathRoutine;

        // Alessio's stamina system (sheet row 10), which replaced the
        // PlayerStamina placeholder. Null-safe: the bar just shows its default
        // text if the component isn't on the player
        private StaminaSystem stamina;
        private Image staminaFill;
        private Text  staminaLabel;

        private RunController runController;

        // Both live on the GameManager, not the player, so they're resolved in Start; Only used to seed the opening values — after that gold and wave arrive on the event bus
        private PlayerWallet wallet;
        private GameStateMachine stateMachine;

        private Image volumeFill;

        private GameObject hudRoot;

        private static Sprite s_speaker;

        private UITheme Theme => theme != null ? theme : UITheme.Default;

        void Awake()
        {
            health = GetComponent<HealthSystem>();
            stamina = GetComponent<StaminaSystem>();

            volume = 0.5f;
            AudioListener.volume = Mathf.Pow(volume, 2f);

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

            // Guard against a second death event re-running the sequence
            if (deathRoutine == null) deathRoutine = StartCoroutine(DeathSequence());
        }

        IEnumerator DeathSequence()
        {
            yield return FadeTo(fadeOverlay, 1f, fadeDuration);
            yield return FadeTo(deathVisual, 1f, messageFadeDuration);
        }

        /// <summary>
        /// Loads the end screen whichever way the texture is imported. Sprite
        /// Mode "Single" answers Load&lt;Sprite&gt;; "Multiple" keeps the
        /// Texture2D as the main asset and only LoadAll finds the sprites
        /// </summary>
        static Sprite LoadEndScreen()
        {
            var single = Resources.Load<Sprite>(EndScreenResourcePath);
            if (single != null) return single;

            var sliced = Resources.LoadAll<Sprite>(EndScreenResourcePath);
            return sliced != null && sliced.Length > 0 ? sliced[0] : null;
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

        void HandleGameStateChanged(GameStateChangedArgs e)
        {
            SetWave(e.Wave);

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
                staminaFill.color = StaminaColor;
            }

            if (staminaLabel != null)
            {
                staminaLabel.text = $"STAMINA  {stamina.Current:0} / {stamina.Max:0}";
            }
        }

        // Only shown once the run is over and the restart delay has passed
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
            AudioListener.volume = Mathf.Pow(volume, 2f);

            if (volumeFill != null)
                volumeFill.rectTransform.anchorMax = new Vector2(volume, 1f);
        }

        void BuildVolumeControl(Transform parent)
        {
            Sprite speaker = GetSpeakerIcon();
            if (speaker != null)
                UIBuilder.CreateIcon(parent, "SpeakerIcon", speaker,
                    new Vector2(0, 0), new Vector2(Margin, Margin), 32f);

            CreateGlyph(parent, "-", new Vector2(Margin + 38f, Margin + 2f));
            volumeFill = UIBuilder.CreateBar(parent, new Vector2(0, 0),
                new Vector2(Margin + 54f, Margin + 11f), 140f, 10f, Color.white,
                Theme.panelBackground);
            volumeFill.raycastTarget = false;
            volumeFill.rectTransform.anchorMax = new Vector2(volume, 1f);
            CreateGlyph(parent, "+", new Vector2(Margin + 198f, Margin + 2f));

            CreatePlainText(parent, "- / + adjust volume",
                new Vector2(Margin, Margin + 38f), 214f);
        }

        Text CreateGlyph(Transform parent, string content, Vector2 pos)
        {
            var rt = UIBuilder.CreateRect(parent, new Vector2(0, 0), new Vector2(0, 0), pos, new Vector2(16f, 28f));
            var txt = rt.gameObject.AddComponent<Text>();
            txt.text = content;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = UIBuilder.GetFont(Theme);
            txt.fontSize = 22;
            txt.raycastTarget = false;
            return txt;
        }

        Text CreatePlainText(Transform parent, string content, Vector2 pos, float width)
        {
            var rt = UIBuilder.CreateRect(parent, new Vector2(0, 0), new Vector2(0, 0), pos, new Vector2(width, 16f));
            var txt = rt.gameObject.AddComponent<Text>();
            txt.text = content;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.color = new Color(1f, 1f, 1f, 0.6f);
            txt.font = UIBuilder.GetFont(Theme);
            txt.fontSize = 12;
            txt.raycastTarget = false;
            return txt;
        }

        Sprite GetSpeakerIcon()
        {
            if (speakerIcon != null) return speakerIcon;
            if (s_speaker == null)
            {
                s_speaker = UIBuilder.LoadIcon(SpeakerResourcePath);
                if (s_speaker == null)
                    Debug.LogWarning($"[PlayerUI] No sprite found at Resources/{SpeakerResourcePath}. " +
                                     "The volume control shows without its icon. " +
                                     "Set the texture's Texture Type to 'Sprite (2D and UI)'.");
            }
            return s_speaker;
        }

        void BuildUI()
        {
            var canvas = UIBuilder.CreateOverlayCanvas("PlayerUICanvas");
            hudRoot = canvas.gameObject;
            Transform t = canvas.transform;

            float barsY = 1080f * 0.04f + 32f + 16f;
            staminaLabel = UIBuilder.CreateLabel(t, "STAMINA  --- / ---",
                new Vector2(0.5f, 0f), new Vector2(0f, barsY),
                BarWidth, TextAnchor.MiddleCenter, Theme);
            staminaFill = UIBuilder.CreateBar(t, new Vector2(0.5f, 0f), new Vector2(0f, barsY + RowStep),
                BarWidth, StaminaBarHeight, StaminaColor, Theme.panelBackground);

            float healthY = barsY + RowStep + StaminaBarHeight + 6f;
            healthLabel = UIBuilder.CreateLabel(t, "PLAYER  --- / ---",
                new Vector2(0.5f, 0f), new Vector2(0f, healthY),
                BarWidth, TextAnchor.MiddleCenter, Theme);
            healthFill = UIBuilder.CreateBar(t, new Vector2(0.5f, 0f), new Vector2(0f, healthY + RowStep),
                BarWidth, BarHeight, Color.white, Theme.panelBackground);

            Vector2 topRight = new Vector2(1, 1);
            goldLabel = UIBuilder.CreateLabel(t, "GOLD  --",
                topRight, new Vector2(-Margin, -Margin),
                CounterWidth, TextAnchor.MiddleRight, Theme);
            waveLabel = UIBuilder.CreateLabel(t, "WAVE  --",
                topRight, new Vector2(-Margin, -Margin - RowStep),
                CounterWidth, TextAnchor.MiddleRight, Theme);
            enemyLabel = UIBuilder.CreateLabel(t, "ENEMIES REMAINING  --",
                topRight, new Vector2(-Margin, -Margin - RowStep * 2f),
                CounterWidth, TextAnchor.MiddleRight, Theme);

            BuildVolumeControl(t);

            var hotbar = GetComponent<HotbarUI>();
            if (hotbar == null) hotbar = gameObject.AddComponent<HotbarUI>();
            hotbar.BuildInto(t);

            BuildDeathScreen(t);
        }

        void BuildDeathScreen(Transform parent)
        {
            var fadeGo = new GameObject("DeathFade");
            fadeGo.transform.SetParent(parent, false);
            var fadeRt = fadeGo.AddComponent<RectTransform>();
            UIBuilder.Stretch(fadeRt);
            fadeOverlay = fadeGo.AddComponent<Image>();
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            fadeOverlay.raycastTarget = false;

            // Artwork if we have it, the text message if error or missing. The image is centered and sized to its own aspect ratio, the text fills the screen
            if (endScreen == null) endScreen = LoadEndScreen();

            if (endScreen != null)
            {
                var imgGo = new GameObject("EndScreen");
                imgGo.transform.SetParent(fadeGo.transform, false);
                var imgRt = imgGo.AddComponent<RectTransform>();
                imgRt.anchorMin = new Vector2(0.5f, 0.5f);
                imgRt.anchorMax = new Vector2(0.5f, 0.5f);
                imgRt.pivot     = new Vector2(0.5f, 0.5f);

                // Size from the sprite's own aspect
                float aspect = endScreen.rect.height > 0f
                    ? endScreen.rect.width / endScreen.rect.height : 1f;
                imgRt.sizeDelta = new Vector2(endScreenHeight * aspect, endScreenHeight);
                imgRt.anchoredPosition = Vector2.zero;

                var img = imgGo.AddComponent<Image>();
                img.sprite = endScreen;
                img.preserveAspect = true;
                img.color = new Color(1f, 1f, 1f, 0f);
                img.raycastTarget = false;
                deathVisual = img;
            }
            else
            {
                // Parented to the sheet so it always draws above the black.
                var textGo = new GameObject("DeathMessage");
                textGo.transform.SetParent(fadeGo.transform, false);
                var textRt = textGo.AddComponent<RectTransform>();
                UIBuilder.Stretch(textRt);
                deathText = textGo.AddComponent<Text>();
                deathText.text = deathMessage;
                deathText.alignment = TextAnchor.MiddleCenter;
                deathText.font = UIBuilder.GetFont(Theme);
                deathText.fontSize = messageFontSize;
                // Red, but starting fully transparent
                deathText.color = new Color(1f, 0f, 0f, 0f);
                deathText.raycastTarget = false;
                deathVisual = deathText;
            }

            // Restart prompt, clear of whatever sits above it
            float promptOffsetY = deathVisual is Image
                ? -(endScreenHeight * 0.5f) - RestartGap
                : -TextPromptOffset;

            var restartGo = new GameObject("RestartPrompt");
            restartGo.transform.SetParent(fadeGo.transform, false);
            var restartRt = restartGo.AddComponent<RectTransform>();
            restartRt.anchorMin = Vector2.zero;
            restartRt.anchorMax = Vector2.one;
            restartRt.offsetMin = new Vector2(0f, promptOffsetY);
            restartRt.offsetMax = new Vector2(0f, promptOffsetY);
            restartText = restartGo.AddComponent<Text>();
            restartText.text = restartMessage;
            restartText.alignment = TextAnchor.MiddleCenter;
            restartText.font = UIBuilder.GetFont(Theme);
            restartText.fontSize = restartFontSize;
            restartText.color = new Color(1f, 1f, 1f, 0f);
            restartText.raycastTarget = false;
        }
    }
}