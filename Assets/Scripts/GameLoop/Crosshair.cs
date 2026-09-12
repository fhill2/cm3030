using UnityEngine;

namespace Game.Core
{
    // Dot at the centre of the screen showing where spells will go.
    // Hidden while the shop or menu is open, and after death.
    // Sits on the GameManager.
    public class Crosshair : MonoBehaviour
    {
        [SerializeField] private float size = 6f;
        [SerializeField] private Color colour = new Color(1f, 1f, 1f, 0.75f);

        private Texture2D dot;

        private void Awake()
        {
            dot = new Texture2D(1, 1);
            dot.SetPixel(0, 0, Color.white);
            dot.Apply();
        }

        private void OnGUI()
        {
            if (PlayerInputLock.InputLocked) return;

            Color previous = GUI.color;
            GUI.color = colour;

            GUI.DrawTexture(new Rect(
                (Screen.width - size) * 0.5f,
                (Screen.height - size) * 0.5f,
                size, size), dot);

            GUI.color = previous;
        }
    }
}