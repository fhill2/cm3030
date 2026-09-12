using UnityEngine;

namespace Game.Core
{
    // The five states the game loop can be in.
    public enum GameStateId
    {
        Menu,
        WaveActive,
        WaveComplete,
        Shop,
        GameOver
    }

    // Sent on every state change.
    public struct GameStateChangedArgs
    {
        public GameStateId Previous;
        public GameStateId Current;
        // Only meaningful for WaveActive.
        public int Wave;

        public GameStateChangedArgs(GameStateId previous, GameStateId current, int wave)
        {
            Previous = previous;
            Current = current;
            Wave = wave;
        }
    }

    // Sent when lightning flashes. ThunderDelay is how long audio waits
    // before the thunder clap.
    public readonly struct LightningArgs
    {
        public readonly float ThunderDelay;

        public LightningArgs(float thunderDelay)
        {
            ThunderDelay = thunderDelay;
        }
    }

    // Sent when a wave is cleared. Wave is the one just finished.
    public struct WaveClearedArgs
    {
        public readonly int Wave;

        public WaveClearedArgs(int wave)
        {
            Wave = wave;
        }
    }

    // Sent when the player picks up a tome.
    public readonly struct TomeFoundArgs
    {
        public readonly SpellDef Spell;

        public TomeFoundArgs(SpellDef spell)
        {
            Spell = spell;
        }
    }

    // Sent when the player's gold changes. Change is the amount added or removed.
    public readonly struct GoldChangedArgs
    {
        public readonly int Total;
        public readonly int Change;

        public GoldChangedArgs(int total, int change)
        {
            Total = total;
            Change = change;
        }
    }

    // Shop countdown, for the timer in the UI.
    public readonly struct ShopTimeArgs
    {
        public readonly float SecondsLeft;
        public readonly float TotalSeconds;

        public ShopTimeArgs(float secondsLeft, float totalSeconds)
        {
            SecondsLeft = secondsLeft;
            TotalSeconds = totalSeconds;
        }
    }

    // Sent when an entity's stamina changes.
    public readonly struct StaminaChangedArgs
    {
        public readonly GameObject Entity;
        public readonly float Current;
        public readonly float Max;

        public StaminaChangedArgs(GameObject entity, float current, float max)
        {
            Entity = entity;
            Current = current;
            Max = max;
        }
    }

    // Sent when an entity is stunned, and again when the stun lifts.
    public readonly struct StunArgs
    {
        public readonly GameObject Entity;
        public readonly bool Stunned;

        public StunArgs(GameObject entity, bool stunned)
        {
            Entity = entity;
            Stunned = stunned;
        }
    }

    public readonly struct FleeArgs
    {
        public readonly GameObject Entity;

        public FleeArgs(GameObject entity)
        {
            Entity = entity;
        }
    }
}