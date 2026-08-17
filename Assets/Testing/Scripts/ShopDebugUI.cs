using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Game.Combat;

namespace Game.Core
{
    // Throwaway debug UI drawn with OnGUI. No Canvas, no prefabs, nothing
    // in the scene to conflict with the real UI when it lands.
    //
    // Always shows gold and the wave number. Shows the shop panel only while
    // the game is in the Shop state.
    //
    // Goes on the GameManager object.
    public class ShopDebugUI : MonoBehaviour
    {
        [SerializeField] private Shop shop;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private GameStateMachine stateMachine;

        [Header("Layout")]
        [SerializeField] private int panelWidth = 420;
        [SerializeField] private int rowHeight = 60;

        private const int EquipmentPageWidth = 780;
        private const int CellWidth = 350;
        private const int CellHeight = 160;
        private const int CellGap = 10;
        private const int PreviewSize = 120;

        private float secondsLeft;
        private bool equipmentPage;
        private Equipment playerEquipment;
        private readonly PreviewRenderer previews = new PreviewRenderer();

        private void OnEnable()
        {
            EventManager.OnShopTime += HandleShopTime;
            EventManager.OnGameStateChanged += HandleGameStateChanged;
        }

        private void OnDisable()
        {
            EventManager.OnShopTime -= HandleShopTime;
            EventManager.OnGameStateChanged -= HandleGameStateChanged;
        }

        private void HandleShopTime(ShopTimeArgs e)
        {
            secondsLeft = e.SecondsLeft;
        }

        private void HandleGameStateChanged(GameStateChangedArgs e)
        {
            if (e.Current == GameStateId.Shop)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (e.Previous == GameStateId.Shop)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnGUI()
        {
            if (stateMachine == null || wallet == null) return;

            DrawStatusBar();

            if (stateMachine.CurrentState == null) return;
            if (stateMachine.CurrentState.Id != GameStateId.Shop)
            {
                equipmentPage = false;
                return;
            }

            if (equipmentPage) DrawEquipmentPage();
            else DrawShopPanel();
        }

        // Gold and wave, top right, always visible.
        private void DrawStatusBar()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box);
            style.fontSize = 18;
            style.alignment = TextAnchor.MiddleLeft;
            style.padding = new RectOffset(12, 12, 8, 8);

            string text = $"Gold: {wallet.Gold}    Wave: {stateMachine.CurrentWave}    [{stateMachine.CurrentState?.Id}]";
            GUI.Box(new Rect(Screen.width - 380, 10, 370, 40), text, style);
        }

        private void DrawShopPanel()
        {
            if (shop == null) return;

            int rows = Mathf.Max(1, shop.ItemCount);
            int height = 130 + rows * rowHeight + 84;

            int x = (Screen.width - panelWidth) / 2;
            int y = (Screen.height - height) / 2;

            GUI.Box(new Rect(x, y, panelWidth, height), "");

            GUIStyle title = new GUIStyle(GUI.skin.label);
            title.fontSize = 24;
            title.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(x, y + 12, panelWidth, 32), "MARKET", title);

