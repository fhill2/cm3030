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
        public delegate void GameStateChangedHandler(GameStateChangedArgs e);

        public static event DamageHandler OnDamage;
        public static event DeathHandler OnDeath;
        public static event HitHandler OnHit;
        public static event GameStateChangedHandler OnGameStateChanged;

        // ── Raise helpers (the only place events are actually invoked) ───────
        public static void RaiseDamage(DamageArgs e) => OnDamage?.Invoke(e);
        public static void RaiseDeath(DeathArgs e) => OnDeath?.Invoke(e);
        public static void RaiseHit(HitArgs e) => OnHit?.Invoke(e);
        public static void RaiseGameStateChanged(GameStateChangedArgs e) => OnGameStateChanged?.Invoke(e);

        // Clears every subscriber. Call on scene load so we don't keep
        // references to objects that no longer exist.
        public static void ClearAll()
        {
            OnDamage = null;
            OnDeath = null;
            OnHit = null;
            OnGameStateChanged = null;
        }
    }
}