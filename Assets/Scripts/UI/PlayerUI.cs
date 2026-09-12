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

        // Loaded by name so nothing needs dragging in, same as MenuUI's logo.
        private const string EndScreenResourcePath = "UI/End_Screen";

        // Gap between the bottom of the end-screen artwork and the restart prompt.
        private const float RestartGap = 48f;
        // Used with the text message instead, which is much shorter than the
        // artwork and needs less clearance.
        private const float TextPromptOffset = 180f;

        [Header("Theme")]
        [Tooltip("Leave empty for the built-in default theme.")]
        [SerializeField] private UITheme theme;

        [Header("Icons")]
        [Tooltip("Speaker icon for the volume control. Leave empty to load Resources/UI/speaker.")]
        [SerializeField] private Sprite speakerIcon;

        private const string SpeakerResourcePath = "UI/speaker";
        private const string SpeakerOffResourcePath = "UI/speaker-off";

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

        // Whichever of the end-screen image or the text message got built.
        private Graphic deathVisual;
        private Text  restartText;
        private Coroutine deathRoutine;

        // Null-safe: the bar just shows its default text if the component
        // isn't on the player.
        private StaminaSystem stamina;
        private Image staminaFill;
        private Text  staminaLabel;

        private RunController runController;

        // On the GameManager rather than the player, so they're resolved in
        // Start. Only used to seed the opening values; after that gold and wave
        // arrive on the event bus.
        private PlayerWallet wallet;
        private GameStateMachine stateMachine;

        private Image volumeFill;
        private Image speakerImage;

        private GameObject hudRoot;

        private static Sprite speakerSprite;
        private static Sprite speakerOffSprite;

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

            // Guard against a second death event re-running the sequence.
            if (deathRoutine == null) deathRoutine = StartCoroutine(DeathSequence());
        }

        IEnumerator DeathSequence()
        {
            yield return FadeTo(fadeOverlay, 1f, fadeDuration);
            yield return FadeTo(deathVisual, 1f, messageFadeDuration);
        }

        // Works whichever way the texture is imported. Sprite Mode "Single"
        // answers Load<Sprite>; "Multiple" keeps the Texture2D as the main
        // asset and only LoadAll finds the sprites.
        static Sprite LoadEndScreen()
        {
            Sprite single = Resources.Load<Sprite>(EndScreenResourcePath);
            if (single != null) return single;

            Sprite[] sliced = Resources.LoadAll<Sprite>(EndScreenResourcePath);
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
            Color colour = target.color;
            colour.a = alpha;
            target.color = colour;
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

        // Only shown once the run is over and the restart delay has passed.
        void RefreshRestartPrompt()
        {
            if (restartText == null) return;

            bool show = runController != null && runController.CanRestart;
            SetAlpha(restartText, show ? 1f : 0f);
        }

        void PollVolumeHotkeys()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.minusKey.wasPressedThisFrame || keyboard.numpadMinusKey.wasPressedThisFrame)
                ApplyVolume(volume - volumeStep);

            if (keyboard.equalsKey.wasPressedThisFrame || keyboard.numpadPlusKey.wasPressedThisFrame)
                ApplyVolume(volume + volumeStep);
        }

        // Squared, so the slider tracks how loud it actually sounds rather than
        // the raw value.
        void ApplyVolume(float value)
        {
            volume = Mathf.Clamp01(value);
            AudioListener.volume = Mathf.Pow(volume, 2f);

            if (volumeFill != null)
                volumeFill.rectTransform.anchorMax = new Vector2(volume, 1f);

            if (speakerImage != null)
            {
                Sprite current = volume > 0f ? GetSpeakerIcon() : GetSpeakerOffIcon();
                if (current != null) speakerImage.sprite = current;
            }
        }

        void BuildVolumeControl(Transform parent)
        {
            Sprite speaker = volume > 0f ? GetSpeakerIcon() : GetSpeakerOffIcon();
            if (speaker != null)
            {
                speakerImage = UIBuilder.CreateIcon(parent, "SpeakerIcon", speaker,
                    new Vector2(0, 0), new Vector2(Margin, Margin), 32f);
            }
            if (speakerImage != null) speakerImage.color = new Color(0.72f, 0.72f, 0.72f, 1f);

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

        Text CreateGlyph(Transform parent, string content, Vector2 position)
        {
            RectTransform rect = UIBuilder.CreateRect(parent, new Vector2(0, 0), new Vector2(0, 0), position, new Vector2(16f, 28f));
            Text glyph = rect.gameObject.AddComponent<Text>();
            glyph.text = content;
            glyph.alignment = TextAnchor.MiddleCenter;
            glyph.color = Color.white;
            glyph.font = UIBuilder.GetFont(Theme);
            glyph.fontSize = 22;
            glyph.raycastTarget = false;
            return glyph;
        }

        Text CreatePlainText(Transform parent, string content, Vector2 position, float width)
        {
            RectTransform rect = UIBuilder.CreateRect(parent, new Vector2(0, 0), new Vector2(0, 0), position, new Vector2(width, 16f));
            Text label = rect.gameObject.AddComponent<Text>();
            label.text = content;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = new Color(1f, 1f, 1f, 0.6f);
            label.font = UIBuilder.GetFont(Theme);
            label.fontSize = 12;
            label.raycastTarget = false;
            return label;
        }

        Sprite GetSpeakerIcon()
        {
            if (speakerIcon != null) return speakerIcon;
            if (speakerSprite == null)
            {
                speakerSprite = UIBuilder.LoadIcon(SpeakerResourcePath);
                if (speakerSprite == null)
                    Debug.LogWarning($"[PlayerUI] No sprite found at Resources/{SpeakerResourcePath}. " +
                                     "The volume control shows without its icon. " +
                                     "Set the texture's Texture Type to 'Sprite (2D and UI)'.");
            }
            return speakerSprite;
        }

        Sprite GetSpeakerOffIcon()
        {
            if (speakerOffSprite == null)
                speakerOffSprite = UIBuilder.LoadIcon(SpeakerOffResourcePath);
            return speakerOffSprite;
        }

        void BuildUI()
        {
            Canvas canvas = UIBuilder.CreateOverlayCanvas("PlayerUICanvas");
            hudRoot = canvas.gameObject;
            Transform root = canvas.transform;

            // Stacked upward from just above the hotbar.
            float barsY = 1080f * 0.04f + 32f + 16f;
            staminaLabel = UIBuilder.CreateLabel(root, "STAMINA  --- / ---",
                new Vector2(0.5f, 0f), new Vector2(0f, barsY),
                BarWidth, TextAnchor.MiddleCenter, Theme);
            staminaFill = UIBuilder.CreateBar(root, new Vector2(0.5f, 0f), new Vector2(0f, barsY + RowStep),
                BarWidth, StaminaBarHeight, StaminaColor, Theme.panelBackground);

            float healthY = barsY + RowStep + StaminaBarHeight + 6f;
            healthLabel = UIBuilder.CreateLabel(root, "PLAYER  --- / ---",
                new Vector2(0.5f, 0f), new Vector2(0f, healthY),
                BarWidth, TextAnchor.MiddleCenter, Theme);
            healthFill = UIBuilder.CreateBar(root, new Vector2(0.5f, 0f), new Vector2(0f, healthY + RowStep),
                BarWidth, BarHeight, Color.white, Theme.panelBackground);

            Vector2 topRight = new Vector2(1, 1);
            goldLabel = UIBuilder.CreateLabel(root, "GOLD  --",
                topRight, new Vector2(-Margin, -Margin),
                CounterWidth, TextAnchor.MiddleRight, Theme);
            waveLabel = UIBuilder.CreateLabel(root, "WAVE  --",
                topRight, new Vector2(-Margin, -Margin - RowStep),
                CounterWidth, TextAnchor.MiddleRight, Theme);
            enemyLabel = UIBuilder.CreateLabel(root, "ENEMIES REMAINING  --",
                topRight, new Vector2(-Margin, -Margin - RowStep * 2f),
                CounterWidth, TextAnchor.MiddleRight, Theme);

            BuildVolumeControl(root);

            HotbarUI hotbar = GetComponent<HotbarUI>();
            if (hotbar == null) hotbar = gameObject.AddComponent<HotbarUI>();
            hotbar.BuildInto(root);

            BuildDeathScreen(root);
        }

        void BuildDeathScreen(Transform parent)
        {
            GameObject fadeGo = new GameObject("DeathFade");
            fadeGo.transform.SetParent(parent, false);
            RectTransform fadeRect = fadeGo.AddComponent<RectTransform>();
            UIBuilder.Stretch(fadeRect);
            fadeOverlay = fadeGo.AddComponent<Image>();
            fadeOverlay.color = new Color(0f, 0f, 0f, 0f);
            fadeOverlay.raycastTarget = false;

            // Artwork if there is any, the text message otherwise. The image is
            // centred and sized to its own aspect; the text fills the screen.
            if (endScreen == null) endScreen = LoadEndScreen();

            if (endScreen != null)
            {
                GameObject imageGo = new GameObject("EndScreen");
                imageGo.transform.SetParent(fadeGo.transform, false);
                RectTransform imageRect = imageGo.AddComponent<RectTransform>();
                imageRect.anchorMin = new Vector2(0.5f, 0.5f);
                imageRect.anchorMax = new Vector2(0.5f, 0.5f);
                imageRect.pivot = new Vector2(0.5f, 0.5f);

                float aspect = endScreen.rect.height > 0f
                    ? endScreen.rect.width / endScreen.rect.height : 1f;
                imageRect.sizeDelta = new Vector2(endScreenHeight * aspect, endScreenHeight);
                imageRect.anchoredPosition = Vector2.zero;

                Image image = imageGo.AddComponent<Image>();
                image.sprite = endScreen;
                image.preserveAspect = true;
                image.color = new Color(1f, 1f, 1f, 0f);
                image.raycastTarget = false;
                deathVisual = image;
            }
            else
            {
                // Parented to the black sheet so it always draws above it.
                GameObject textGo = new GameObject("DeathMessage");
                textGo.transform.SetParent(fadeGo.transform, false);
                RectTransform textRect = textGo.AddComponent<RectTransform>();
                UIBuilder.Stretch(textRect);
                deathText = textGo.AddComponent<Text>();
                deathText.text = deathMessage;
                deathText.alignment = TextAnchor.MiddleCenter;
                deathText.font = UIBuilder.GetFont(Theme);
                deathText.fontSize = messageFontSize;
                // Red, starting fully transparent.
                deathText.color = new Color(1f, 0f, 0f, 0f);
                deathText.raycastTarget = false;
                deathVisual = deathText;
            }

            // Dropped clear of whatever sits above it.
            float promptOffsetY = deathVisual is Image
                ? -(endScreenHeight * 0.5f) - RestartGap
                : -TextPromptOffset;

            GameObject restartGo = new GameObject("RestartPrompt");
            restartGo.transform.SetParent(fadeGo.transform, false);
            RectTransform restartRect = restartGo.AddComponent<RectTransform>();
            restartRect.anchorMin = Vector2.zero;
            restartRect.anchorMax = Vector2.one;
            restartRect.offsetMin = new Vector2(0f, promptOffsetY);
            restartRect.offsetMax = new Vector2(0f, promptOffsetY);
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