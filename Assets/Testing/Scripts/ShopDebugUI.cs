using UnityEngine;

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

        private float secondsLeft;

        private void OnEnable()
        {
            EventManager.OnShopTime += HandleShopTime;
        }

        private void OnDisable()
        {
            EventManager.OnShopTime -= HandleShopTime;
        }

        private void HandleShopTime(ShopTimeArgs e)
        {
            secondsLeft = e.SecondsLeft;
        }

        private void OnGUI()
        {
            if (stateMachine == null || wallet == null) return;

            DrawStatusBar();

            if (stateMachine.CurrentState == null) return;
            if (stateMachine.CurrentState.Id == GameStateId.Shop) DrawShopPanel();
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
            int height = 130 + rows * rowHeight;

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

            if (GUI.Button(new Rect(x + 15, rowY + 8, panelWidth - 30, 34), "DONE"))
            {
                shop.Close();
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

            // Greyed out when it can't be bought, so it's obvious why nothing
            // happens on click.
            GUI.enabled = affordable;
            if (GUI.Button(new Rect(x, y + 18, width, 30), label))
            {
                shop.TryBuy(index);
            }
            GUI.enabled = true;
        }
    }
}