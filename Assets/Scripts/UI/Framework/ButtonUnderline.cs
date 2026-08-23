using UnityEngine;

namespace Game.UI
{
    public class ButtonUnderline : MonoBehaviour
    {
        private const float Height = 4f;
        private const float Speed = 14f;

        private RectTransform rt;
        private float currentWidth;
        private float targetWidth;

        private void Awake()
        {
            rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, Height);
            }
            currentWidth = 0f;
            targetWidth = 0f;
        }

        public void Show(float fullWidth) { targetWidth = fullWidth; }
        public void Hide() { targetWidth = 0f; }

        private void Update()
        {
            if (rt == null) return;
            currentWidth = Mathf.Lerp(currentWidth, targetWidth,
                1f - Mathf.Exp(-Speed * Time.unscaledDeltaTime));
            rt.sizeDelta = new Vector2(currentWidth, Height);
        }
    }
}
