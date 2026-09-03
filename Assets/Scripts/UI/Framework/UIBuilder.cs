using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Game.UI
{
    public static class UIBuilder
    {
        private static Font s_font;
        private static readonly Dictionary<string, Sprite> s_artCache = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> s_missingArt = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_font = null;
            s_artCache.Clear();
            s_missingArt.Clear();
        }

        public static Canvas CreateOverlayCanvas(string name, int sortingOrder = 0)
        {
            var canvasGo = new GameObject(name);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform CreateRect(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size)
        {
            var go = new GameObject();
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = anchorMin;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform CreateStretchChild(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            Stretch(rt);
            return rt;
        }

        public static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static Image AttachImage(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text AttachText(RectTransform rt, string content, TextAnchor alignment,
            int fontSize, Color color, Font font)
        {
            var txt = rt.gameObject.AddComponent<Text>();
            txt.text = content;
            txt.alignment = alignment;
            txt.color = color;
            txt.font = font;
            txt.fontSize = fontSize;
            txt.verticalOverflow = VerticalWrapMode.Overflow;
            txt.raycastTarget = false;
            return txt;
        }

        public static Text CreateText(Transform parent, string name, string content,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size,
            TextAnchor alignment, int fontSize, Color color, UITheme theme)
        {
            var rt = CreateRect(parent, anchorMin, anchorMax, anchoredPosition, size);
            rt.gameObject.name = name;
            return AttachText(rt, content, alignment, fontSize, color, GetFont(theme));
        }

        public static Image CreateBarFill(RectTransform background, Color fillColor)
        {
            var fillGo = new GameObject("Bar_Fill");
            fillGo.transform.SetParent(background, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = new Vector2(2, 2);
            fillRt.offsetMax = new Vector2(-2, -2);
            var fillImg = fillGo.AddComponent<Image>();
            fillImg.color = fillColor;
            return fillImg;
        }

        public static Image CreateBar(Transform parent, Vector2 anchor, Vector2 anchoredPosition,
            float width, float height, Color fillColor, Color backgroundColor)
        {
            var bgRt = CreateRect(parent, anchor, anchor, anchoredPosition, new Vector2(width, height));
            AttachImage(bgRt, backgroundColor);
            return CreateBarFill(bgRt, fillColor);
        }

        public static Text CreateLabel(Transform parent, string content, Vector2 anchor,
            Vector2 anchoredPosition, float width, TextAnchor alignment, UITheme theme,
            float height = 28f, int fontSize = 18)
        {
            var bgRt = CreateRect(parent, anchor, anchor, anchoredPosition, new Vector2(width, height));
            AttachImage(bgRt, theme.panelBackground);

            var textRt = CreateStretchChild(bgRt, "Text");
            textRt.offsetMin = new Vector2(8, 2);
            textRt.offsetMax = new Vector2(-8, -2);
            return AttachText(textRt, content, alignment, fontSize, Color.white, GetFont(theme));
        }

        public const float ButtonAspect = 1.55f;
        public const string ButtonResourceFolder = "UI/Buttons";
        public static readonly Color ButtonHoverText = new Color(0.761f, 0.663f, 0.439f);
        public const float ButtonHoverFontScale = 1.2f;

        public static Button CreateButton(Transform parent, string name, string label,
            Vector2 center, float width, UITheme theme, int fontSize,
            System.Action onClick = null)
        {
            var rt = CreateRect(parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                center, new Vector2(width, width / ButtonAspect));
            rt.gameObject.name = name;
            var img = AttachImage(rt, Color.clear);

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = theme.accent;
            colors.pressedColor = theme.buttonPressed;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var underlineGo = new GameObject("Underline");
            underlineGo.transform.SetParent(rt, false);
            var underlineRt = underlineGo.AddComponent<RectTransform>();
            underlineRt.anchorMin = new Vector2(0.5f, 0f);
            underlineRt.anchorMax = new Vector2(0.5f, 0f);
            underlineRt.pivot = new Vector2(0.5f, 0f);
            underlineRt.anchoredPosition = Vector2.zero;
            underlineRt.sizeDelta = new Vector2(0f, 4f);
            var underlineImg = AttachImage(underlineRt, theme.accent);
            underlineImg.raycastTarget = false;
            var underline = underlineGo.AddComponent<ButtonUnderline>();

            var labelRt = CreateStretchChild(rt, "Text");
            var labelText = AttachText(labelRt, label, TextAnchor.MiddleCenter, fontSize, theme.text, GetFont(theme));
            HookButtonHover(rt.gameObject, button, labelText, fontSize, theme, underline, width);
            return button;
        }

        static void HookButtonHover(GameObject go, Button button, Text label, int fontSize, UITheme theme,
            ButtonUnderline underline, float width)
        {
            var trigger = go.AddComponent<EventTrigger>();

            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                if (!button.IsInteractable()) return;
                label.fontSize = Mathf.Max(fontSize + 2, Mathf.RoundToInt(fontSize * ButtonHoverFontScale));
                label.color = ButtonHoverText;
                if (underline != null) underline.Show(width);
            });
            trigger.triggers.Add(enter);

            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                label.fontSize = fontSize;
                label.color = theme.text;
                if (underline != null) underline.Hide();
            });
            trigger.triggers.Add(exit);
        }

        public static float MeasureButtonWidth(string[] labels, float padding, int fontSize, UITheme theme)
        {
            var go = new GameObject();
            var txt = AttachText(go.AddComponent<RectTransform>(), "",
                TextAnchor.MiddleCenter, fontSize, Color.white, GetFont(theme));

            float widest = 0f;
            foreach (string label in labels)
            {
                if (string.IsNullOrEmpty(label)) continue;
                txt.text = label;
                widest = Mathf.Max(widest, txt.preferredWidth);
            }

            Object.Destroy(go);
            return widest + padding * 2f;
        }

        public static void SetButtonSize(Button button, float width)
        {
            if (button == null) return;
            ((RectTransform)button.transform).sizeDelta = new Vector2(width, width / ButtonAspect);
        }

        public static Image CreateIcon(Transform parent, string name, Sprite sprite, Vector2 anchor,
            Vector2 anchoredPosition, float size)
        {
            var rt = CreateRect(parent, anchor, anchor, anchoredPosition, new Vector2(size, size));
            rt.gameObject.name = name;
            var img = AttachImage(rt, Color.white);
            img.sprite = sprite;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Vector2 anchor,
            Vector2 anchoredPosition, Vector2 size, UITheme theme,
            BorderStyle border = null, Color? background = null)
        {
            var rt = CreateRect(parent, anchor, anchor, anchoredPosition, size);
            rt.gameObject.name = name;
            var bg = AttachImage(rt, background ?? theme.panelBackground);
            bg.raycastTarget = true;
            ApplyBorder(rt, border);
            return rt;
        }

        public static RectTransform CreateContentRect(RectTransform panel, float inset)
        {
            var content = CreateStretchChild(panel, "Content");
            content.offsetMin = Vector2.one * inset;
            content.offsetMax = Vector2.one * -inset;
            return content;
        }

        public static ScrollRect CreateScrollList(Transform parent, string name, Vector2 anchor,
            Vector2 anchoredPosition, Vector2 size, UITheme theme, out RectTransform content)
        {
            var rootRt = CreateRect(parent, anchor, anchor, anchoredPosition, size);
            rootRt.gameObject.name = name;

            var viewportRt = CreateStretchChild(rootRt, "Viewport");
            viewportRt.gameObject.AddComponent<RectMask2D>();

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportRt, false);
            content = contentGo.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            var scroll = rootRt.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportRt;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return scroll;
        }

        public const string BorderResourceFolder = "UI/Borders";

        public static RectTransform ApplyBorder(RectTransform target, BorderStyle style)
        {
            if (target == null || style == null) return null;

            Sprite borderSprite = ResolveBorderSprite(style);
            if (borderSprite == null) return null;

            var containerRt = CreateStretchChild(target, "Border");
            var img = containerRt.gameObject.AddComponent<Image>();
            img.sprite = borderSprite;
            img.color = style.tint;
            img.raycastTarget = false;
            return containerRt;
        }

        public static Sprite ResolveBorderSprite(BorderStyle style)
        {
            if (style.sprite != null) return style.sprite;

            if (string.IsNullOrEmpty(style.spriteName)) return null;

            string path = BorderResourceFolder + "/" + style.spriteName;
            if (s_artCache.TryGetValue(path, out Sprite cached)) return cached;

            var sprite = LoadTextureSprite(path);
            if (sprite == null)
            {
                if (s_missingArt.Add(path))
                    Debug.LogWarning($"[UIBuilder] No border art found at Resources/{path}.");
                return null;
            }

            s_artCache[path] = sprite;
            return sprite;
        }

        public const string GameFontPath = "UI/MedievalSharp-Regular";

        public static Font GetFont(UITheme theme)
        {
            if (theme != null && theme.font != null) return theme.font;
            if (s_font == null)
                s_font = Resources.Load<Font>(GameFontPath)
                    ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return s_font;
        }

        public static Sprite LoadIcon(string resourcePath)
        {
            var single = Resources.Load<Sprite>(resourcePath);
            if (single != null) return single;

            var sliced = Resources.LoadAll<Sprite>(resourcePath);
            return sliced != null && sliced.Length > 0 ? sliced[0] : null;
        }

        public static Sprite LoadTextureSprite(string resourcePath)
        {
            var tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) return null;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
