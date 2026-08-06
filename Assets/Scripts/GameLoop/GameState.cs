namespace Game.Core
{
    // Base class every state inherits from.
    //
    // This is not a MonoBehaviour on purpose. The states are just normal
    // objects created by GameStateMachine and never sit on a GameObject.
    // Side effect: they can't run coroutines, so use fsm.StartCoroutine(...)
    // and let the machine run it instead.
    //
    // EnterState is virtual and does the shared setup. States override it,
    // call base.EnterState(machine) first, then do their own thing.
    public abstract class GameState
    {
        protected GameStateMachine fsm;

        // Which state this is.
        public abstract GameStateId Id { get; }

        // Runs once when we switch into this state.
        public virtual void EnterState(GameStateMachine machine)
        {
            fsm = machine;
        }

        // Runs every frame while this state is the current one.
        public virtual void UpdateState() { }

        // Runs once as we switch out of this state.
        public virtual void ExitState() { }
    }
}