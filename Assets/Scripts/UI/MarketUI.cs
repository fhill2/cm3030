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
            spriteName = "market_ui_panel_border_3"
        };

        [Tooltip("Market panel height as a fraction of the canvas' shorter side. The panel renders at a 16:9 aspect ratio.")]
        [SerializeField, Range(0.05f, 1f)] private float panelSizePercent = 0.25f;

        private const float PanelAspect = 16f / 9f;

        [Tooltip("Inset of the content area from the panel edge as a fraction of panel size. Keeps content clear of the border art.")]
        [SerializeField, Range(0f, 0.3f)] private float contentInsetPercent = 0.15f;

        private const float HeaderHeight = 96f;
        private const float RowGap       = 14f;
        private const float Padding      = 20f;
        private const float ButtonPadding = 28f;
        private const float PageInset    = 64f;

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
        private GameObject powerUpsPage;
        private RectTransform canvasRect;

        private Text marketTimer;
        private Text equipmentTimer;

        private readonly List<ItemRow> rows = new List<ItemRow>();
        private readonly List<EquipCell> cells = new List<EquipCell>();
        private readonly Dictionary<GameObject, Sprite> thumbnails = new Dictionary<GameObject, Sprite>();
        private bool warnedMissingThumbnail;

        private float secondsLeft;
        private Equipment playerEquipment;

        private UITheme Theme => theme != null ? theme : UITheme.Default;

        private float PanelSize
        {
            get
            {
                float shortSide = canvasRect != null
                    ? Mathf.Min(canvasRect.rect.width, canvasRect.rect.height)
                    : 1080f;
                return shortSide * panelSizePercent;
            }
        }

        private void Awake()
        {
            if (shop == null) shop = FindFirstObjectByType<Shop>();
            if (wallet == null) wallet = FindFirstObjectByType<PlayerWallet>();
            if (stateMachine == null) stateMachine = FindFirstObjectByType<GameStateMachine>();
        }

        private void Start()
        {
            BuildUI();
            SyncWithCurrentState();
        }

        private void SyncWithCurrentState()
        {
            if (root == null || stateMachine == null || stateMachine.CurrentState == null) return;

            if (stateMachine.CurrentState.Id == GameStateId.Shop)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                root.SetActive(true);
                ShowMarketPage();
                RefreshAll();
            }
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
                row.BuyLabel.text = ItemRowLabel(row.Index);
                row.Buy.interactable = shop.CanAfford(row.Index);
            }

            FitRowsUniform();
        }

        private void FitRowsUniform()
        {
            float widest = 0f;
            foreach (ItemRow row in rows)
                widest = Mathf.Max(widest, row.BuyLabel.preferredWidth);
            widest += ButtonPadding * 2f;

            foreach (ItemRow row in rows)
                UIBuilder.SetButtonSize(row.Buy, widest);
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

            FitCellsUniform();
        }

        private void FitCellsUniform()
        {
            float widest = 0f;
            foreach (EquipCell cell in cells)
                widest = Mathf.Max(widest, cell.BuyLabel.preferredWidth);
            widest += ButtonPadding * 2f;

            foreach (EquipCell cell in cells)
                UIBuilder.SetButtonSize(cell.Buy, widest);
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
            if (powerUpsPage != null) powerUpsPage.SetActive(false);
        }

        private void ShowEquipmentPage()
        {
            if (marketPage != null) marketPage.SetActive(false);
            if (equipmentPage != null) equipmentPage.SetActive(true);
            if (powerUpsPage != null) powerUpsPage.SetActive(false);
        }

        private void ShowPowerUpsPage()
        {
            if (marketPage != null) marketPage.SetActive(false);
            if (equipmentPage != null) equipmentPage.SetActive(false);
            if (powerUpsPage != null) powerUpsPage.SetActive(true);
        }

        private void BuildUI()
        {
            UIBuilder.EnsureEventSystem();

            root = UIBuilder.CreateOverlayCanvas("MarketUICanvas", sortingOrder: 50).gameObject;
            canvasRect = root.GetComponent<RectTransform>();

            var backdropRt = UIBuilder.CreateStretchChild(root.transform, "Backdrop");
            var backdrop = UIBuilder.AttachImage(backdropRt, Theme.backdropDim);
            backdrop.raycastTarget = true;

            marketPage = BuildMarketPage();
            equipmentPage = BuildEquipmentPage();
            powerUpsPage = BuildPowerUpsPage();

            root.SetActive(false);
        }

        private GameObject BuildMarketPage()
        {
            float height = PanelSize;
            float width = height * PanelAspect;
            float inset = height * contentInsetPercent;

            var panel = UIBuilder.CreatePanel(root.transform, "MarketPanel",
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(width, height),
                Theme, panelBorder, Color.clear);
            var content = UIBuilder.CreateContentRect(panel, inset);

            float inner = height - inset * 2f;
            float innerW = width - inset * 2f;
            float quarter = inner * 0.25f;

            UIBuilder.CreateText(content, "Title", "MARKET",
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -quarter * 0.25f),
                new Vector2(innerW * 0.5f, quarter * 0.5f), TextAnchor.MiddleLeft, 24, Theme.text, Theme);

            marketTimer = UIBuilder.CreateText(content, "Timer", "",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -quarter * 0.25f),
                new Vector2(innerW * 0.5f, quarter * 0.5f), TextAnchor.MiddleRight, 16, Theme.dimText, Theme);

            float navWidth = UIBuilder.MeasureButtonWidth(
                new[] { "BUY", "RECHARGE", "DONE" },
                ButtonPadding, 18, Theme);
            navWidth = Mathf.Min(navWidth, innerW / 3f - Padding);
            float navHeight = navWidth / UIBuilder.ButtonAspect;

            float doneCenterY = -inner * 0.5f + Padding + navHeight * 0.5f;
            float headerBottom = inner * 0.5f - quarter * 0.6f;
            float navCenterY = (headerBottom + doneCenterY + navHeight * 0.5f) * 0.5f;

            float gap = (innerW - navWidth * 3f) * 0.25f;
            float navX = innerW * 0.5f - gap - navWidth * 0.5f;

            UIBuilder.CreateButton(content, "EquipmentButton", "BUY",
                new Vector2(-navX, navCenterY), navWidth,
                Theme, 18, ShowEquipmentPage);

            UIBuilder.CreateButton(content, "PowerUpsButton", "RECHARGE",
                new Vector2(0f, navCenterY), navWidth,
                Theme, 18, ShowPowerUpsPage);

            UIBuilder.CreateButton(content, "SpellsButton", "BUY",
                new Vector2(navX, navCenterY), navWidth,
                Theme, 18);

            UIBuilder.CreateButton(content, "DoneButton", "DONE",
                new Vector2(0f, doneCenterY), navWidth,
                Theme, 18, CloseShop);

            return panel.gameObject;
        }

        private GameObject BuildPowerUpsPage()
        {
            float pageWidth = EquipPageWidth * 0.5f;
            float pageHeight = EquipPageHeight * 0.5f;

            var panel = UIBuilder.CreatePanel(root.transform, "PowerUpsPanel",
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(pageWidth, pageHeight),
                Theme, panelBorder, Color.clear);
            var content = UIBuilder.CreateContentRect(panel, PageInset);

            UIBuilder.CreateText(content, "Title", "POWERUPS",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f),
                new Vector2(0f, 40f), TextAnchor.MiddleCenter, 24, Theme.text, Theme);

            float innerH = pageHeight - PageInset * 2f;

            int itemCount = shop != null ? shop.ItemCount : 0;

            var rowLabels = new System.Collections.Generic.List<string>();
            for (int i = 0; i < itemCount; i++)
                rowLabels.Add(ItemRowLabel(i));
            float rowWidth = UIBuilder.MeasureButtonWidth(rowLabels.ToArray(), ButtonPadding, 18, Theme);
            float rowHeight = rowWidth / UIBuilder.ButtonAspect;

            float cursor = innerH * 0.5f - HeaderHeight;
            for (int i = 0; i < itemCount; i++)
            {
                cursor -= rowHeight * 0.5f;
                CreateItemRow(content, i, new Vector2(0f, cursor), rowWidth);
                cursor -= rowHeight * 0.5f + RowGap;
            }

            float backWidth = UIBuilder.MeasureButtonWidth(
                new[] { "BACK" }, ButtonPadding, 16, Theme);

            UIBuilder.CreateButton(content, "BackButton", "BACK",
                new Vector2(0f, -innerH * 0.5f + Padding + backWidth / UIBuilder.ButtonAspect * 0.5f),
                backWidth, Theme, 16, ShowMarketPage);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private string ItemRowLabel(int index)
        {
            ShopItemDef item = shop != null ? shop.ItemAt(index) : null;
            if (item == null) return "";

            int cost = shop.CostOf(index);
            return shop.IsAvailable(index)
                ? $"{item.DisplayName} \u2014 {cost}g"
                : $"{item.DisplayName} \u2014 sold out";
        }

        private void CreateItemRow(RectTransform parent, int index, Vector2 center, float width)
        {
            ShopItemDef item = shop.ItemAt(index);
            if (item == null) return;

            var buy = UIBuilder.CreateButton(parent, "BuyButton", "",
                center, width,
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
            var panel = UIBuilder.CreatePanel(root.transform, "EquipmentPanel",
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(EquipPageWidth, EquipPageHeight),
                Theme, panelBorder, Color.clear);
            var pageContent = UIBuilder.CreateContentRect(panel, PageInset);
            float innerWidth = EquipPageWidth - PageInset * 2f;
            float innerHeight = EquipPageHeight - PageInset * 2f;

            UIBuilder.CreateText(pageContent, "Title", "EQUIPMENT",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f),
                new Vector2(0f, 40f), TextAnchor.MiddleCenter, 24, Theme.text, Theme);

            equipmentTimer = UIBuilder.CreateText(pageContent, "Timer", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f),
                new Vector2(0f, 24f), TextAnchor.MiddleCenter, 16, Theme.text, Theme);

            float backWidth = UIBuilder.MeasureButtonWidth(
                new[] { "BACK" }, ButtonPadding, 16, Theme);

            var back = UIBuilder.CreateButton(pageContent, "BackButton", "BACK",
                new Vector2(0f, innerHeight * 0.5f - backWidth / UIBuilder.ButtonAspect * 0.5f - Padding),
                backWidth, Theme, 16, ShowMarketPage);
            var backRt = back.GetComponent<RectTransform>();
            backRt.anchoredPosition = new Vector2(
                innerWidth * 0.5f - backRt.rect.width * 0.5f - Padding,
                backRt.anchoredPosition.y);

            IReadOnlyList<EquipmentEntry> entries = EquipmentCatalog.Entries;
            int rowCount = Mathf.Max(1, Mathf.CeilToInt(entries.Count / (float)Columns));
            float contentHeight = rowCount * (CellHeight + CellGap) + Padding * 2f;

            var cellLabels = new System.Collections.Generic.List<string> { "EQUIPPED" };
            foreach (EquipmentEntry entry in entries)
                cellLabels.Add($"BUY \u2014 {entry.Cost}g");
            float cellButtonWidth = UIBuilder.MeasureButtonWidth(cellLabels.ToArray(), ButtonPadding, 16, Theme);
            float cellButtonHeight = cellButtonWidth / UIBuilder.ButtonAspect;

            UIBuilder.CreateScrollList(pageContent, "Grid",
                new Vector2(0.5f, 1f), new Vector2(0f, -HeaderHeight),
                new Vector2(innerWidth - Padding * 2f, innerHeight - HeaderHeight - Padding),
                Theme, out RectTransform gridContent);
            gridContent.sizeDelta = new Vector2(0f, contentHeight);

            for (int i = 0; i < entries.Count; i++)
                CreateEquipmentCell(gridContent, entries[i], i, cellButtonWidth, cellButtonHeight);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private void CreateEquipmentCell(RectTransform content, EquipmentEntry entry, int index,
            float buttonWidth, float buttonHeight)
        {
            int column = index % Columns;
            int row = index / Columns;

            float innerWidth = EquipPageWidth - PageInset * 2f - Padding * 2f;
            float gridWidth = Columns * CellWidth + (Columns - 1) * CellGap;
            float x0 = (innerWidth - gridWidth) * 0.5f;

            var cell = UIBuilder.CreatePanel(content, "Cell_" + entry.Name,
                new Vector2(0f, 1f),
                new Vector2(x0 + column * (CellWidth + CellGap),
                            -(Padding + row * (CellHeight + CellGap))),
                new Vector2(CellWidth, CellHeight), Theme, null, Color.clear);

            float textX = PreviewSize + Padding * 2f;

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
                new Vector2(0f, -CellHeight * 0.5f + buttonHeight * 0.5f + Padding), buttonWidth,
                Theme, 16, () => BuyEquipment(entry));
            var buyRt = buy.GetComponent<RectTransform>();
            buyRt.anchoredPosition = new Vector2(
                -CellWidth * 0.5f + textX + buyRt.rect.width * 0.5f,
                buyRt.anchoredPosition.y);

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