            GUIStyle timer = new GUIStyle(GUI.skin.label);
            timer.fontSize = 16;
            timer.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(x, y + 46, panelWidth, 24),
                $"{Mathf.CeilToInt(secondsLeft)}s left    ·    {wallet.Gold} gold", timer);

            int rowY = y + 80;

            for (int i = 0; i < shop.ItemCount; i++)
            {
                DrawItemRow(i, x + 15, rowY, panelWidth - 30);
                rowY += rowHeight;
            }

            rowY += 8;

            if (GUI.Button(new Rect(x + 15, rowY, panelWidth - 30, 34), "DONE"))
            {
                shop.Close();
            }

            rowY += 42;

            if (GUI.Button(new Rect(x + 15, rowY, panelWidth - 30, 34), "BUY EQUIPMENT"))
            {
                equipmentPage = true;
            }
        }

        private void DrawItemRow(int index, int x, int y, int width)
        {
            ShopItemDef item = shop.ItemAt(index);
            if (item == null) return;

            int cost = shop.CostOf(index);
            bool available = shop.IsAvailable(index);
            bool affordable = shop.CanAfford(index);

            string label = available
                ? $"{item.DisplayName} — {cost}g"
                : $"{item.DisplayName} — sold out";

            GUIStyle desc = new GUIStyle(GUI.skin.label);
            desc.fontSize = 12;

            GUI.Label(new Rect(x, y, width, 18), item.Description, desc);

            GUI.enabled = affordable;
            if (GUI.Button(new Rect(x, y + 18, width, 30), label))
            {
                shop.TryBuy(index);
            }
            GUI.enabled = true;
        }

        private void DrawEquipmentPage()
        {
            if (shop == null) return;

            IReadOnlyList<EquipmentEntry> entries = EquipmentCatalog.Entries;
            if (entries.Count == 0) return;

            int columns = Mathf.Max(1, (EquipmentPageWidth - 30 - CellGap) / (CellWidth + CellGap));
            int rows = Mathf.CeilToInt(entries.Count / (float)columns);
            int height = 120 + rows * (CellHeight + CellGap);

            int x = (Screen.width - EquipmentPageWidth) / 2;
            int y = (Screen.height - height) / 2;

            GUI.Box(new Rect(x, y, EquipmentPageWidth, height), "");

            GUIStyle title = new GUIStyle(GUI.skin.label);
            title.fontSize = 24;
            title.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(x, y + 12, EquipmentPageWidth, 32), "EQUIPMENT", title);

            GUIStyle timer = new GUIStyle(GUI.skin.label);
            timer.fontSize = 16;
            timer.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(x, y + 46, EquipmentPageWidth - 110, 24),
                $"{Mathf.CeilToInt(secondsLeft)}s left    ·    {wallet.Gold} gold", timer);

            if (GUI.Button(new Rect(x + EquipmentPageWidth - 105, y + 42, 90, 28), "BACK"))
            {
                equipmentPage = false;
            }

            int rowY = y + 84;

            for (int i = 0; i < entries.Count; i++)
            {
                int cx = x + 15 + (i % columns) * (CellWidth + CellGap);
                int cy = rowY + (i / columns) * (CellHeight + CellGap);
                DrawEquipmentCell(entries[i], cx, cy);
            }
        }

        private void DrawEquipmentCell(EquipmentEntry entry, int x, int y)
        {
            GUI.Box(new Rect(x, y, CellWidth, CellHeight), "");

            RenderTexture preview = previews.Get(entry.Prefab, this);
            if (preview != null)
            {
                DrawFlipped(new Rect(x + 12, y + (CellHeight - PreviewSize) / 2, PreviewSize, PreviewSize), preview);
            }

            int textX = x + PreviewSize + 24;
            int textW = CellWidth - PreviewSize - 36;

            GUIStyle nameStyle = new GUIStyle(GUI.skin.label);
            nameStyle.fontSize = 16;

            GUI.Label(new Rect(textX, y + 10, textW, 22), entry.Name, nameStyle);

            GUIStyle statStyle = new GUIStyle(GUI.skin.label);
            statStyle.fontSize = 13;
            statStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            string stats = entry.Kind == EquipmentKind.Weapon
                ? $"DMG {entry.Damage:0}    SWING {entry.Speed:0.0}s    LV {entry.Level}"
                : $"LV {entry.Level}";

            GUI.Label(new Rect(textX, y + 36, textW, 18), stats, statStyle);

            Equipment equip = PlayerEquipment();
            bool equipped = equip != null &&
                (entry.Kind == EquipmentKind.Shield
                    ? equip.ShieldPrefab == entry.Prefab
                    : equip.WeaponPrefab == entry.Prefab);

            string label = equipped ? "EQUIPPED" : $"BUY — {entry.Cost}g";

            GUI.enabled = !equipped && wallet.Gold >= entry.Cost;
            if (GUI.Button(new Rect(textX, y + 68, textW, 32), label))
            {
                shop.TryBuyEquipment(entry);
            }
            GUI.enabled = true;
        }

        private Equipment PlayerEquipment()
        {
            if (playerEquipment == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                playerEquipment = player != null ? player.GetComponent<Equipment>() : null;
            }
            return playerEquipment;
        }

        private static void DrawFlipped(Rect rect, Texture texture)
        {
            Matrix4x4 previous = GUI.matrix;
            GUIUtility.ScaleAroundPivot(new Vector2(1f, -1f),
                new Vector2(rect.x + rect.width / 2f, rect.y + rect.height / 2f));
            GUI.DrawTexture(rect, texture);
            GUI.matrix = previous;
        }

        private class PreviewRenderer
        {
            private const float RigY = -500f;

            private readonly Dictionary<GameObject, RenderTexture> cache =
                new Dictionary<GameObject, RenderTexture>();

            public RenderTexture Get(GameObject prefab, MonoBehaviour host)
            {
                if (cache.TryGetValue(prefab, out RenderTexture cached)) return cached;

                var rig = new GameObject("ShopPreviewRig");
                rig.transform.position = new Vector3(0f, RigY, 0f);
                rig.hideFlags = HideFlags.HideAndDontSave;

                GameObject instance = Object.Instantiate(prefab, rig.transform);
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;

                var renderers = instance.GetComponentsInChildren<Renderer>();
                if (renderers.Length == 0)
                {
                    Object.Destroy(rig);
                    return null;
                }

                Bounds bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);

                var camGo = new GameObject("PreviewCam");
                camGo.transform.SetParent(rig.transform);
                camGo.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.max.z + 1f);
                camGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

                var cam = camGo.AddComponent<Camera>();
                cam.orthographic = true;
                cam.orthographicSize = Mathf.Max(bounds.extents.x, bounds.extents.y, 0.3f) * 1.25f;
                cam.nearClipPlane = 0.01f;
                cam.farClipPlane = 5f;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0.13f, 0.13f, 0.13f, 1f);
                cam.enabled = true;

                var lightGo = new GameObject("PreviewLight");
                lightGo.transform.SetParent(rig.transform);
                lightGo.transform.rotation = Quaternion.Euler(40f, 200f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.2f;

                var rt = new RenderTexture(256, 256, 24);
                cam.targetTexture = rt;

                cache[prefab] = rt;

                if (host != null && host.isActiveAndEnabled)
                    host.StartCoroutine(DestroyAfterFrames(rig, cam, 6));
                else
                    Object.Destroy(rig);

                return rt;
            }

            private static IEnumerator DestroyAfterFrames(GameObject rig, Camera cam, int frames)
            {
                for (int i = 0; i < frames; i++) yield return null;

                cam.enabled = false;
                cam.targetTexture = null;
                Object.Destroy(rig);
            }
        }
    }
}
