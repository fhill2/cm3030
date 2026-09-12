using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Game.Core;

namespace Game.UI
{
    public class MenuUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Leave empty to find the GameStateMachine in the scene.")]
        [SerializeField] private GameStateMachine stateMachine;

        [Header("Theme")]
        [Tooltip("Leave empty for the built-in default theme.")]
        [SerializeField] private UITheme theme;

        [Header("Content")]
        [SerializeField] private string titleText = "THE FALL OF CAMELOT";
        [SerializeField] private string playText  = "PLAY";
        [SerializeField] private string hintText  = "or press SPACE";
        [Tooltip("One-shot clip under Resources/ played when the Play button is pressed.")]
        [SerializeField] private string playClip = "Ambient/KSHMR_sok5_drum_orchestral_low_degree";

        [Header("Logo")]
        [Tooltip("Shown in place of the title text. Leave empty to load Resources/UI/start_screen automatically.")]
        [SerializeField] private Sprite logo;
        [Tooltip("Logo height in reference pixels (the canvas reference is 1920x1080). Width follows the image's own aspect ratio.")]
        [SerializeField] private float logoHeight = 280f;

        private const string LogoResourcePath = "UI/start_screen";

        private const float TitleHeight   = 160f;
        private const float ButtonPadding = 48f;
        private const float HintHeight    = 40f;
        private const float HeaderGap     = 48f;
        private const float ButtonGap     = 16f;

        [Header("Style")]
        [Tooltip("Only used if no logo sprite can be found.")]
        [SerializeField] private int titleFontSize = 96;
        [Tooltip("Darkens the scenic shot so the title stays readable. 0 = no dimming.")]
        [SerializeField, Range(0f, 1f)] private float backdropDim = 0.35f;

        private GameObject panel;

        private UITheme Theme => theme != null ? theme : UITheme.Default;

        void Awake()
        {
            if (stateMachine == null) stateMachine = FindFirstObjectByType<GameStateMachine>();
            BuildUI();
        }

        void OnEnable()
        {
            EventManager.OnGameStateChanged += HandleGameStateChanged;
        }

        void OnDisable()
        {
            EventManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        void Update()
        {
            if (!IsShowing()) return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;
            if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)
                Play();
        }

        void HandleGameStateChanged(GameStateChangedArgs e)
        {
            if (panel != null) panel.SetActive(e.Current == GameStateId.Menu);
        }

        bool IsShowing() => panel != null && panel.activeSelf;

        public void Play()
        {
            if (stateMachine == null) return;
            if (stateMachine.CurrentState == null) return;
            if (stateMachine.CurrentState.Id != GameStateId.Menu) return;

            if (!string.IsNullOrEmpty(playClip))
            {
                AudioClip clip = Resources.Load<AudioClip>(playClip);
                if (clip != null)
                {
                    AudioSource source = gameObject.AddComponent<AudioSource>();
                    source.clip = clip;
                    source.loop = false;
                    source.volume = 0.9f;
                    source.Play();
                }
                else
                {
                    Debug.LogWarning($"[MenuUI] No clip at Resources/{playClip}");
                }
            }

            stateMachine.MoveToState(GameStateId.WaveActive);
        }

        void BuildUI()
        {
            UIBuilder.EnsureEventSystem();

            Canvas canvas = UIBuilder.CreateOverlayCanvas("MenuUICanvas", sortingOrder: 100);

            panel = new GameObject("MenuPanel");
            panel.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panel.AddComponent<RectTransform>();
            UIBuilder.Stretch(panelRect);

            Image backdrop = panel.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, backdropDim);
            backdrop.raycastTarget = true;

            if (logo == null) logo = LoadLogo();
            float headerHeight = logo != null ? logoHeight : TitleHeight;
            float playHeight = UIBuilder.MeasureButtonWidth(
                new[] { playText }, ButtonPadding, 30, Theme) / UIBuilder.ButtonAspect;

            // Whole block is measured first, then centred, so the title, button
            // and hint stay together whichever header is used.
            float blockHeight = headerHeight + HeaderGap + playHeight + ButtonGap + HintHeight;
            float top = blockHeight * 0.5f;

            float buttonY = top - headerHeight - HeaderGap - playHeight * 0.5f;
            float hintY = buttonY - playHeight * 0.5f - ButtonGap - HintHeight * 0.5f;

            CreateTitle(panel.transform, top);
            CreatePlayButton(panel.transform, buttonY);
            CreateHint(panel.transform, hintY);

            panel.SetActive(false);
        }

        void CreateTitle(Transform parent, float topY)
        {
            if (logo != null)
            {
                CreateLogo(parent, topY);
                return;
            }

            Debug.LogWarning($"[MenuUI] No sprite found at Resources/{LogoResourcePath}. " +
                              "Set the texture's Texture Type to 'Sprite (2D and UI)'. Using text title.");
            CreateTitleText(parent, topY);
        }

        static Sprite LoadLogo()
        {
            return UIBuilder.LoadIcon(LogoResourcePath);
        }

        void CreateLogo(Transform parent, float topY)
        {
            GameObject logoGo = new GameObject("Logo");
            logoGo.transform.SetParent(parent, false);
            RectTransform rect = logoGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);

            float aspect = logo.rect.height > 0f ? logo.rect.width / logo.rect.height : 1f;
            rect.sizeDelta = new Vector2(logoHeight * aspect, logoHeight);
            rect.anchoredPosition = new Vector2(0f, topY);

            Image image = logoGo.AddComponent<Image>();
            image.sprite = logo;
            image.preserveAspect = true;
            image.raycastTarget = false;
        }

        void CreateTitleText(Transform parent, float topY)
        {
            GameObject titleGo = new GameObject("Title");
            titleGo.transform.SetParent(parent, false);
            RectTransform rect = titleGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(1200f, TitleHeight);
            rect.anchoredPosition = new Vector2(0f, topY);

            Text title = titleGo.AddComponent<Text>();
            title.text = titleText;
            title.font = UIBuilder.GetFont(Theme);
            title.fontSize = titleFontSize;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.raycastTarget = false;
        }

        void CreatePlayButton(Transform parent, float centreY)
        {
            float width = UIBuilder.MeasureButtonWidth(
                new[] { playText }, ButtonPadding, 30, Theme);
            UIBuilder.CreateButton(parent, "PlayButton", playText,
                new Vector2(0f, centreY), width, Theme, 30, Play);
        }

        void CreateHint(Transform parent, float centreY)
        {
            GameObject hintGo = new GameObject("Hint");
            hintGo.transform.SetParent(parent, false);
            RectTransform rect = hintGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(600f, HintHeight);
            rect.anchoredPosition = new Vector2(0f, centreY);

            Text hint = hintGo.AddComponent<Text>();
            hint.text = hintText;
            hint.font = UIBuilder.GetFont(Theme);
            hint.fontSize = 20;
            hint.alignment = TextAnchor.MiddleCenter;
            hint.color = new Color(1f, 1f, 1f, 0.6f);
            hint.raycastTarget = false;
        }
    }
}