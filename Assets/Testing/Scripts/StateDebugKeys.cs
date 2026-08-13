using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    // Temporary. Drives the game loop from the keyboard while there's no
    // menu or shop UI. Delete once the real triggers exist.
    //
    // 1  next wave        4  buy shop item 0
    // 2  open shop        5  close shop (the Done button)
    // 3  back to menu     6  print gold and shop prices
    //
    // Goes on the GameManager object.
    public class StateDebugKeys : MonoBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;
        [SerializeField] private Shop shop;
        [SerializeField] private PlayerWallet wallet;

        private void Update()
        {
            if (Keyboard.current == null) return;

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
                stateMachine.MoveToState(GameStateId.WaveActive);

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
                stateMachine.MoveToState(GameStateId.Shop);

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
                stateMachine.MoveToState(GameStateId.Menu);

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
                Buy(0);

            if (Keyboard.current.digit5Key.wasPressedThisFrame)
                shop.Close();

            if (Keyboard.current.digit6Key.wasPressedThisFrame)
                PrintShop();
        }

        private void Buy(int index)
        {
            if (shop == null) return;

            if (!shop.TryBuy(index))
            {
                Debug.Log($"[Debug] Can't buy item {index} — too expensive or unavailable.");
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

                Debug.Log($"[Debug] {i}: {item.DisplayName} — {shop.CostOf(i)} gold, bought {shop.TimesBought(i)}x");
            }
        }
    }
}