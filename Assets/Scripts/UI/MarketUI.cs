using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Game.Combat;
using Game.Core;

namespace Game.UI
{
    public class MarketUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Leave empty to find the Shop, PlayerWallet and GameStateMachine in the scene.")]
        [SerializeField] private Shop shop;
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private GameStateMachine stateMachine;

        [Header("Theme")]
        [Tooltip("Leave empty for the built-in default theme.")]
        [SerializeField] private UITheme theme;
        [Tooltip("Border around the two market pages.")]
        [SerializeField] private BorderStyle panelBorder = new BorderStyle
        {
            mode = BorderStyle.BorderMode.Sprite,
            color = new Color(0.85f, 0.72f, 0.35f, 1f),
            thickness = 4f,
            spriteName = "market_frame"
        };
        [Tooltip("Border around one equipment cell.")]
        [SerializeField] private BorderStyle cellBorder = new BorderStyle
        {
            mode = BorderStyle.BorderMode.Edges,
            color = new Color(0.85f, 0.72f, 0.35f, 1f),
            thickness = 2f
        };

        private const float PanelWidth   = 560f;
        private const float HeaderHeight = 96f;
        private const float RowHeight    = 64f;
        private const float RowGap       = 8f;
        private const float FooterHeight = 96f;
        private const float Padding      = 16f;

        private const float EquipPageWidth  = 1600f;
        private const float EquipPageHeight = 920f;
        private const int   Columns      = 3;
        private const float CellWidth    = 480f;
        private const float CellHeight   = 150f;
        private const float CellGap      = 12f;
        private const float PreviewSize  = 120f;

        private class ItemRow
        {
            public int Index;
            public Button Buy;
            public Text BuyLabel;
        }

        private class EquipCell
        {
            public EquipmentEntry Entry;
            public Button Buy;
            public Text BuyLabel;
        }

        private GameObject root;
        private GameObject marketPage;
        private GameObject equipmentPage;

        private Text marketTimer;
        private Text equipmentTimer;

        private readonly List<ItemRow> rows = new List<ItemRow>();
        private readonly List<EquipCell> cells = new List<EquipCell>();
        private readonly Dictionary<GameObject, Sprite> thumbnails = new Dictionary<GameObject, Sprite>();
        private bool warnedMissingThumbnail;

        private float secondsLeft;
        private Equipment playerEquipment;

        private UITheme Theme => theme != null ? theme : UITheme.Default;

        private void Awake()
        {
            if (shop == null) shop = FindFirstObjectByType<Shop>();
            if (wallet == null) wallet = FindFirstObjectByType<PlayerWallet>();
            if (stateMachine == null) stateMachine = FindFirstObjectByType<GameStateMachine>();

            BuildUI();
        }

        private void OnEnable()
        {
            EventManager.OnGameStateChanged += HandleGameStateChanged;
            EventManager.OnShopTime += HandleShopTime;
            EventManager.OnGoldChanged += HandleGoldChanged;
            EventManager.OnShopChanged += HandleShopChanged;
        }

        private void OnDisable()
        {
            EventManager.OnGameStateChanged -= HandleGameStateChanged;
            EventManager.OnShopTime -= HandleShopTime;
            EventManager.OnGoldChanged -= HandleGoldChanged;
            EventManager.OnShopChanged -= HandleShopChanged;
        }

