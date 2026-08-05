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

        public GameStateChangedArgs(GameStateId previous, GameStateId current)
        {
            Previous = previous;
            Current = current;
        }
    }
}