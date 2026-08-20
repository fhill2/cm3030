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

        private const int CellWidth = 350;
        private const int CellHeight = 160;
        private const int CellGap = 10;
        private const int PreviewSize = 120;

        private float secondsLeft;
        private bool equipmentPage;
        private Vector2 scrollPosition;
        private Equipment playerEquipment;
        private readonly Dictionary<GameObject, Texture2D> thumbnails = new Dictionary<GameObject, Texture2D>();
        private bool warnedMissingThumbnail;

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

            // Gold and wave moved to PlayerUI as real canvas elements. They are
            // not drawn here any more: IMGUI renders above every
            // ScreenSpaceOverlay canvas, so this box always covered the HUD
            // rather than sitting alongside it.

            if (stateMachine.CurrentState == null) return;
            if (stateMachine.CurrentState.Id != GameStateId.Shop)
            {
                equipmentPage = false;
                return;
            }

            if (equipmentPage) DrawEquipmentPage();
            else DrawShopPanel();
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

            int pageWidth = Screen.width - 40;
            int pageHeight = Screen.height - 20;

            int columns = Mathf.Max(1, (pageWidth - 30 - CellGap) / (CellWidth + CellGap));
            int rows = Mathf.CeilToInt(entries.Count / (float)columns);
            int contentHeight = 120 + rows * (CellHeight + CellGap);

            int x = 20;
            int y = 10;

            GUI.Box(new Rect(x, y, pageWidth, pageHeight), "");

            GUIStyle title = new GUIStyle(GUI.skin.label);
            title.fontSize = 24;
            title.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(x, y + 12, pageWidth, 32), "EQUIPMENT", title);

            GUIStyle timer = new GUIStyle(GUI.skin.label);
            timer.fontSize = 16;
            timer.alignment = TextAnchor.MiddleCenter;

            GUI.Label(new Rect(x, y + 46, pageWidth - 110, 24),
                $"{Mathf.CeilToInt(secondsLeft)}s left    ·    {wallet.Gold} gold", timer);

            if (GUI.Button(new Rect(x + pageWidth - 105, y + 42, 90, 28), "BACK"))
            {
                equipmentPage = false;
            }

            int rowY = y + 84;
            int gridHeight = pageHeight - 94;

            if (contentHeight > gridHeight)
            {
                scrollPosition = GUI.BeginScrollView(
                    new Rect(x + 5, rowY, pageWidth - 10, gridHeight),
                    scrollPosition,
                    new Rect(0, 0, pageWidth - 30, contentHeight - 94));

                for (int i = 0; i < entries.Count; i++)
                {
                    int cx = 10 + (i % columns) * (CellWidth + CellGap);
                    int cy = (i / columns) * (CellHeight + CellGap);
                    DrawEquipmentCell(entries[i], cx, cy);
                }

                GUI.EndScrollView();
            }
            else
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    int cx = x + 15 + (i % columns) * (CellWidth + CellGap);
                    int cy = rowY + (i / columns) * (CellHeight + CellGap);
                    DrawEquipmentCell(entries[i], cx, cy);
                }
            }
        }

        private void DrawEquipmentCell(EquipmentEntry entry, int x, int y)
        {
            GUI.Box(new Rect(x, y, CellWidth, CellHeight), "");

            Texture2D preview = GetThumbnail(entry);
            Texture draw = preview != null ? preview : Placeholder();
            GUI.DrawTexture(new Rect(x + 12, y + (CellHeight - PreviewSize) / 2, PreviewSize, PreviewSize), draw);

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

        private Texture2D GetThumbnail(EquipmentEntry entry)
        {
            if (thumbnails.TryGetValue(entry.Prefab, out Texture2D cached)) return cached;

            string folder = entry.Kind == EquipmentKind.Shield ? "Equipment/Shields" : "Equipment/Weapons";
            Texture2D tex = Resources.Load<Texture2D>(folder + "/" + entry.Prefab.name);

            if (tex == null && !warnedMissingThumbnail)
            {
                warnedMissingThumbnail = true;
                Debug.LogWarning("[ShopDebugUI] Missing thumbnail for '" + entry.Prefab.name +
                    "' — run Tools > Equipment > Generate Thumbnails.");
            }

            thumbnails[entry.Prefab] = tex;
            return tex;
        }

        private static Texture2D s_placeholder;

        private static Texture2D Placeholder()
        {
            if (s_placeholder == null)
            {
                s_placeholder = new Texture2D(2, 2);
                for (int y = 0; y < 2; y++)
                    for (int x = 0; x < 2; x++)
                        s_placeholder.SetPixel(x, y, new Color(0.22f, 0.22f, 0.22f, 1f));
                s_placeholder.Apply();
            }
            return s_placeholder;
        }
    }
}
