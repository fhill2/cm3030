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
        private static readonly Dictionary<string, Sprite> s_ringCache = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, Sprite> s_borderCache = new Dictionary<string, Sprite>();
        private static readonly HashSet<string> s_missingBorders = new HashSet<string>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_font = null;
            s_ringCache.Clear();
            s_borderCache.Clear();
            s_missingBorders.Clear();
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

        public static Button CreateButton(Transform parent, string name, string label, Vector2 anchor,
            Vector2 anchoredPosition, float width, float height, UITheme theme, int fontSize,
            System.Action onClick = null)
        {
            var rt = CreateRect(parent, anchor, anchor, anchoredPosition, new Vector2(width, height));
            rt.gameObject.name = name;
            var img = AttachImage(rt, theme.buttonBackground);

            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = theme.accent;
            colors.pressedColor = theme.buttonPressed;
            button.colors = colors;
            if (onClick != null) button.onClick.AddListener(() => onClick());

            var labelRt = CreateStretchChild(rt, "Text");
            AttachText(labelRt, label, TextAnchor.MiddleCenter, fontSize, theme.text, GetFont(theme));
            return button;
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

        public static RectTransform CreateTitledPanel(Transform parent, string name, Vector2 anchor,
            Vector2 anchoredPosition, Vector2 size, string title, UITheme theme,
            out Text titleText, BorderStyle border = null, Color? background = null)
        {
            var panel = CreatePanel(parent, name, anchor, anchoredPosition, size, theme, border, background);

            var titleRt = CreateRect(panel, new Vector2(0f, 1f), new Vector2(1f, 1f),
                Vector2.zero, new Vector2(0f, 40f));
            titleRt.gameObject.name = "Title";
            titleText = AttachText(titleRt, title, TextAnchor.MiddleCenter, 24, theme.text, GetFont(theme));
            return panel;
        }

        public static ScrollRect CreateScrollList(Transform parent, string name, Vector2 anchor,
            Vector2 anchoredPosition, Vector2 size, UITheme theme, out RectTransform content)
        {
            var rootRt = CreateRect(parent, anchor, anchor, anchoredPosition, size);
            rootRt.gameObject.name = name;
            AttachImage(rootRt, theme.panelBackground);

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

        public static RectTransform ApplyBorder(RectTransform target, BorderStyle style)
        {
            if (target == null || style == null || !style.IsVisible) return null;

            var containerRt = CreateStretchChild(target, "Border");

            switch (style.mode)
            {
                case BorderStyle.BorderMode.Edges:
                    CreateEdgeStrip(containerRt, "Top", style, isTop: true);
                    CreateEdgeStrip(containerRt, "Bottom", style, isTop: false);
                    CreateEdgeStrip(containerRt, "Left", style, isTop: false, isVertical: true);
                    CreateEdgeStrip(containerRt, "Right", style, isTop: false, isVertical: true);
                    break;

                case BorderStyle.BorderMode.Ring:
                    Sprite ringArt = ResolveBorderSprite(style);
                    if (ringArt != null)
                    {
                        var art = containerRt.gameObject.AddComponent<Image>();
                        art.sprite = ringArt;
                        art.preserveAspect = true;
                        art.raycastTarget = false;
                        break;
                    }

                    float diameter = Mathf.Min(target.rect.width, target.rect.height);
                    if (diameter <= 0f) diameter = Mathf.Min(target.sizeDelta.x, target.sizeDelta.y);
                    if (diameter <= 0f) break;

                    var ring = containerRt.gameObject.AddComponent<Image>();
                    ring.sprite = GetRingSprite(diameter, style.thickness, style.color);
                    ring.raycastTarget = false;
                    break;

                case BorderStyle.BorderMode.Sprite:
                    Sprite frameSprite = ResolveBorderSprite(style);
                    if (frameSprite == null)
                    {
                        CreateEdgeStrip(containerRt, "Top", style, isTop: true);
                        CreateEdgeStrip(containerRt, "Bottom", style, isTop: false);
                        CreateEdgeStrip(containerRt, "Left", style, isTop: false, isVertical: true);
                        CreateEdgeStrip(containerRt, "Right", style, isTop: false, isVertical: true);
                        break;
                    }

                    var frame = containerRt.gameObject.AddComponent<Image>();
                    frame.sprite = frameSprite;
                    frame.type = Image.Type.Sliced;
                    frame.color = style.color;
                    frame.raycastTarget = false;
                    break;
            }

            return containerRt;
        }

        public const string BorderResourceFolder = "UI/Borders";

        public static Sprite ResolveBorderSprite(BorderStyle style)
        {
            if (style.sprite != null) return style.sprite;
            if (string.IsNullOrEmpty(style.spriteName)) return null;
            return LoadBorderSprite(style.spriteName, style.spriteInset);
        }

        public static Sprite LoadBorderSprite(string name, float inset)
        {
            if (string.IsNullOrEmpty(name)) return null;

            string path = BorderResourceFolder + "/" + name;
            string key = $"{name}_{inset:0.###}";
            if (s_borderCache.TryGetValue(key, out Sprite cached)) return cached;

            var tex = Resources.Load<Texture2D>(path);
            if (tex == null)
            {
                if (s_missingBorders.Add(name))
                    Debug.LogWarning($"[UIBuilder] No border art found at Resources/{path}. " +
                                     "Falling back to the generated border.");
                return null;
            }

            float band = Mathf.Min(tex.width, tex.height) * Mathf.Clamp01(inset);
            var border = new Vector4(band, band, band, band);
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            s_borderCache[key] = sprite;
            return sprite;
        }

        private static void CreateEdgeStrip(RectTransform parent, string name, BorderStyle style,
            bool isTop, bool isVertical = false)
        {
            bool on = isVertical ? (name == "Left" ? style.left : style.right)
                                 : (isTop ? style.top : style.bottom);
            if (!on) return;

            float t = style.thickness;
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();

            if (isVertical)
            {
                float x = name == "Left" ? 0f : 1f;
                rt.anchorMin = new Vector2(x, 0f);
                rt.anchorMax = new Vector2(x, 1f);
                rt.offsetMin = new Vector2(name == "Left" ? 0f : -t, style.bottom ? t : 0f);
                rt.offsetMax = new Vector2(name == "Left" ? t : 0f, style.top ? -t : 0f);
            }
            else
            {
                rt.anchorMin = new Vector2(0f, isTop ? 1f : 0f);
                rt.anchorMax = new Vector2(1f, isTop ? 1f : 0f);
                rt.offsetMin = new Vector2(style.left ? t : 0f, isTop ? -t : 0f);
                rt.offsetMax = new Vector2(style.right ? -t : 0f, isTop ? 0f : t);
            }

            var img = go.AddComponent<Image>();
            img.color = style.color;
            img.raycastTarget = false;
        }

        private static Sprite GetRingSprite(float rectSize, float thickness, Color color)
        {
            int d = Mathf.Max(16, Mathf.CeilToInt(rectSize));
            int t = Mathf.Clamp(Mathf.RoundToInt(thickness), 2, d / 4);
            string key = $"{d}_{t}_{ColorUtility.ToHtmlStringRGBA(color)}";
            if (s_ringCache.TryGetValue(key, out Sprite cached)) return cached;

            var tex = new Texture2D(d, d, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;

            float c = (d - 1) * 0.5f;
            float radius = d * 0.5f - t * 0.5f - 0.5f;
            float inner = radius - t * 0.5f;
            float outer = radius + t * 0.5f;

            var clear = new Color(0f, 0f, 0f, 0f);
            for (int y = 0; y < d; y++)
            {
                for (int x = 0; x < d; x++)
                {
                    float dist = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float alpha = Mathf.Clamp01(outer - dist + 0.5f) *
                                  Mathf.Clamp01(dist - inner + 0.5f);
                    tex.SetPixel(x, y, alpha <= 0f ? clear : new Color(color.r, color.g, color.b, color.a * alpha));
                }
            }

            tex.Apply();
            var sprite = Sprite.Create(tex, new Rect(0f, 0f, d, d), new Vector2(0.5f, 0.5f), 100f);
            s_ringCache[key] = sprite;
            return sprite;
        }

        public static Font GetFont(UITheme theme)
        {
            if (theme != null && theme.font != null) return theme.font;
            if (s_font == null) s_font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
