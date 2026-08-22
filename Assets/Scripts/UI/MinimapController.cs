using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Game.UI
{
    public class MinimapController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform minimapCamera;
        [SerializeField] private WaveSpawner waveSpawner;

        [Header("UI")]
        [Tooltip("The minimap's RawImage rect — dots and the player icon are positioned relative to this.")]
        [SerializeField] private RectTransform mapRect;
        [SerializeField] private RectTransform playerIcon;
        [Tooltip("Prefab for one enemy dot. A small red UI Image is enough.")]
        [SerializeField] private RectTransform enemyDotPrefab;

        [Header("Camera Follow")]
        [Tooltip("Height above the player the camera sits at, looking straight down.")]
        [SerializeField] private float cameraHeight = 30f;

        [Header("Render Throttle")]
        [Tooltip("Seconds between minimap camera renders. 0.1 = 10 Hz; 0 renders every frame.")]
        [SerializeField] private float renderInterval = 0.1f;

        [Header("Edge Clamping")]
        [Tooltip("Enemies outside the camera's view get pinned to the rim of the panel, this many pixels in from the actual edge, instead of being placed off the panel entirely.")]
        [SerializeField] private float edgePadding = 8f;

        [Header("Visibility")]
        [Tooltip("Optional. If set, the minimap fades in/out with a CanvasGroup instead of always being visible — hidden during Menu, shown for every other state. Leave empty to keep the old always-on behaviour.")]
        [SerializeField] private CanvasGroup visibilityGroup;


        private Camera cam;
        private bool isVisible = true;
        private float renderTimer;
        private readonly Dictionary<GameObject, RectTransform> dots = new Dictionary<GameObject, RectTransform>();
        private readonly List<GameObject> staleBuffer = new List<GameObject>();

        private void Awake()
        {
            cam = minimapCamera != null ? minimapCamera.GetComponent<Camera>() : null;

            // The camera component stays disabled so Unity never auto-renders it
            // every frame; RenderMinimap() forces a manual render on a timer.
            if (cam != null) cam.enabled = false;

            // Hidden by default the instant the scene loads, before the game
            // state machine's first OnGameStateChanged has a chance to fire —
            // avoids a one-frame flash of the map over the start screen.
            if (visibilityGroup != null) SetVisible(false);
        }

        private void OnEnable()
        {
            EventManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            EventManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        // Only the Menu (start screen) hides the map — every other state
        // (WaveActive, WaveComplete, Shop, GameOver) shows it.
        private void HandleGameStateChanged(GameStateChangedArgs e)
        {
            SetVisible(e.Current != GameStateId.Menu);
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;

            if (visibilityGroup == null) return;
            visibilityGroup.alpha = visible ? 1f : 0f;
            visibilityGroup.interactable = visible;
            visibilityGroup.blocksRaycasts = visible;
        }

        private void LateUpdate()
        {
            if (player == null || cam == null || mapRect == null) return;
            if (visibilityGroup != null && !isVisible) return;

            if (cam.enabled) cam.enabled = false;

            UpdatePlayerIcon();
            UpdateEnemyDots();
            RenderMinimap();
        }

        private void RenderMinimap()
        {
            renderTimer += Time.unscaledDeltaTime;
            if (renderInterval > 0f && renderTimer < renderInterval) return;
            renderTimer = 0f;

            FollowPlayer();
            cam.Render();
        }

        private void FollowPlayer()
        {
            Vector3 pos = player.position;
            minimapCamera.position = new Vector3(pos.x, pos.y + cameraHeight, pos.z);
        }

        private void UpdatePlayerIcon()
        {
            if (playerIcon == null) return;

            playerIcon.anchoredPosition = Vector2.zero;
            playerIcon.localRotation = Quaternion.Euler(0f, 0f, -player.eulerAngles.y);
        }

        private void UpdateEnemyDots()
        {
            IReadOnlyList<GameObject> enemies = waveSpawner != null ? waveSpawner.LiveEnemies : null;

            if (enemies == null || enemies.Count == 0)
            {
                ClearAllDots();
                return;
            }

            RemoveStaleDots(enemies);

            foreach (GameObject enemy in enemies)
            {
                if (enemy == null) continue;

                if (!dots.TryGetValue(enemy, out RectTransform dot))
                {
                    dot = Instantiate(enemyDotPrefab, mapRect);
                    dot.gameObject.SetActive(true);
                    dots[enemy] = dot;
                }

                PositionOnMap(dot, enemy.transform.position);
            }
        }

        private void RemoveStaleDots(IReadOnlyList<GameObject> liveEnemies)
        {
            if (dots.Count == 0) return;

            staleBuffer.Clear();
            foreach (KeyValuePair<GameObject, RectTransform> kvp in dots)
            {
                if (kvp.Key == null || !Contains(liveEnemies, kvp.Key))
                    staleBuffer.Add(kvp.Key);
            }

            foreach (GameObject key in staleBuffer)
            {
                if (dots.TryGetValue(key, out RectTransform dot) && dot != null)
                    Destroy(dot.gameObject);
                dots.Remove(key);
            }
        }

        private static bool Contains(IReadOnlyList<GameObject> list, GameObject item)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] == item) return true;
            return false;
        }

        private void ClearAllDots()
        {
            foreach (RectTransform dot in dots.Values)
                if (dot != null) Destroy(dot.gameObject);
            dots.Clear();
        }

        private void PositionOnMap(RectTransform icon, Vector3 worldPos)
        {
            Vector3 viewport = cam.WorldToViewportPoint(worldPos);

            float x = (viewport.x - 0.5f) * mapRect.rect.width;
            float y = (viewport.y - 0.5f) * mapRect.rect.height;

            float radius = Mathf.Min(mapRect.rect.width, mapRect.rect.height) * 0.5f - edgePadding;
            Vector2 pos = new Vector2(x, y);
            if (pos.magnitude > radius) pos = pos.normalized * radius;

            icon.anchoredPosition = pos;
        }

        private void OnDestroy()
        {
            ClearAllDots();
        }
    }
}
