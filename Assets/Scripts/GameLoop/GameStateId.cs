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

    // Data sent with the state changed event, so subscribers know
    // where we came from as well as where we are now.
    public struct GameStateChangedArgs
    {
        public GameStateId Previous;
        public GameStateId Current;
        // Wave number at the moment of the transition; only meaningful for WaveActive.
        public int Wave;

        public GameStateChangedArgs(GameStateId previous, GameStateId current, int wave)
        {
            Previous = previous;
            Current = current;
            Wave = wave;
        }
    }

    // Sent when a wave is cleared; Wave is the number that was just finished.
    public struct WaveClearedArgs
    {
        public readonly int Wave;

        public WaveClearedArgs(int wave)
        {
            Wave = wave;
        }
    }

    // Sent when a tome drops into the player's hands, so the UI can announce
    // that a new spell is available in the market.
    public readonly struct TomeFoundArgs
    {
        public readonly SpellDef Spell;

        public TomeFoundArgs(SpellDef spell)
        {
            Spell = spell;
        }
    }

    // Sent whenever the player's gold changes. Change is how much was added
    // or removed, so the UI can show a "+10" popup as well as the new total.
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

    // Counts down while the shop is open, so the UI can show a timer.
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

    // Sent whenever an entity's stamina changes. Entity is included so a bar
    // can ignore anyone but its own owner.
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

    // Sent when an entity is stunned by running out of stamina, and again
    // when the stun lifts.
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