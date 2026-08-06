namespace Game.Core
{
    // Title screen, nothing happening yet.
    public class MenuState : GameState
    {
        public override GameStateId Id => GameStateId.Menu;

        public override void EnterState(GameStateMachine machine)
        {
            base.EnterState(machine);
            fsm.CurrentWave = 0;   // back to the menu means a fresh run
        }
    }

    // Orcs are spawning and the player is fighting.
    public class WaveActiveState : GameState
    {
        public override GameStateId Id => GameStateId.WaveActive;

        public override void EnterState(GameStateMachine machine)
        {
            base.EnterState(machine);
            fsm.CurrentWave++;   // wave 1 on the first entry
        }
    }

    // Wave cleared. Short gap before the shop.
    public class WaveCompleteState : GameState
    {
        public override GameStateId Id => GameStateId.WaveComplete;
    }

    // Shop is open between rounds.
    public class ShopState : GameState
    {
        public override GameStateId Id => GameStateId.Shop;
    }

    // Player died, run over.
    public class GameOverState : GameState
    {
        public override GameStateId Id => GameStateId.GameOver;
    }
}