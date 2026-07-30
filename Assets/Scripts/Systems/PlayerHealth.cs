using Game.Shared;

namespace Game.Systems
{
    public class PlayerHealth : HealthSystem
    {
        protected override void OnDeath()
        {
            GameEvents.OnGameOver?.Invoke();
        }
    }
}
