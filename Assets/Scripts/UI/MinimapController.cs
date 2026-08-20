using System.Collections.Generic;
using UnityEngine;
using Game.Core;

namespace Game.UI
{
    // Bottom-left minimap: a top-down camera feed of the castle with the
    // player centered, plus a red dot for every live enemy so the player can
    // find fights instead of wandering the whole map looking for them.
    //
    // The minimap camera only translates to follow the player's XZ position —
    // it never rotates — so the map stays north-up and dot positions are a
    // plain viewport conversion, no manual trig needed.
    //
    // Enemy dots are pooled per-enemy (one RectTransform per GameObject in
    // WaveSpawner.LiveEnemies) and cleaned up automatically as enemies die,
    // so there's no separate death-tracking needed here.
    //
    // Goes on the minimap Canvas object, alongside the RawImage that shows
    // the camera's RenderTexture.
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

        [Header("Edge Clamping")]
        [Tooltip("Enemies outside the camera's view get pinned to the rim of the panel, this many pixels in from the actual edge, instead of being placed off the panel entirely.")]
        [SerializeField] private float edgePadding = 8f;

        private Camera cam;
        private readonly Dictionary<GameObject, RectTransform> dots = new Dictionary<GameObject, RectTransform>();
        private readonly List<GameObject> staleBuffer = new List<GameObject>();

        private void Awake()
        {
            cam = minimapCamera != null ? minimapCamera.GetComponent<Camera>() : null;
        }

        private void LateUpdate()
        {
            if (player == null || cam == null || mapRect == null) return;

            FollowPlayer();
            UpdatePlayerIcon();
            UpdateEnemyDots();
        }

        // Keeps the camera centered over the player, looking straight down,
        // never rotating, so the minimap always reads north-up.
        private void FollowPlayer()
        {
            Vector3 pos = player.position;
            minimapCamera.position = new Vector3(pos.x, pos.y + cameraHeight, pos.z);
        }

        private void UpdatePlayerIcon()
        {
            if (playerIcon == null) return;

            // The player is always at the camera's center by construction
            // (FollowPlayer keeps them there), so the icon just sits in the
            // middle of the map rect — only its rotation needs updating, to
            // point the way the player's facing.
            //
            // NOTE: if the arrow ends up pointing the wrong way in testing,
            // flip the sign here — it depends on the exact rotation you give
            // the top-down camera in the editor setup.
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

        // Drops dots for anyone who died or despawned since the last check.
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

        // Converts a world position to a point inside the map rect via the
        // minimap camera's viewport — works cleanly for a top-down
        // orthographic camera without any manual trig.
        //
        // An enemy outside the camera's orthographic view produces a
        // viewport value outside [0,1], which without clamping would place
        // the dot way outside the panel — even over the 3D game view behind
        // it. Clamping pins it to the rim instead, in the right direction,
        // so a far-off enemy still shows as "something's over there" rather
        // than vanishing or floating off the minimap entirely.
        //
        // The map is masked into a circle, so the clamp has to follow that
        // circle too (by vector magnitude) rather than clamping X and Y
        // independently — a square clamp lets dots sit in the square's
        // corners, which are outside the visible circle and get sliced by
        // the mask into odd shapes.
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
