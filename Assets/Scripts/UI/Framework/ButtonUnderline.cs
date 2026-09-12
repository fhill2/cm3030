using UnityEngine;

namespace Game.UI
{
    public class ButtonUnderline : MonoBehaviour
    {
        private const float Height = 4f;
        private const float Speed = 14f;

        private RectTransform rect;
        private float currentWidth;
        private float targetWidth;

        private void Awake()
        {
            rect = GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(0f, Height);
            }
            currentWidth = 0f;
            targetWidth = 0f;
        }

        public void Show(float fullWidth) { targetWidth = fullWidth; }
        public void Hide() { targetWidth = 0f; }

        private void Update()
        {
            if (rect == null) return;
            currentWidth = Mathf.Lerp(currentWidth, targetWidth,
                1f - Mathf.Exp(-Speed * Time.unscaledDeltaTime));
            rect.sizeDelta = new Vector2(currentWidth, Height);
        }
    }
}