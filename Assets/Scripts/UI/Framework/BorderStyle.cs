using UnityEngine;

namespace Game.UI
{
    [System.Serializable]
    public class BorderStyle
    {
        [Tooltip("Border art name; loads Resources/UI/Borders/<name>. Drawn whole over the panel.")]
        public string spriteName = "";
        [Tooltip("Optional direct sprite reference.")]
        public Sprite sprite;
        [Tooltip("Tint multiplied onto the border art.")]
        public Color tint = Color.white;
    }
}
