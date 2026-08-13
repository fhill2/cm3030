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
}