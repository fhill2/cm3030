using UnityEngine;

namespace Game.UI
{
    [System.Serializable]
    public class BorderStyle
    {
        public enum BorderMode
        {
            None,
            Edges,
            Ring,
            Sprite
        }

        public BorderMode mode = BorderMode.None;
        public Color color = new Color(0.85f, 0.72f, 0.35f, 1f);
        [Tooltip("Border thickness in reference pixels.")]
        public float thickness = 3f;

        [Header("Edges")]
        public bool top = true;
        public bool bottom = true;
        public bool left = true;
        public bool right = true;

        [Header("Sprite")]
        [Tooltip("9-sliced frame sprite. Only used in Sprite mode.")]
        public Sprite sprite;
        [Tooltip("Dynamically loads Resources/UI/Borders/<name> at runtime when Sprite is empty. Works for Sprite mode (9-sliced panel frame) and Ring mode (circular frame art).")]
        public string spriteName = "";
        [Tooltip("Fraction of the shorter side used as the 9-slice border band. Keep all frame detail inside this fraction of the artwork.")]
        [Range(0.05f, 0.45f)] public float spriteInset = 0.25f;

        public bool IsVisible
        {
            get
            {
                if (mode == BorderMode.None || thickness <= 0f || color.a <= 0f) return false;
                if (mode == BorderMode.Sprite && sprite == null && string.IsNullOrEmpty(spriteName)) return false;
                if (mode == BorderMode.Edges && !top && !bottom && !left && !right) return false;
                return true;
            }
        }
    }
}
