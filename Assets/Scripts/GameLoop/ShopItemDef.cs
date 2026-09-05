using UnityEngine;

namespace Game.Core
{
    // What an item actually does when bought.
    public enum ShopEffect
    {
        WeaponDamage,   // adds Magnitude to the weapon's damage
        WeaponSpeed,    // subtracts Magnitude from swing duration, so lower is faster
        Tome,           // teaches the spell in the Spell field
        WeaponSwap,     // placeholder
        ShieldSwap,     // placeholder
        HealthPotion,   // adds one health potion to the player's belt
        StaminaPotion   // adds one stamina potion to the player's belt
    }

    // One purchasable item. Make these as assets:
    // Create > Fall of Camelot > Shop Item
    [CreateAssetMenu(fileName = "ShopItem", menuName = "Fall of Camelot/Shop Item")]
    public class ShopItemDef : ScriptableObject
    {
        [Header("Display")]
        [SerializeField] private string displayName = "New Item";
        [SerializeField] private string description = "";

        [Header("Cost")]
        [SerializeField] private int baseCost = 50;
        [Tooltip("Cost is multiplied by this for every purchase already made. 1 means the price never rises.")]
        [SerializeField] private float costGrowth = 1.5f;

        [Header("Effect")]
        [SerializeField] private ShopEffect effect = ShopEffect.WeaponDamage;
        [Tooltip("How much the effect applies per purchase.")]
        [SerializeField] private float magnitude = 5f;
        [Tooltip("How many times this can be bought in a run. 0 means unlimited.")]
        [SerializeField] private int maxPurchases = 0;
        [Tooltip("Only used when Effect is Tome. The spell this item teaches.")]
        [SerializeField] private SpellDef spell;

        public string DisplayName => displayName;
        public string Description => description;
        public ShopEffect Effect => effect;
        public float Magnitude => magnitude;
        public int MaxPurchases => maxPurchases;
        public SpellDef Spell => spell;

        // Price rises each time it's bought, so the first upgrade is cheap
        // and later ones cost real gold.
        public int CostAfter(int timesBought)
        {
            return Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, timesBought));
        }
    }
}