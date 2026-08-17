using System.Collections.Generic;
using UnityEngine;
using Game.Combat;

namespace Game.Core
{
    // Holds the shop catalogue and handles purchases.
    //
    // Knows nothing about UI. Whatever draws the panel asks for ItemCount,
    // ItemAt and CostOf to build the list, calls TryBuy when a button is
    // pressed, and listens to OnShopChanged to refresh. The panel itself is
    // shown and hidden by watching OnGameStateChanged for the Shop state.
    //
    // Goes on the GameManager object.
    public class Shop : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerWallet wallet;
        [SerializeField] private PlayerLoadout loadout;
        [SerializeField] private GameStateMachine stateMachine;

        [Header("Catalogue")]
        [SerializeField] private ShopItemDef[] items;

        // How many times each item has been bought this run, by index.
        private readonly Dictionary<int, int> purchaseCounts = new Dictionary<int, int>();

        public int ItemCount => items != null ? items.Length : 0;

        public ShopItemDef ItemAt(int index)
        {
            if (items == null || index < 0 || index >= items.Length) return null;
            return items[index];
        }

        // Current price of an item, accounting for how often it's been bought.
        public int CostOf(int index)
        {
            ShopItemDef item = ItemAt(index);
            if (item == null) return 0;

            return item.CostAfter(TimesBought(index));
        }

        public int TimesBought(int index)
        {
            return purchaseCounts.TryGetValue(index, out int count) ? count : 0;
        }

        // Whether the item can still be bought at all, ignoring gold.
        public bool IsAvailable(int index)
        {
            ShopItemDef item = ItemAt(index);
            if (item == null) return false;
            if (item.MaxPurchases <= 0) return true;

            return TimesBought(index) < item.MaxPurchases;
        }

        // True if the player can afford it right now and it's still available.
        public bool CanAfford(int index)
        {
            if (!IsAvailable(index)) return false;
            if (wallet == null) return false;

            return wallet.Gold >= CostOf(index);
        }

        // The one entry point for buying. Returns false and changes nothing
        // if the item is unavailable or the player is short.
        public bool TryBuy(int index)
        {
            ShopItemDef item = ItemAt(index);
            if (item == null) return false;
            if (!IsAvailable(index)) return false;
            if (wallet == null) return false;

            int cost = CostOf(index);
            if (!wallet.TrySpend(cost)) return false;

            purchaseCounts[index] = TimesBought(index) + 1;
            ApplyEffect(item);

            Debug.Log($"[Shop] Bought {item.DisplayName} for {cost}");
            EventManager.RaiseShopChanged();
            return true;
        }

        private void ApplyEffect(ShopItemDef item)
        {
            if (loadout == null)
            {
                Debug.LogWarning("[Shop] No loadout assigned, effect not applied.");
                return;
            }

            switch (item.Effect)
            {
                case ShopEffect.WeaponDamage:
                    loadout.AddWeaponDamage(item.Magnitude);
                    break;

                case ShopEffect.WeaponSpeed:
                    loadout.AddWeaponSpeed(item.Magnitude);
                    break;

                // Not built yet. The item can exist in the catalogue and be
                // priced, it just does nothing until the system lands.
                case ShopEffect.Tome:
                case ShopEffect.WeaponSwap:
                case ShopEffect.ShieldSwap:
                    Debug.Log($"[Shop] {item.Effect} is not implemented yet.");
                    break;
            }
        }

        public bool TryBuyEquipment(EquipmentEntry entry)
        {
            if (wallet == null || !wallet.TrySpend(entry.Cost)) return false;

            Equipment equipment = loadout != null ? loadout.PlayerEquipment : null;
            if (equipment == null)
            {
                wallet.Add(entry.Cost);
                return false;
            }

            if (entry.Kind == EquipmentKind.Shield) equipment.EquipShield(entry.Prefab);
            else equipment.EquipWeapon(entry.Prefab);

            Debug.Log($"[Shop] Equipped {entry.Name} for {entry.Cost}g");
            return true;
        }

        // Called by the Done button. Ends the shop early and starts the wave.
        public void Close()
        {
            if (stateMachine == null) return;
            if (stateMachine.CurrentState == null) return;
            if (stateMachine.CurrentState.Id != GameStateId.Shop) return;

            stateMachine.MoveToState(GameStateId.WaveActive);
        }

        // Wipes purchase history for a new run. Upgrades themselves are on
        // the runtime weapon copy, which dies with play mode anyway.
        public void ResetForNewRun()
        {
            purchaseCounts.Clear();
            EventManager.RaiseShopChanged();
        }
    }
}