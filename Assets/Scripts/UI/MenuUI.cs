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

        [Header("Logo")]
        [Tooltip("Shown in place of the title text. Leave empty to load Resources/UI/start_screen automatically.")]
        [SerializeField] private Sprite logo;
        [Tooltip("Logo height in reference pixels (the canvas reference is 1920x1080). Width follows the image's own aspect ratio.")]
        [SerializeField] private float logoHeight = 280f;

        private const string LogoResourcePath = "UI/start_screen";

        private const float TitleHeight   = 160f;
        private const float ButtonWidth   = 280f;
        private const float ButtonHeight  = 64f;
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

        public void Play()
        {
            if (stateMachine == null) return;
            if (stateMachine.CurrentState == null) return;
            if (stateMachine.CurrentState.Id != GameStateId.Menu) return;

            stateMachine.MoveToState(GameStateId.WaveActive);
        }

        void BuildUI()
        {
            UIBuilder.EnsureEventSystem();

            var canvas = UIBuilder.CreateOverlayCanvas("MenuUICanvas",
                sortingOrder: 100);

            panel = new GameObject("MenuPanel");
            panel.transform.SetParent(canvas.transform, false);
            var panelRt = panel.AddComponent<RectTransform>();
            UIBuilder.Stretch(panelRt);
            var backdrop = panel.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, backdropDim);

            backdrop.raycastTarget = true;

            if (logo == null) logo = LoadLogo();
            float headerHeight = logo != null ? logoHeight : TitleHeight;

            float blockHeight = headerHeight + HeaderGap + ButtonHeight + ButtonGap + HintHeight;
            float top = blockHeight * 0.5f;

            float buttonY = top - headerHeight - HeaderGap - ButtonHeight * 0.5f;
            float hintY   = buttonY - ButtonHeight * 0.5f - ButtonGap - HintHeight * 0.5f;

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
            txt.font = UIBuilder.GetFont(Theme);
            txt.fontSize = titleFontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
        }

        void CreatePlayButton(Transform parent, float centreY)
        {
            UIBuilder.CreateButton(parent, "PlayButton", playText,
                new Vector2(0.5f, 0.5f), new Vector2(0f, centreY),
                ButtonWidth, ButtonHeight, Theme, 30, Play);
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
            txt.font = UIBuilder.GetFont(Theme);
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1f, 1f, 1f, 0.6f);
            txt.raycastTarget = false;
        }
    }
}
