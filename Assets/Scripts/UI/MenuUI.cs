using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using Game.Core;

namespace Game.UI
{
    /// <summary>
    /// Start screen. Shows a title and a Play button while the game loop sits in and moves it to WaveActive when the
    /// player commits.    
    /// The camera side of the effect lives, it holds a framed anchor pose
    /// while in Menu, then flies down to the player once the state changes
    /// </summary>
    public class MenuUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Leave empty to find the GameStateMachine in the scene.")]
        [SerializeField] private GameStateMachine stateMachine;

        [Header("Content")]
        [SerializeField] private string titleText = "THE FALL OF CAMELOT";
        [SerializeField] private string playText  = "PLAY";
        [SerializeField] private string hintText  = "or press SPACE";

        [Header("Logo")]
        [Tooltip("Shown in place of the title text. Leave empty to load Resources/UI/start_screen automatically.")]
        [SerializeField] private Sprite logo;
        [Tooltip("Logo height in reference pixels (the canvas reference is 1920x1080). Width follows the image's own aspect ratio.")]
        [SerializeField] private float logoHeight = 280f;

        // Loaded by name
        private const string LogoResourcePath = "UI/start_screen";

        // Layout of the centred block: header, gap, button, gap
        private const float TitleHeight   = 160f;   // header height when falling back to text
        private const float ButtonWidth   = 280f;
        private const float ButtonHeight  = 64f;
        private const float HintHeight    = 40f;
        private const float HeaderGap     = 48f;    // header -> button
        private const float ButtonGap     = 16f;    // button -> hint

        [Header("Style")]
        [Tooltip("Only used if no logo sprite can be found.")]
        [SerializeField] private int titleFontSize = 96;
        [Tooltip("Darkens the scenic shot so the title stays readable. 0 = no dimming.")]
        [SerializeField, Range(0f, 1f)] private float backdropDim = 0.35f;

        private GameObject panel;
        private static Font s_font;

        void Awake()
        {
            if (stateMachine == null) stateMachine = FindFirstObjectByType<GameStateMachine>();
            BuildUI();
        }

        void OnEnable()
        {
            // Here rather than in Start
            EventManager.OnGameStateChanged += HandleGameStateChanged;
        }

        void OnDisable()
        {
            EventManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        void Update()
        {
            if (!IsShowing()) return;

            // Keyboard fallback so the menu still works if the pointer setup ever breaks
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)
                Play();
        }

        void HandleGameStateChanged(GameStateChangedArgs e)
        {
            if (panel != null) panel.SetActive(e.Current == GameStateId.Menu);
        }

        bool IsShowing() => panel != null && panel.activeSelf;

        /// <summary>Leave the menu and start the first wave. Wired to the Play
        /// button and callable from anywhere else that wants to start a run.</summary>
        public void Play()
        {
            if (stateMachine == null) return;
            if (stateMachine.CurrentState == null) return;
            if (stateMachine.CurrentState.Id != GameStateId.Menu) return;

            stateMachine.MoveToState(GameStateId.WaveActive);
        }

        void BuildUI()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("MenuUICanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above PlayerUI's canvas, which sits at the default 0
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            panel = new GameObject("MenuPanel");
            panel.transform.SetParent(canvasGo.transform, false);
            var panelRt = panel.AddComponent<RectTransform>();
            Stretch(panelRt);
            var backdrop = panel.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, backdropDim);

            backdrop.raycastTarget = true;

            // Resolve the logo before laying out: the block's height, and so
            // where everything sits, depends on whether there is on
            if (logo == null) logo = LoadLogo();
            float headerHeight = logo != null ? logoHeight : TitleHeight;

            float blockHeight = headerHeight + HeaderGap + ButtonHeight + ButtonGap + HintHeight;
            float top = blockHeight * 0.5f;

            // Header hangs from the top of the block
            float buttonY = top - headerHeight - HeaderGap - ButtonHeight * 0.5f;
            float hintY   = buttonY - ButtonHeight * 0.5f - ButtonGap - HintHeight * 0.5f;

            CreateTitle(panel.transform, top);
            CreatePlayButton(panel.transform, buttonY);
            CreateHint(panel.transform, hintY);

            // Hidden until the state bus confirms we are actually in Menu
            panel.SetActive(false);
        }

        void CreateTitle(Transform parent, float topY)
        {
            if (logo != null)
            {
                CreateLogo(parent, topY);
                return;
            }

            // Text fallback, so a failed load leaves something readable instead of a blank screen
            Debug.LogWarning($"[MenuUI] No sprite found at Resources/{LogoResourcePath}. " +
                             "Set the texture's Texture Type to 'Sprite (2D and UI)'. Using text title.");
            CreateTitleText(parent, topY);
        }

        /// <summary>
        /// Loads the logo whichever way the texture is imported
        /// </summary>
        static Sprite LoadLogo()
        {
            var single = Resources.Load<Sprite>(LogoResourcePath);
            if (single != null) return single;

            var sliced = Resources.LoadAll<Sprite>(LogoResourcePath);
            return sliced != null && sliced.Length > 0 ? sliced[0] : null;
        }

        void CreateLogo(Transform parent, float topY)
        {
            var go = new GameObject("Logo");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 1f);

            float aspect = logo.rect.height > 0f ? logo.rect.width / logo.rect.height : 1f;
            rt.sizeDelta = new Vector2(logoHeight * aspect, logoHeight);
            rt.anchoredPosition = new Vector2(0f, topY);

            var img = go.AddComponent<Image>();
            img.sprite = logo;
            img.preserveAspect = true;
            img.raycastTarget = false;
        }

        void CreateTitleText(Transform parent, float topY)
        {
            var go = new GameObject("Title");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(1200f, TitleHeight);
            rt.anchoredPosition = new Vector2(0f, topY);

            var txt = go.AddComponent<Text>();
            txt.text = titleText;
            txt.font = GetFont();
            txt.fontSize = titleFontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
        }

        void CreatePlayButton(Transform parent, float centreY)
        {
            var go = new GameObject("PlayButton");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(ButtonWidth, ButtonHeight);
            rt.anchoredPosition = new Vector2(0f, centreY);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.12f, 0.12f, 0.14f, 0.95f);

            var button = go.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1f, 0.85f, 0.4f, 1f);
            colors.pressedColor     = new Color(0.8f, 0.65f, 0.25f, 1f);
            button.colors = colors;
            button.onClick.AddListener(Play);

            var labelGo = new GameObject("Text");
            labelGo.transform.SetParent(go.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            Stretch(labelRt);
            var label = labelGo.AddComponent<Text>();
            label.text = playText;
            label.font = GetFont();
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        void CreateHint(Transform parent, float centreY)
        {
            var go = new GameObject("Hint");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600f, HintHeight);
            rt.anchoredPosition = new Vector2(0f, centreY);

            var txt = go.AddComponent<Text>();
            txt.text = hintText;
            txt.font = GetFont();
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1f, 1f, 1f, 0.6f);
            txt.raycastTarget = false;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static Font GetFont()
        {
            if (s_font != null) return s_font;
            s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return s_font;
        }
    }
}
