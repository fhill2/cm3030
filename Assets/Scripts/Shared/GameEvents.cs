using System;
using UnityEngine;

namespace Game.Shared
{
    public static class GameEvents
    {
        public static Action<GameObject, float, DamageType> OnDamageDealt;
        public static Action<GameObject> OnEnemyKilled;
        public static Action OnPlayerBlock;
        public static Action OnPlayerParry;

        public static Action<int> OnWaveChanged;
        public static Action OnRoundStart;
        public static Action OnRoundEnd;
        public static Action OnGameStart;
        public static Action OnGameOver;
        public static Action<int> OnMoraleChanged;
    }
}
