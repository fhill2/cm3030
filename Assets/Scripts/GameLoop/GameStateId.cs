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
}