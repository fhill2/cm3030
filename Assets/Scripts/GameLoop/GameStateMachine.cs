using UnityEngine;

namespace Game.Core
{
    // Runs the game loop state machine.
    //
    // Holds one instance of each state, stores the data they share, and does
    // the switching. Every switch fires OnGameStateChanged so the UI, audio,
    // spawner and shop can each react on their own without knowing about
    // each other.
    //
    // Goes on the GameManager object in the scene.
    public class GameStateMachine : MonoBehaviour
    {
        [SerializeField] private bool logTransitions = true;

        public MenuState Menu { get; private set; }
        public WaveActiveState WaveActive { get; private set; }
        public WaveCompleteState WaveComplete { get; private set; }
        public ShopState Shop { get; private set; }
        public GameOverState GameOver { get; private set; }

        // The state we're in right now.
        public GameState CurrentState { get; private set; }

        // Which wave the player is on. Reset in Menu, +1 on entering WaveActive.
        public int CurrentWave { get; set; }

        private void Awake()
        {
            Menu = new MenuState();
            WaveActive = new WaveActiveState();
            WaveComplete = new WaveCompleteState();
            Shop = new ShopState();
            GameOver = new GameOverState();
        }

        private void Start()
        {
            // Done in Start rather than Awake so anything that subscribed in
            // OnEnable still catches the very first state change.
            MoveToState(Menu);
        }

        private void Update()
        {
            if (CurrentState != null) CurrentState.UpdateState();
        }

        // The only place CurrentState gets assigned.
        public void MoveToState(GameState next)
        {
            if (next == null) return;

            // On the first call there's no previous state, so just use the new one.
            GameStateId previous = CurrentState != null ? CurrentState.Id : next.Id;

            if (CurrentState != null) CurrentState.ExitState();

            CurrentState = next;
            CurrentState.EnterState(this);

            if (logTransitions)
            {
                Debug.Log($"[GameState] {previous} -> {CurrentState.Id}");
            }

            EventManager.RaiseGameStateChanged(new GameStateChangedArgs(previous, CurrentState.Id, CurrentWave));
        }

        // Same thing but by enum, easier to call from other scripts and to test.
        public void MoveToState(GameStateId id)
        {
            MoveToState(StateFor(id));
        }

        private GameState StateFor(GameStateId id)
        {
            switch (id)
            {
                case GameStateId.Menu: return Menu;
                case GameStateId.WaveActive: return WaveActive;
                case GameStateId.WaveComplete: return WaveComplete;
                case GameStateId.Shop: return Shop;
                case GameStateId.GameOver: return GameOver;
                default: return null;
            }
        }
    }
}