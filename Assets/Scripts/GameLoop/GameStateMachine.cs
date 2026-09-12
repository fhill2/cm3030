using UnityEngine;

namespace Game.Core
{
    // Runs the game loop state machine. Holds one instance of each state and
    // fires OnGameStateChanged on every switch, so the UI, audio, spawner and
    // shop can react independently.
    // Sits on the GameManager in the scene.
    public class GameStateMachine : MonoBehaviour
    {
        [SerializeField] private bool logTransitions = true;

        [Tooltip("Which state the game starts in on load. Defaults to WaveActive so enemies spawn immediately.")]
        [SerializeField] private GameStateId initialState = GameStateId.WaveActive;

        [Tooltip("Seconds between a wave being cleared and the shop opening.")]
        [SerializeField] private float waveCompleteDelay = 3f;

        [Tooltip("Seconds the shop stays open before the next wave starts automatically.")]
        [SerializeField] private float shopDuration = 30f;

        public MenuState Menu { get; private set; }
        public WaveActiveState WaveActive { get; private set; }
        public WaveCompleteState WaveComplete { get; private set; }
        public ShopState Shop { get; private set; }
        public GameOverState GameOver { get; private set; }

        public GameState CurrentState { get; private set; }

        // Reset in Menu, +1 on entering WaveActive.
        public int CurrentWave { get; set; }

        public float WaveCompleteDelay => waveCompleteDelay;
        public float ShopDuration => shopDuration;

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
            // In Start, not Awake, so anything subscribing in OnEnable still
            // catches the first state change.
            MoveToState(initialState);
        }

        private void OnEnable()
        {
            EventManager.OnWaveCleared += HandleWaveCleared;
        }

        private void OnDisable()
        {
            EventManager.OnWaveCleared -= HandleWaveCleared;
        }

        private void HandleWaveCleared(WaveClearedArgs e)
        {
            MoveToState(GameStateId.WaveComplete);
        }

        private void Update()
        {
            if (CurrentState != null) CurrentState.UpdateState();
        }

        // The only place CurrentState gets assigned.
        public void MoveToState(GameState next)
        {
            if (next == null) return;

            // No previous state on the first call, so use the new one.
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