        private void HandleGameStateChanged(GameStateChangedArgs e)
        {
            if (e.Current == GameStateId.Shop)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;

                if (root != null && !root.activeSelf)
                {
                    root.SetActive(true);
                    ShowMarketPage();
                    RefreshAll();
                }
            }
            else if (e.Previous == GameStateId.Shop)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                if (root != null) root.SetActive(false);
            }
        }

        private void HandleShopTime(ShopTimeArgs e)
        {
            secondsLeft = e.SecondsLeft;
            RefreshHeader();
        }

        private void HandleGoldChanged(GoldChangedArgs e)
        {
            RefreshHeader();
            RefreshRows();
            RefreshCells();
        }

        private void HandleShopChanged()
        {
            RefreshRows();
        }

        private void RefreshAll()
        {
            RefreshHeader();
            RefreshRows();
            RefreshCells();
        }

        private void RefreshHeader()
        {
            if (wallet == null) return;

            string line = $"{Mathf.CeilToInt(secondsLeft)}s left    \u00b7    {wallet.Gold} gold";
            if (marketTimer != null) marketTimer.text = line;
            if (equipmentTimer != null) equipmentTimer.text = line;
        }

        private void RefreshRows()
        {
            if (shop == null) return;

            foreach (ItemRow row in rows)
            {
                ShopItemDef item = shop.ItemAt(row.Index);
                if (item == null) continue;

                int cost = shop.CostOf(row.Index);
                bool available = shop.IsAvailable(row.Index);

                row.BuyLabel.text = available
                    ? $"{item.DisplayName} \u2014 {cost}g"
                    : $"{item.DisplayName} \u2014 sold out";
                row.Buy.interactable = shop.CanAfford(row.Index);
            }
        }

        private void RefreshCells()
        {
            Equipment equip = PlayerEquipment();

            foreach (EquipCell cell in cells)
            {
                bool equipped = equip != null &&
                    (cell.Entry.Kind == EquipmentKind.Shield
                        ? equip.ShieldPrefab == cell.Entry.Prefab
                        : equip.WeaponPrefab == cell.Entry.Prefab);

                cell.BuyLabel.text = equipped ? "EQUIPPED" : $"BUY \u2014 {cell.Entry.Cost}g";
                cell.Buy.interactable = !equipped && wallet != null && wallet.Gold >= cell.Entry.Cost;
            }
        }

        private void BuyItem(int index)
        {
            if (shop == null) return;

            shop.TryBuy(index);
        }

        private void BuyEquipment(EquipmentEntry entry)
        {
            if (shop == null) return;

            shop.TryBuyEquipment(entry);

            RefreshCells();
        }

        private void ShowMarketPage()
        {
            if (marketPage != null) marketPage.SetActive(true);
            if (equipmentPage != null) equipmentPage.SetActive(false);
        }

        private void ShowEquipmentPage()
        {
            if (marketPage != null) marketPage.SetActive(false);
            if (equipmentPage != null) equipmentPage.SetActive(true);
        }

        private void BuildUI()
        {
            UIBuilder.EnsureEventSystem();

            root = UIBuilder.CreateOverlayCanvas("MarketUICanvas", sortingOrder: 50).gameObject;

            var backdropRt = UIBuilder.CreateStretchChild(root.transform, "Backdrop");
            var backdrop = UIBuilder.AttachImage(backdropRt, Theme.backdropDim);
            backdrop.raycastTarget = true;

            marketPage = BuildMarketPage();
            equipmentPage = BuildEquipmentPage();

            root.SetActive(false);
        }

        private GameObject BuildMarketPage()
        {
            int itemCount = shop != null ? shop.ItemCount : 0;
            float rowsHeight = Mathf.Max(1, itemCount) * (RowHeight + RowGap);
            float height = HeaderHeight + rowsHeight + FooterHeight;

            var panel = UIBuilder.CreateTitledPanel(root.transform, "MarketPanel",
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PanelWidth, height),
                "MARKET", Theme, out _, panelBorder);

            marketTimer = UIBuilder.CreateText(panel, "Timer", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f),
                new Vector2(0f, 24f), TextAnchor.MiddleCenter, 16, Theme.text, Theme);

            float y = -HeaderHeight;
            for (int i = 0; i < itemCount; i++)
            {
                CreateItemRow(panel, i, y);
                y -= RowHeight + RowGap;
            }

            float contentWidth = PanelWidth - Padding * 2f;

            UIBuilder.CreateButton(panel, "DoneButton", "DONE",
                new Vector2(0f, 0f), new Vector2(Padding, 48f), contentWidth, 36f,
                Theme, 18, CloseShop);

            UIBuilder.CreateButton(panel, "EquipmentButton", "BUY EQUIPMENT",
                new Vector2(0f, 0f), new Vector2(Padding, 8f), contentWidth, 36f,
                Theme, 18, ShowEquipmentPage);

            return panel.gameObject;
        }

        private void CreateItemRow(RectTransform panel, int index, float topY)
        {
            ShopItemDef item = shop.ItemAt(index);
            if (item == null) return;

            float contentWidth = PanelWidth - Padding * 2f;

            UIBuilder.CreateText(panel, "Description", item.Description,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(Padding, topY),
                new Vector2(contentWidth, 20f), TextAnchor.MiddleLeft, 14,
                Theme.dimText, Theme);

            var buy = UIBuilder.CreateButton(panel, "BuyButton", "",
                new Vector2(0f, 1f), new Vector2(Padding, topY - 24f), contentWidth, 34f,
                Theme, 18, () => BuyItem(index));

            rows.Add(new ItemRow
            {
                Index = index,
                Buy = buy,
                BuyLabel = buy.GetComponentInChildren<Text>()
            });
        }

        private GameObject BuildEquipmentPage()
        {
            var panel = UIBuilder.CreateTitledPanel(root.transform, "EquipmentPanel",
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(EquipPageWidth, EquipPageHeight),
                "EQUIPMENT", Theme, out _, panelBorder);

            equipmentTimer = UIBuilder.CreateText(panel, "Timer", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f),
                new Vector2(0f, 24f), TextAnchor.MiddleCenter, 16, Theme.text, Theme);

            UIBuilder.CreateButton(panel, "BackButton", "BACK",
                new Vector2(1f, 1f), new Vector2(-Padding, -42f), 110f, 30f,
                Theme, 16, ShowMarketPage);

            IReadOnlyList<EquipmentEntry> entries = EquipmentCatalog.Entries;
            int rowCount = Mathf.Max(1, Mathf.CeilToInt(entries.Count / (float)Columns));
            float contentHeight = rowCount * (CellHeight + CellGap) + Padding * 2f;

            UIBuilder.CreateScrollList(panel, "Grid",
                new Vector2(0.5f, 1f), new Vector2(0f, -HeaderHeight),
                new Vector2(EquipPageWidth - Padding * 2f, EquipPageHeight - HeaderHeight - Padding),
                Theme, out RectTransform content);
            content.sizeDelta = new Vector2(0f, contentHeight);

            for (int i = 0; i < entries.Count; i++)
                CreateEquipmentCell(content, entries[i], i);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private void CreateEquipmentCell(RectTransform content, EquipmentEntry entry, int index)
        {
            int column = index % Columns;
            int row = index / Columns;

            float innerWidth = EquipPageWidth - Padding * 2f;
            float gridWidth = Columns * CellWidth + (Columns - 1) * CellGap;
            float x0 = (innerWidth - gridWidth) * 0.5f;

            var cell = UIBuilder.CreatePanel(content, "Cell_" + entry.Name,
                new Vector2(0f, 1f),
                new Vector2(x0 + column * (CellWidth + CellGap),
                            -(Padding + row * (CellHeight + CellGap))),
                new Vector2(CellWidth, CellHeight), Theme, cellBorder,
                new Color(0.1f, 0.1f, 0.12f, 0.95f));

            float textX = PreviewSize + Padding * 2f;
            float textWidth = CellWidth - textX - Padding;

            Sprite preview = GetThumbnail(entry);
            if (preview != null)
                UIBuilder.CreateIcon(cell, "Preview", preview,
                    new Vector2(0f, 0.5f), new Vector2(Padding, 0f), PreviewSize);

            UIBuilder.CreateText(cell, "Name", entry.Name,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(textX, -12f),
                new Vector2(-textX - Padding, 24f), TextAnchor.MiddleLeft, 18,
                Theme.text, Theme);

            string stats = entry.Kind == EquipmentKind.Weapon
                ? $"DMG {entry.Damage:0}    SWING {entry.Speed:0.0}s    LV {entry.Level}"
                : $"LV {entry.Level}";

            UIBuilder.CreateText(cell, "Stats", stats,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(textX, -40f),
                new Vector2(-textX - Padding, 20f), TextAnchor.MiddleLeft, 14,
                Theme.dimText, Theme);

            var buy = UIBuilder.CreateButton(cell, "BuyButton", "",
                new Vector2(0f, 0f), new Vector2(textX, 12f), textWidth, 32f,
                Theme, 16, () => BuyEquipment(entry));

            cells.Add(new EquipCell
            {
                Entry = entry,
                Buy = buy,
                BuyLabel = buy.GetComponentInChildren<Text>()
            });
        }

        private Sprite GetThumbnail(EquipmentEntry entry)
        {
            if (thumbnails.TryGetValue(entry.Prefab, out Sprite cached)) return cached;

            string folder = entry.Kind == EquipmentKind.Shield ? "Equipment/Shields" : "Equipment/Weapons";
            Sprite sprite = UIBuilder.LoadTextureSprite(folder + "/" + entry.Prefab.name);

            if (sprite == null && !warnedMissingThumbnail)
            {
                warnedMissingThumbnail = true;
                Debug.LogWarning("[MarketUI] Missing thumbnail for '" + entry.Prefab.name +
                    "' \u2014 run Tools > Equipment > Generate Thumbnails.");
            }

            thumbnails[entry.Prefab] = sprite;
            return sprite;
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

        private void CloseShop()
        {
            if (shop == null) return;
            shop.Close();
        }
    }
}
