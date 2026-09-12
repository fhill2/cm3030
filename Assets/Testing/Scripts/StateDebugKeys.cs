using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    // Drives the game loop from the keyboard for testing.
    //
    // F1  next wave        F4  buy shop item 0
    // F2  open shop        F5  close shop
    // F3  back to menu     F6  print gold and shop prices
    //
    // Goes on the GameManager.
    public class StateDebugKeys : MonoBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;
        [SerializeField] private Shop shop;
        [SerializeField] private PlayerWallet wallet;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame)
                stateMachine.MoveToState(GameStateId.WaveActive);

            if (keyboard.f2Key.wasPressedThisFrame)
                stateMachine.MoveToState(GameStateId.Shop);

            if (keyboard.f3Key.wasPressedThisFrame)
                stateMachine.MoveToState(GameStateId.Menu);

            if (keyboard.f4Key.wasPressedThisFrame)
                Buy(0);

            if (keyboard.f5Key.wasPressedThisFrame)
                shop.Close();

            if (keyboard.f6Key.wasPressedThisFrame)
                PrintShop();
        }

        private void Buy(int index)
        {
            if (shop == null) return;

            if (!shop.TryBuy(index))
            {
                Debug.Log($"[Debug] Can't buy item {index}, too expensive or unavailable.");
            }
        }

        private void PrintShop()
        {
            if (shop == null || wallet == null) return;

            Debug.Log($"[Debug] Gold: {wallet.Gold}");

            for (int i = 0; i < shop.ItemCount; i++)
            {
                ShopItemDef item = shop.ItemAt(i);
                if (item == null) continue;

                Debug.Log($"[Debug] {i}: {item.DisplayName}, {shop.CostOf(i)} gold, bought {shop.TimesBought(i)}x");
            }
        }
    }
}