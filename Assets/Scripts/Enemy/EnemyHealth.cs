using Game.Health;

namespace Game.Enemy
{
    /// <summary>
    /// Enemy health. Inherits all damage/death handling from <see cref="HealthSystem"/>,
    /// which already implements <see cref="Game.Shared.IDamageable"/>, fires the
    /// OnDamage/OnDeath events through <see cref="Game.Core.EventManager"/>, and
    /// plays the Hit/Die animation triggers.
    /// Add enemy-specific death logic (score, drops, wave manager notify) in OnDeath.
    /// </summary>
    public class EnemyHealth : HealthSystem
    {
        protected override void OnDeath()
        {
            // TODO: enemy-specific death logic (score, drops, wave notify).
        }
    }
}
