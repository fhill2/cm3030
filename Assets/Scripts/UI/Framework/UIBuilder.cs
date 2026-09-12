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
        private static Font font;
        private static readonly Dictionary<string, Sprite> artCache = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> missingArt = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            font = null;
            artCache.Clear();
            missingArt.Clear();
        }

        public static Canvas CreateOverlayCanvas(string name, int sortingOrder = 0)
        {
            GameObject canvasGo = new GameObject(name);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
        }

        public static RectTransform CreateRect(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPosition, Vector2 size)
        {
            GameObject go = new GameObject();
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = anchorMin;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return rect;
        }

        public static RectTransform CreateStretchChild(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            RectTransform rect = go.AddComponent<RectTransform>();
            Stretch(rect);
            return rect;
        }

        public static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        public static Image AttachImage(RectTransform rect, Color color)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        public static Text AttachText(RectTransform rect, string content, TextAnchor alignment,
            int fontSize, Color color, Font font)
        {
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = content;
            text.alignment = alignment;
            text.color = color;
            text.font = font;
            text.fontSize = fontSize;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        public static Text CreateText(Transform parent, string name, string content,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size,
            TextAnchor alignment, int fontSize, Color color, UITheme theme)
        {
            RectTransform rect = CreateRect(parent, anchorMin, anchorMax, anchoredPosition, size);
            rect.gameObject.name = name;
            return AttachText(rect, content, alignment, fontSize, color, GetFont(theme));
        }

        public static Image CreateBarFill(RectTransform background, Color fillColor)
        {
            GameObject fillGo = new GameObject("Bar_Fill");
            fillGo.transform.SetParent(background, false);
            RectTransform fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            Image fillImage = fillGo.AddComponent<Image>();
            fillImage.color = fillColor;
            return fillImage;
        }

        public static Image CreateBar(Transform parent, Vector2 anchor, Vector2 anchoredPosition,
            float width, float height, Color fillColor, Color backgroundColor)
        {
            RectTransform background = CreateRect(parent, anchor, anchor, anchoredPosition, new Vector2(width, height));
            AttachImage(background, backgroundColor);
            return CreateBarFill(background, fillColor);
        }

        public static Text CreateLabel(Transform parent, string content, Vector2 anchor,
            Vector2 anchoredPosition, float width, TextAnchor alignment, UITheme theme,
            float height = 28f, int fontSize = 18)
        {
            RectTransform background = CreateRect(parent, anchor, anchor, anchoredPosition, new Vector2(width, height));
            AttachImage(background, theme.panelBackground);

            RectTransform textRect = CreateStretchChild(background, "Text");
            textRect.offsetMin = new Vector2(8, 2);
            textRect.offsetMax = new Vector2(-8, -2);
            return AttachText(textRect, content, alignment, fontSize, Color.white, GetFont(theme));
        }

        public const float ButtonAspect = 1.55f;
        public const string ButtonResourceFolder = "UI/Buttons";
        public static readonly Color ButtonHoverText = new Color(0.761f, 0.663f, 0.439f);
        public const float ButtonHoverFontScale = 1.2f;

        public static Button CreateButton(Transform parent, string name, string label,
            Vector2 center, float width, UITheme theme, int fontSize,
            System.Action onClick = null)
        {
            RectTransform rect = CreateRect(parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                center, new Vector2(width, width / ButtonAspect));
            rect.gameObject.name = name;
            Image background = AttachImage(rect, Color.clear);

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = theme.accent;
            colors.pressedColor = theme.buttonPressed;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            GameObject underlineGo = new GameObject("Underline");
            underlineGo.transform.SetParent(rect, false);
            RectTransform underlineRect = underlineGo.AddComponent<RectTransform>();
            underlineRect.anchorMin = new Vector2(0.5f, 0f);
            underlineRect.anchorMax = new Vector2(0.5f, 0f);
            underlineRect.pivot = new Vector2(0.5f, 0f);
            underlineRect.anchoredPosition = Vector2.zero;
            underlineRect.sizeDelta = new Vector2(0f, 4f);
            Image underlineImage = AttachImage(underlineRect, theme.accent);
            underlineImage.raycastTarget = false;
            ButtonUnderline underline = underlineGo.AddComponent<ButtonUnderline>();

            RectTransform labelRect = CreateStretchChild(rect, "Text");
            Text labelText = AttachText(labelRect, label, TextAnchor.MiddleCenter, fontSize, theme.text, GetFont(theme));
            HookButtonHover(rect.gameObject, button, labelText, fontSize, theme, underline, width);
            return button;
        }

        static void HookButtonHover(GameObject target, Button button, Text label, int fontSize, UITheme theme,
            ButtonUnderline underline, float width)
        {
            EventTrigger trigger = target.AddComponent<EventTrigger>();

            EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ =>
            {
                if (!button.IsInteractable()) return;
                label.fontSize = Mathf.Max(fontSize + 2, Mathf.RoundToInt(fontSize * ButtonHoverFontScale));
                label.color = ButtonHoverText;
                if (underline != null) underline.Show(width);
            });
            trigger.triggers.Add(enter);

            EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ =>
            {
                if (!button.IsInteractable()) return;
                label.fontSize = fontSize;
                label.color = theme.text;
                if (underline != null) underline.Hide();
            });
            trigger.triggers.Add(exit);
        }

        // Builds a throwaway Text object to measure against, since preferredWidth
        // needs a real component.
        public static float MeasureButtonWidth(string[] labels, float padding, int fontSize, UITheme theme)
        {
            GameObject measureGo = new GameObject();
            Text measureText = AttachText(measureGo.AddComponent<RectTransform>(), "",
                TextAnchor.MiddleCenter, fontSize, Color.white, GetFont(theme));

            float widest = 0f;
            foreach (string label in labels)
            {
                if (string.IsNullOrEmpty(label)) continue;
                measureText.text = label;
                widest = Mathf.Max(widest, measureText.preferredWidth);
            }

            Object.Destroy(measureGo);
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
            RectTransform rect = CreateRect(parent, anchor, anchor, anchoredPosition, new Vector2(size, size));
            rect.gameObject.name = name;
            Image image = AttachImage(rect, Color.white);
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        public static RectTransform CreatePanel(Transform parent, string name, Vector2 anchor,
            Vector2 anchoredPosition, Vector2 size, UITheme theme,
            BorderStyle border = null, Color? background = null)
        {
            RectTransform rect = CreateRect(parent, anchor, anchor, anchoredPosition, size);
            rect.gameObject.name = name;
            Image backgroundImage = AttachImage(rect, background ?? theme.panelBackground);
            backgroundImage.raycastTarget = true;
            ApplyBorder(rect, border);
            return rect;
        }

        public static RectTransform CreateContentRect(RectTransform panel, float inset)
        {
            RectTransform content = CreateStretchChild(panel, "Content");
            content.offsetMin = Vector2.one * inset;
            content.offsetMax = Vector2.one * -inset;
            return content;
        }

        public static ScrollRect CreateScrollList(Transform parent, string name, Vector2 anchor,
            Vector2 anchoredPosition, Vector2 size, UITheme theme, out RectTransform content)
        {
            RectTransform rootRect = CreateRect(parent, anchor, anchor, anchoredPosition, size);
            rootRect.gameObject.name = name;

            RectTransform viewportRect = CreateStretchChild(rootRect, "Viewport");
            viewportRect.gameObject.AddComponent<RectMask2D>();

            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewportRect, false);
            content = contentGo.AddComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;

            ScrollRect scroll = rootRect.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
            return scroll;
        }

        public static Scrollbar CreateScrollbar(Transform parent, ScrollRect scroll, float height, UITheme theme)
        {
            RectTransform trackRect = CreateRect(parent, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-4f, -4f), new Vector2(10f, height - 8f));
            trackRect.gameObject.name = "Scrollbar";
            AttachImage(trackRect, theme.panelBackground);

            GameObject handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(trackRect, false);
            RectTransform handleRect = handleGo.AddComponent<RectTransform>();
            handleRect.anchorMin = Vector2.zero;
            handleRect.anchorMax = Vector2.one;
            handleRect.offsetMin = new Vector2(2f, 2f);
            handleRect.offsetMax = new Vector2(-2f, -2f);
            Image handleImage = AttachImage(handleRect, theme.dimText);

            Scrollbar bar = trackRect.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handleRect;
            bar.targetGraphic = handleImage;
            bar.direction = Scrollbar.Direction.BottomToTop;

            ColorBlock colors = bar.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);
            bar.colors = colors;

            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            return bar;
        }

        public const string BorderResourceFolder = "UI/Borders";

        public static RectTransform ApplyBorder(RectTransform target, BorderStyle style)
        {
            if (target == null || style == null) return null;

            Sprite borderSprite = ResolveBorderSprite(style);
            if (borderSprite == null) return null;

            RectTransform container = CreateStretchChild(target, "Border");
            Image image = container.gameObject.AddComponent<Image>();
            image.sprite = borderSprite;
            image.color = style.tint;
            image.raycastTarget = false;
            return container;
        }

        public static Sprite ResolveBorderSprite(BorderStyle style)
        {
            if (style.sprite != null) return style.sprite;

            if (string.IsNullOrEmpty(style.spriteName)) return null;

            string path = BorderResourceFolder + "/" + style.spriteName;
            if (artCache.TryGetValue(path, out Sprite cached)) return cached;

            Sprite sprite = LoadTextureSprite(path);
            if (sprite == null)
            {
                // Logged once per path, not once per panel.
                if (missingArt.Add(path))
                    Debug.LogWarning($"[UIBuilder] No border art found at Resources/{path}.");
                return null;
            }

            artCache[path] = sprite;
            return sprite;
        }

        public const string GameFontPath = "UI/MedievalSharp-Regular";

        public static Font GetFont(UITheme theme)
        {
            if (theme != null && theme.font != null) return theme.font;
            if (font == null)
                font = Resources.Load<Font>(GameFontPath)
                    ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }

        // Works whichever way the texture is imported, same as PlayerUI's
        // end screen.
        public static Sprite LoadIcon(string resourcePath)
        {
            Sprite single = Resources.Load<Sprite>(resourcePath);
            if (single != null) return single;

            Sprite[] sliced = Resources.LoadAll<Sprite>(resourcePath);
            return sliced != null && sliced.Length > 0 ? sliced[0] : null;
        }

        // For textures imported as plain Texture2D rather than Sprite.
        public static Sprite LoadTextureSprite(string resourcePath)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null) return null;
            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}