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
        [Tooltip("Shown once the screen is fully black.")]
        [SerializeField] private string deathMessage = "Camelot has Fallen";
        [SerializeField] private int messageFontSize = 72;

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

        private StaminaSystem stamina;
        private Image staminaFill;
        private Text  staminaLabel;

        private RunController runController;

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

            if (deathRoutine == null) deathRoutine = StartCoroutine(DeathSequence());
        }

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
                staminaFill.color = stamina.IsStunned ? StunnedColor : StaminaColor;
            }

            if (staminaLabel != null)
            {
                staminaLabel.text = stamina.IsStunned
                    ? "STAMINA  EXHAUSTED"
                    : $"STAMINA  {stamina.Current:0} / {stamina.Max:0}";
            }
        }

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

            healthFill = UIBuilder.CreateBar(t, new Vector2(0, 1), new Vector2(Margin, -Margin),
                BarWidth, BarHeight, Color.white, Theme.panelBackground);
            healthLabel = UIBuilder.CreateLabel(t, "PLAYER  --- / ---",
                new Vector2(0, 1), new Vector2(Margin, -Margin - BarHeight - 4f),
                BarWidth, TextAnchor.MiddleLeft, Theme);

            float staminaY = -Margin - BarHeight - 4f - 28f - 6f;
            staminaFill = UIBuilder.CreateBar(t, new Vector2(0, 1), new Vector2(Margin, staminaY),
                BarWidth, StaminaBarHeight, StaminaColor, Theme.panelBackground);
            staminaLabel = UIBuilder.CreateLabel(t, "STAMINA  --- / ---",
                new Vector2(0, 1), new Vector2(Margin, staminaY - StaminaBarHeight - 4f),
                BarWidth, TextAnchor.MiddleLeft, Theme);

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

            var textGo = new GameObject("DeathMessage");
            textGo.transform.SetParent(fadeGo.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            UIBuilder.Stretch(textRt);
            deathText = textGo.AddComponent<Text>();
            deathText.text = deathMessage;
            deathText.alignment = TextAnchor.MiddleCenter;
            deathText.font = UIBuilder.GetFont(Theme);
            deathText.fontSize = messageFontSize;
            deathText.color = new Color(1f, 0f, 0f, 0f);
            deathText.raycastTarget = false;

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
            restartText.font = UIBuilder.GetFont(Theme);
            restartText.fontSize = restartFontSize;
            restartText.color = new Color(1f, 1f, 1f, 0f);
            restartText.raycastTarget = false;
        }
    }
}