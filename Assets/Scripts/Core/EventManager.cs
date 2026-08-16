using UnityEngine;
using Game.Health;

namespace Game.Core
{
    // Central place for all our C# events.
    //
    // Everything is static so any script can subscribe with just the class
    // name (EventManager.OnDamage += ...) without having to find the object.
    // Using `event` instead of a plain delegate means other scripts can only
    // add or remove themselves, they can't fire it or wipe the list.
    // Firing only happens in the Raise methods below.
    //
    // Naming pattern for each event:
    //   delegate  <Name>Handler
    //   field     On<Name>
    //   payload   <Name>Args
    //   raise     Raise<Name>
    //
    // Goes on the GameManager object in the scene.
    public class EventManager : MonoBehaviour
    {
        public delegate void DamageHandler(DamageArgs e);
        public delegate void DeathHandler(DeathArgs e);
        public delegate void HitHandler(HitArgs e);
        public delegate void BlockHandler(BlockArgs e);
        public delegate void GameStateChangedHandler(GameStateChangedArgs e);
        public delegate void WaveClearedHandler(WaveClearedArgs e);
        public delegate void GoldChangedHandler(GoldChangedArgs e);
        public delegate void ShopChangedHandler();
        public delegate void ShopTimeHandler(ShopTimeArgs e);
        public delegate void StaminaChangedHandler(StaminaChangedArgs e);
        public delegate void StunHandler(StunArgs e);
        public delegate void TauntHandler();
        public delegate void FleeHandler(FleeArgs e);

        public static event DamageHandler OnDamage;
        public static event DeathHandler OnDeath;
        public static event HitHandler OnHit;
        public static event BlockHandler OnBlock;
        public static event GameStateChangedHandler OnGameStateChanged;
        public static event WaveClearedHandler OnWaveCleared;
        public static event GoldChangedHandler OnGoldChanged;
        public static event ShopChangedHandler OnShopChanged;
        public static event ShopTimeHandler OnShopTime;
        public static event StaminaChangedHandler OnStaminaChanged;
        public static event StunHandler OnStun;
        public static event TauntHandler OnTaunt;
        public static event FleeHandler OnFlee;

        // ── Raise helpers (the only place events are actually invoked) ───────
        public static void RaiseDamage(DamageArgs e) => OnDamage?.Invoke(e);
        public static void RaiseDeath(DeathArgs e) => OnDeath?.Invoke(e);
        public static void RaiseHit(HitArgs e) => OnHit?.Invoke(e);
        public static void RaiseBlock(BlockArgs e) => OnBlock?.Invoke(e);
        public static void RaiseGameStateChanged(GameStateChangedArgs e) => OnGameStateChanged?.Invoke(e);
        public static void RaiseWaveCleared(WaveClearedArgs e) => OnWaveCleared?.Invoke(e);
        public static void RaiseGoldChanged(GoldChangedArgs e) => OnGoldChanged?.Invoke(e);
        public static void RaiseShopChanged() => OnShopChanged?.Invoke();
        public static void RaiseShopTime(ShopTimeArgs e) => OnShopTime?.Invoke(e);
        public static void RaiseStaminaChanged(StaminaChangedArgs e) => OnStaminaChanged?.Invoke(e);
        public static void RaiseStun(StunArgs e) => OnStun?.Invoke(e);
        public static void RaiseTaunt() => OnTaunt?.Invoke();
        public static void RaiseFlee(FleeArgs e) => OnFlee?.Invoke(e);

        // Clears every subscriber. Call on scene load so we don't keep
        // references to objects that no longer exist.
        public static void ClearAll()
        {
            OnDamage = null;
            OnDeath = null;
            OnHit = null;
            OnBlock = null;
            OnGameStateChanged = null;
            OnWaveCleared = null;
            OnGoldChanged = null;
            OnShopChanged = null;
            OnShopTime = null;
            OnStaminaChanged = null;
            OnStun = null;
            OnTaunt = null;
            OnFlee = null;
        }
    }
}