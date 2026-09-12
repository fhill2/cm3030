using System.Collections;
using UnityEngine;

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

    // Wave cleared. Short breather, then the shop opens.
    public class WaveCompleteState : GameState
    {
        public override GameStateId Id => GameStateId.WaveComplete;

        private Coroutine waitRoutine;

        public override void EnterState(GameStateMachine machine)
        {
            base.EnterState(machine);
            waitRoutine = fsm.StartCoroutine(WaitThenShop());
        }

        public override void ExitState()
        {
            // Stop the timer if something else moved us on first, like the
            // player dying during the gap.
            if (waitRoutine != null)
            {
                fsm.StopCoroutine(waitRoutine);
                waitRoutine = null;
            }
        }

        private IEnumerator WaitThenShop()
        {
            yield return new WaitForSeconds(fsm.WaveCompleteDelay);

            waitRoutine = null;
            fsm.MoveToState(GameStateId.Shop);
        }
    }

    // Shop is open. Fixed window to spend, or press Done to skip the rest.
    // Either way the next wave follows.
    public class ShopState : GameState
    {
        public override GameStateId Id => GameStateId.Shop;

        private Coroutine timer;

        public override void EnterState(GameStateMachine machine)
        {
            base.EnterState(machine);
            timer = fsm.StartCoroutine(RunTimer());
        }

        public override void ExitState()
        {
            // Done button or player death gets us out early, so kill the timer
            // or it would push us into another wave later.
            if (timer != null)
            {
                fsm.StopCoroutine(timer);
                timer = null;
            }

            // Tell the UI the clock is at zero so nothing is left on screen.
            EventManager.RaiseShopTime(new ShopTimeArgs(0f, fsm.ShopDuration));
        }

        private IEnumerator RunTimer()
        {
            float totalSeconds = fsm.ShopDuration;
            float secondsLeft = totalSeconds;

            while (secondsLeft > 0f)
            {
                EventManager.RaiseShopTime(new ShopTimeArgs(secondsLeft, totalSeconds));
                yield return null;
                secondsLeft -= Time.deltaTime;
            }

            timer = null;
            fsm.MoveToState(GameStateId.WaveActive);
        }
    }

    // Player died, run over.
    public class GameOverState : GameState
    {
        public override GameStateId Id => GameStateId.GameOver;
    }
}