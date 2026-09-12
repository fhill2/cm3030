namespace Game.Core
{
    // Base class every state inherits from. Not a MonoBehaviour, so states
    public abstract class GameState
    {
        protected GameStateMachine fsm;

        public abstract GameStateId Id { get; }

        // Runs once on switching into this state.
        public virtual void EnterState(GameStateMachine machine)
        {
            fsm = machine;
        }

        // Runs every frame while this state is current.
        public virtual void UpdateState() { }

        // Runs once on switching out.
        public virtual void ExitState() { }
    }
}