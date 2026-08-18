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
    /// player commits. Goes on the GameManager, alongside GameStateMachine.
    ///
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

        [Header("Style")]
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
            // Here rather than in Start: GameStateMachine raises its
            // opening state change from its own Start and a Start-time
            // subscription can miss it entirely.
            EventManager.OnGameStateChanged += HandleGameStateChanged;
        }

        void OnDisable()
        {
            EventManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        void Update()
        {
            if (!IsShowing()) return;

            // Keyboard fallback so the menu still works if the pointer setup
            // ever breaks — a dead Play button would otherwise be unstartable.
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
            // Above PlayerUI's canvas, which sits at the default 0.
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
            // The scenic shot is the point of this screen, so the backdrop only
            // dims it. It still needs to swallow clicks aimed past the button.
            backdrop.raycastTarget = true;

            CreateTitle(panel.transform);
            CreatePlayButton(panel.transform);
            CreateHint(panel.transform);

            // Hidden until the state bus confirms we are actually in Menu, so
            // scenes that boot straight into a wave never flash the title.
            panel.SetActive(false);
        }

        void CreateTitle(Transform parent)
        {
            var go = new GameObject("Title");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1200f, 160f);
            rt.anchoredPosition = new Vector2(0f, 140f);

            var txt = go.AddComponent<Text>();
            txt.text = titleText;
            txt.font = GetFont();
            txt.fontSize = titleFontSize;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.red;
            txt.raycastTarget = false;
        }

        void CreatePlayButton(Transform parent)
        {
            var go = new GameObject("PlayButton");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(280f, 64f);
            rt.anchoredPosition = new Vector2(0f, -20f);

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
            // Must not intercept the click on its way to the Button above it.
            label.raycastTarget = false;
        }

        void CreateHint(Transform parent)
        {
            var go = new GameObject("Hint");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600f, 40f);
            rt.anchoredPosition = new Vector2(0f, -80f);

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
