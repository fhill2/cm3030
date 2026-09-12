using UnityEngine;

namespace Game.UI
{
    [CreateAssetMenu(fileName = "UITheme", menuName = "Fall of Camelot/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Palette")]
        [Tooltip("Panel, label and bar backgrounds.")]
        public Color panelBackground = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        [Tooltip("Full-screen dim behind overlay panels.")]
        public Color backdropDim = new Color(0f, 0f, 0f, 0.35f);
        [Tooltip("Primary text.")]
        public Color text = Color.white;
        [Tooltip("Secondary text: hints and descriptions.")]
        public Color dimText = new Color(1f, 1f, 1f, 0.6f);
        [Tooltip("Highlight color for buttons and accents.")]
        public Color accent = new Color(1f, 0.85f, 0.4f, 1f);
        [Tooltip("Button pressed tint.")]
        public Color buttonPressed = new Color(0.8f, 0.65f, 0.25f, 1f);

        [Header("Type")]
        [Tooltip("Leave empty for the built-in runtime font.")]
        public Font font;

        private static UITheme defaultTheme;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            defaultTheme = null;
        }

        // Built in memory rather than loaded from an asset, so the UI works
        // without a theme assigned.
        public static UITheme Default
        {
            get
            {
                if (defaultTheme == null)
                {
                    defaultTheme = CreateInstance<UITheme>();
                    defaultTheme.hideFlags = HideFlags.HideAndDontSave;
                }
                return defaultTheme;
            }
        }
    }
}