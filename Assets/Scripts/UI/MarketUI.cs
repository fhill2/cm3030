using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
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

        // Fixed row height. UIBuilder derives button height from width, which
        // turns a long item name into a button hundreds of pixels tall.
        private const float RowHeight = 52f;

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
        private GameObject spellsPage;
        private RectTransform canvasRect;

        private Text marketTimer;
        private Text equipmentTimer;
        private Text powerUpsTimer;
        private Text spellsTimer;

        private readonly List<ItemRow> rows = new List<ItemRow>();
        private readonly List<EquipCell> cells = new List<EquipCell>();
        private readonly Dictionary<GameObject, Sprite> thumbnails = new Dictionary<GameObject, Sprite>();
        private bool warnedMissingThumbnail;

        [Header("Debug")]
        [Tooltip("Log page wiring, shop contents and row layout so a blank page can be traced in the Console.")]
        [SerializeField] private bool logDiagnostics = true;

        private float secondsLeft;
        private Equipment playerEquipment;

        // Both catalogue pages share one row width so they line up and a
        // refresh can't change the layout underneath them.
        private float rowWidth;

        private UITheme Theme => theme != null ? theme : UITheme.Default;

        private static readonly Color CannotAffordColor = new Color(0.85f, 0.3f, 0.25f, 1f);

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

            Diag($"Awake: shop={shop != null} wallet={wallet != null} stateMachine={stateMachine != null}" +
                $" shopItemCount={(shop != null ? shop.ItemCount : -1)}");
        }

        private void Diag(string message)
        {
            if (logDiagnostics) Debug.Log("[MarketUI] " + message);
        }

        [ContextMenu("Dump Diagnostics")]
        private void DumpDiagnostics()
        {
            Diag($"pages: market={marketPage != null} equipment={equipmentPage != null}" +
                $" powerUps={powerUpsPage != null} spells={spellsPage != null} active={spellsPage != null && spellsPage.activeSelf}");
            Diag($"shop={shop != null} itemCount={(shop != null ? shop.ItemCount : -1)} rows={rows.Count}");

            if (shop != null)
            {
                for (int i = 0; i < shop.ItemCount; i++)
                {
                    ShopItemDef item = shop.ItemAt(i);
                    Diag($"item[{i}]: {(item == null ? "NULL" : item.DisplayName + " effect=" + item.Effect)}");
                }
            }

            foreach (ItemRow row in rows)
            {
                var rt = row.Buy.transform as RectTransform;
                Diag($"row[{row.Index}] '{row.BuyLabel.text}' parent={rt.parent.name}" +
                    $" pos={rt.anchoredPosition} size={rt.sizeDelta} active={row.Buy.gameObject.activeInHierarchy}");
            }
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
            if (powerUpsTimer != null) powerUpsTimer.text = line;
            if (spellsTimer != null) spellsTimer.text = line;
        }

        // Only the text and whether it's clickable change on refresh. Sizes are
        // fixed at build time so the page can't reflow while it's open.
        private void RefreshRows()
        {
            if (shop == null) return;

            foreach (ItemRow row in rows)
            {
                row.BuyLabel.text = ItemRowLabel(row.Index);
                bool affordable = shop.CanAfford(row.Index);
                row.Buy.interactable = affordable;
                row.BuyLabel.color = affordable ? Theme.text : CannotAffordColor;
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
                bool affordable = wallet != null && wallet.Gold >= cell.Entry.Cost;
                cell.Buy.interactable = !equipped && affordable;
                cell.BuyLabel.color = !equipped && !affordable ? CannotAffordColor : Theme.text;
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

        private void ShowMarketPage()   { SetPage(marketPage); }
        private void ShowEquipmentPage() { SetPage(equipmentPage); }
        private void ShowPowerUpsPage() { SetPage(powerUpsPage); }
        private void ShowSpellsPage()   { SetPage(spellsPage); }

        // One place that turns pages on and off, so adding a page doesn't mean
        // remembering to hide it in three other methods.
        private void SetPage(GameObject page)
        {
            ClearPageHover(marketPage);
            ClearPageHover(equipmentPage);
            ClearPageHover(powerUpsPage);
            ClearPageHover(spellsPage);
            ClearSelection();

            if (marketPage != null) marketPage.SetActive(page == marketPage);
            if (equipmentPage != null) equipmentPage.SetActive(page == equipmentPage);
            if (powerUpsPage != null) powerUpsPage.SetActive(page == powerUpsPage);
            if (spellsPage != null) spellsPage.SetActive(page == spellsPage);
        }

        private static void ClearSelection()
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        }

        // A button hidden mid-hover keeps its highlight when it comes back, so
        // send it a pointer-exit on the way out.
        private static void ClearPageHover(GameObject page)
        {
            if (page == null || !page.activeSelf) return;
            var es = EventSystem.current;
            if (es == null) return;
            var ped = new PointerEventData(es);
            foreach (var sel in page.GetComponentsInChildren<Selectable>())
                ExecuteEvents.Execute<IPointerExitHandler>(sel.gameObject, ped,
                    (h, e) => h.OnPointerExit((PointerEventData)e));
        }

        private void BuildUI()
        {
            UIBuilder.EnsureEventSystem();

            root = UIBuilder.CreateOverlayCanvas("MarketUICanvas", sortingOrder: 50).gameObject;
            canvasRect = root.GetComponent<RectTransform>();

            var backdropRt = UIBuilder.CreateStretchChild(root.transform, "Backdrop");
            var backdrop = UIBuilder.AttachImage(backdropRt, Theme.backdropDim);
            backdrop.raycastTarget = true;

            rowWidth = MeasureRowWidth();

            marketPage    = BuildSafely("MarketPage",    BuildMarketPage);
            equipmentPage = BuildSafely("EquipmentPage", BuildEquipmentPage);
            powerUpsPage  = BuildSafely("PowerUpsPage",  () => BuildItemListPage("PowerUpsPanel", "UPGRADES", false));
            spellsPage    = BuildSafely("SpellsPage",    () => BuildItemListPage("SpellsPanel", "SPELLS", true));

            root.SetActive(false);
        }

        // One page failing shouldn't take the whole market down with it.
        private GameObject BuildSafely(string pageName, System.Func<GameObject> build)
        {
            try
            {
                GameObject page = build();
                Diag($"built {pageName}: {(page != null ? page.name : "null")}");
                return page;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MarketUI] {pageName} threw during build: {e}");
                return null;
            }
        }

        // Widest label across the whole catalogue, so both pages use one width
        // and every row lines up.
        private float MeasureRowWidth()
        {
            var labels = new List<string> { "NOTHING HERE YET" };

            int itemCount = shop != null ? shop.ItemCount : 0;
            for (int i = 0; i < itemCount; i++)
            {
                ShopItemDef item = shop.ItemAt(i);
                if (item == null) continue;

                // The longest form the label can take, not its current one, or
                // the row shrinks the moment a spell unlocks.
                labels.Add($"{item.DisplayName} \u2014 tome not found");
                labels.Add($"{item.DisplayName} \u2014 {item.CostAfter(0)}g");
            }

            return UIBuilder.MeasureButtonWidth(labels.ToArray(), ButtonPadding, 18, Theme);
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

            float navWidth = UIBuilder.MeasureButtonWidth(
                new[] { "EQUIPMENT", "UPGRADES", "SPELLS", "DONE" },
                ButtonPadding, 18, Theme);
            navWidth = Mathf.Min(navWidth, innerW / 3f - Padding);
            float navHeight = navWidth / UIBuilder.ButtonAspect;

            float headerHeight = navHeight * 0.6f;
            float gap = Mathf.Max(4f,
                (inner - headerHeight - navHeight * 2f) * 0.25f);

            float headerCenterY = inner * 0.5f - gap - headerHeight * 0.5f;
            float navCenterY = headerCenterY - headerHeight * 0.5f - gap - navHeight * 0.5f;
            float doneCenterY = navCenterY - navHeight * 0.5f - gap - navHeight * 0.5f;

            UIBuilder.CreateText(content, "Title", "MARKET",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-innerW * 0.25f + Padding * 1.5f, headerCenterY),
                new Vector2(innerW * 0.5f, headerHeight), TextAnchor.MiddleLeft, 32, Theme.text, Theme);

            marketTimer = UIBuilder.CreateText(content, "Timer", "",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(innerW * 0.25f, headerCenterY),
                new Vector2(innerW * 0.5f, headerHeight), TextAnchor.MiddleRight, 16, Theme.dimText, Theme);

            float navGap = (innerW - navWidth * 3f) * 0.25f;
            float navX = innerW * 0.5f - navGap - navWidth * 0.5f;

            UIBuilder.CreateButton(content, "EquipmentButton", "EQUIPMENT",
                new Vector2(-navX, navCenterY), navWidth,
                Theme, 18, ShowEquipmentPage);

            UIBuilder.CreateButton(content, "PowerUpsButton", "UPGRADES",
                new Vector2(0f, navCenterY), navWidth,
                Theme, 18, ShowPowerUpsPage);

            UIBuilder.CreateButton(content, "SpellsButton", "SPELLS",
                new Vector2(navX, navCenterY), navWidth,
                Theme, 18, ShowSpellsPage);

            UIBuilder.CreateButton(content, "DoneButton", "DONE",
                new Vector2(0f, doneCenterY), navWidth,
                Theme, 18, CloseShop);

            return panel.gameObject;
        }

        // Builds one of the two catalogue pages. They differ only in title and
        // which items they take: spells on one, everything else on the other.
        // The panel is sized from its own row count, so it fits exactly what it
        // holds rather than overflowing once the catalogue grows.
        private GameObject BuildItemListPage(string name, string title, bool spells)
        {
            var indices = new List<int>();
            int itemCount = shop != null ? shop.ItemCount : 0;
            for (int i = 0; i < itemCount; i++)
            {
                if (shop.IsSpell(i) == spells) indices.Add(i);
            }

            int lines = Mathf.Max(1, indices.Count);
            float innerHeight = HeaderHeight
                                + lines * (RowHeight + RowGap)
                                + RowGap + RowHeight + Padding;

            float pageHeight = innerHeight + PageInset * 2f;
            float pageWidth = rowWidth + (PageInset + Padding) * 2f;

            var panel = UIBuilder.CreatePanel(root.transform, name,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(pageWidth, pageHeight),
                Theme, panelBorder, Color.clear);
            var content = UIBuilder.CreateContentRect(panel, PageInset);

            UIBuilder.CreateText(content, "Title", title,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -8f),
                new Vector2(0f, 40f), TextAnchor.MiddleCenter, 24, Theme.text, Theme);

            Text timer = UIBuilder.CreateText(content, "Timer", "",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -48f),
                new Vector2(0f, 24f), TextAnchor.MiddleCenter, 16, Theme.text, Theme);
            if (spells) spellsTimer = timer; else powerUpsTimer = timer;

            float cursor = innerHeight * 0.5f - HeaderHeight;

            if (indices.Count == 0)
            {
                UIBuilder.CreateText(content, "Empty", "NOTHING HERE YET",
                    new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, cursor - RowHeight * 0.5f),
                    new Vector2(rowWidth, RowHeight), TextAnchor.MiddleCenter,
                    18, Theme.dimText, Theme);
                cursor -= RowHeight + RowGap;
            }
            else
            {
                foreach (int i in indices)
                {
                    cursor -= RowHeight * 0.5f;
                    CreateItemRow(content, i, new Vector2(0f, cursor));
                    cursor -= RowHeight * 0.5f + RowGap;
                }
            }

            var back = UIBuilder.CreateButton(content, "BackButton", "BACK",
                new Vector2(0f, cursor - RowGap - RowHeight * 0.5f),
                rowWidth * 0.4f, Theme, 16, ShowMarketPage);
            SetRowSize(back, rowWidth * 0.4f, RowHeight);

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private string ItemRowLabel(int index)
        {
            ShopItemDef item = shop != null ? shop.ItemAt(index) : null;
            if (item == null) return "";

            if (!shop.IsAvailable(index))
            {
                // A spell whose tome hasn't dropped isn't sold out, the player
                // just hasn't found it yet.
                return shop.IsSpell(index)
                    ? $"{item.DisplayName} \u2014 tome not found"
                    : $"{item.DisplayName} \u2014 sold out";
            }

            return $"{item.DisplayName} \u2014 {shop.CostOf(index)}g";
        }

        private void CreateItemRow(RectTransform parent, int index, Vector2 center)
        {
            ShopItemDef item = shop.ItemAt(index);
            if (item == null) return;

            var buy = UIBuilder.CreateButton(parent, "BuyButton", ItemRowLabel(index),
                center, rowWidth, Theme, 18, () => BuyItem(index));

            SetRowSize(buy, rowWidth, RowHeight);

            rows.Add(new ItemRow
            {
                Index = index,
                Buy = buy,
                BuyLabel = buy.GetComponentInChildren<Text>()
            });
        }

        // UIBuilder ties button height to width, so both are set directly here.
        private static void SetRowSize(Button button, float width, float height)
        {
            if (button == null) return;

            var rt = (RectTransform)button.transform;
            rt.sizeDelta = new Vector2(width, height);
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

            // Cheapest tier first, then alphabetical, so the grid reads in a
            // predictable order.
            var sorted = new System.Collections.Generic.List<EquipmentEntry>(EquipmentCatalog.Entries);
            sorted.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : string.Compare(a.Name, b.Name, System.StringComparison.OrdinalIgnoreCase));
            IReadOnlyList<EquipmentEntry> entries = sorted;
            int rowCount = Mathf.Max(1, Mathf.CeilToInt(entries.Count / (float)Columns));
            float contentHeight = rowCount * (CellHeight + CellGap) + Padding * 2f;

            var cellLabels = new List<string> { "EQUIPPED" };
            foreach (EquipmentEntry entry in entries)
                cellLabels.Add($"BUY \u2014 {entry.Cost}g");
            float cellButtonWidth = UIBuilder.MeasureButtonWidth(cellLabels.ToArray(), ButtonPadding, 16, Theme);
            float cellButtonHeight = cellButtonWidth / UIBuilder.ButtonAspect;

            Vector2 gridSize = new Vector2(innerWidth - Padding * 2f - 16f, innerHeight - HeaderHeight - Padding);
            ScrollRect gridScroll = UIBuilder.CreateScrollList(pageContent, "Grid",
                new Vector2(0.5f, 1f), new Vector2(0f, -HeaderHeight),
                gridSize, Theme, out RectTransform gridContent);
            gridContent.sizeDelta = new Vector2(0f, contentHeight);
            Scrollbar gridBar = UIBuilder.CreateScrollbar(gridScroll.transform, gridScroll, gridSize.y, Theme);
            gridBar.gameObject.SetActive(contentHeight > gridSize.y);

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

        // Cached, since a miss also logs and the grid rebuilds on every open.
        private Sprite GetThumbnail(EquipmentEntry entry)
        {
            if (thumbnails.TryGetValue(entry.Prefab, out Sprite cached)) return cached;

            string folder = entry.Kind == EquipmentKind.Shield ? "Equipment/Shields" : "Equipment/Weapons";
            Sprite sprite = UIBuilder.LoadTextureSprite(folder + "/" + entry.Prefab.name);

            if (sprite == null && !warnedMissingThumbnail)
            {
                warnedMissingThumbnail = true;
                Debug.LogWarning("[MarketUI] Missing thumbnail for '" + entry.Prefab.name +
                    "'. Run Tools > Equipment > Generate Thumbnails.");
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