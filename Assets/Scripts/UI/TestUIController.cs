using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Game.Core;
using Game.Health;
using Game.Shared;

namespace Game.UI
{
    /// <summary>
    /// Test harness for the EventManagerScript communication system.
    /// Creates UI buttons at runtime that trigger events, and displays
    /// that react to them — demonstrating the full pub/sub flow.
    ///
    /// Usage: add to any empty GameObject in the scene.
    /// It auto-finds or creates a test target with PlayerHealth.
    /// </summary>
    public class TestUIController : MonoBehaviour
    {
        private const float ButtonWidth = 180f;
        private const float ButtonHeight = 44f;
        private const float Spacing = 10f;
        private const float TestDamage = 25f;

        private HealthSystem target;
        private Text healthLabel;
        private Text statusLabel;

        private static Font s_CachedFont;

        // ── Lifecycle ───────────────────────────────────────────

        void Awake()
        {
            EnsureEventSystem();
            ResolveTarget();
            BuildUI();
            RefreshHealthLabel();
        }

        void OnEnable()
        {
            EventManagerScript.OnDamageDealt += OnDamageDealt;
            EventManagerScript.OnEntityDied += OnEntityDied;
        }

        void OnDisable()
        {
            EventManagerScript.OnDamageDealt -= OnDamageDealt;
            EventManagerScript.OnEntityDied -= OnEntityDied;
        }

        // ── Target resolution ───────────────────────────────────

        private void ResolveTarget()
        {
            target = FindFirstObjectByType<HealthSystem>();
            if (target != null) return;

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.AddComponent<PlayerHealth>();
                Debug.Log("[TestUI] Added PlayerHealth to existing player.");
                return;
            }

            var dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            dummy.name = "Test Dummy";
            dummy.transform.position = new Vector3(0, 1, 3);
            target = dummy.AddComponent<PlayerHealth>();
            Debug.Log("[TestUI] No player found — created test dummy with PlayerHealth.");
        }

        // ── Event handlers ──────────────────────────────────────

        private void OnDamageDealt(DamageDealt e)
        {
            if (e.Target == target.gameObject)
                RefreshHealthLabel();
        }

        private void OnEntityDied(EntityDied e)
        {
            if (e.Entity == target.gameObject)
            {
                statusLabel.text = "ENTITY DIED";
                statusLabel.color = new Color(1f, 0.4f, 0.4f);
                RefreshHealthLabel();
            }
        }

        private void RefreshHealthLabel()
        {
            if (target != null && healthLabel != null)
                healthLabel.text = $"HP: {target.CurrentHealth:0} / {target.MaxHealth:0}";
        }

        // ── UI construction ─────────────────────────────────────

        private void BuildUI()
        {
            // Canvas
            var canvasGo = new GameObject("TestUICanvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            // Buttons — centered row at top
            float totalWidth = ButtonWidth * 4 + Spacing * 3;
            float startX = -totalWidth * 0.5f + ButtonWidth * 0.5f;
            float topOffset = -20f;

            CreateButton(canvasGo.transform, "Take 25 Damage",
                new Vector2(startX + (ButtonWidth + Spacing) * 0, topOffset),
                () => target?.TakeDamage(TestDamage, DamageType.Heavy, gameObject));

            CreateButton(canvasGo.transform, "Kill Player",
                new Vector2(startX + (ButtonWidth + Spacing) * 1, topOffset),
                () => target?.Kill());

            CreateButton(canvasGo.transform, "Test Footstep",
                new Vector2(startX + (ButtonWidth + Spacing) * 2, topOffset),
                EventManagerScript.RaiseFootstep);

            CreateButton(canvasGo.transform, "Test Jump",
                new Vector2(startX + (ButtonWidth + Spacing) * 3, topOffset),
                EventManagerScript.RaiseJump);

            // Health display
            healthLabel = CreateText(canvasGo.transform, "HP: 100 / 100",
                new Vector2(0, topOffset - ButtonHeight - Spacing),
                24, new Color(0.7f, 1f, 0.7f));

            // Status display
            statusLabel = CreateText(canvasGo.transform, "",
                new Vector2(0, topOffset - ButtonHeight - Spacing - 36f),
                20, new Color(1f, 0.5f, 0.5f));
        }

        // ── UI helpers ──────────────────────────────────────────

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        private void CreateButton(Transform parent, string label, Vector2 anchoredPos, UnityAction onClick)
        {
            var rt = CreateAnchoredRect(parent, anchoredPos, new Vector2(ButtonWidth, ButtonHeight));
            rt.gameObject.name = $"Btn_{label}";

            var img = rt.gameObject.AddComponent<Image>();
            img.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

            var btn = rt.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.3f, 0.3f, 0.3f);
            colors.pressedColor = new Color(0.1f, 0.5f, 0.1f);
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var labelRt = CreateAnchoredRect(rt, Vector2.zero, new Vector2(ButtonWidth, ButtonHeight));
            var txt = labelRt.gameObject.AddComponent<Text>();
            txt.text = label;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.font = GetFont();
            txt.fontSize = 16;
            txt.raycastTarget = false;
        }

        private Text CreateText(Transform parent, string content, Vector2 anchoredPos, int fontSize, Color color)
        {
            var rt = CreateAnchoredRect(parent, anchoredPos, new Vector2(400, fontSize + 8));
            var txt = rt.gameObject.AddComponent<Text>();
            txt.text = content;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = color;
            txt.font = GetFont();
            txt.fontSize = fontSize;
            txt.raycastTarget = false;
            return txt;
        }

        private static RectTransform CreateAnchoredRect(Transform parent, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject();
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            return rt;
        }

        private static Font GetFont()
        {
            if (s_CachedFont != null) return s_CachedFont;
            s_CachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return s_CachedFont;
        }
    }
}
