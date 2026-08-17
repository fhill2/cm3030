using System.Reflection;
using UnityEngine;
using Game.Health;

namespace Game.Core
{
    public class UnlimitedHealth : MonoBehaviour
    {
        private static readonly FieldInfo MaxField =
            typeof(HealthSystem).GetField("maxHealth", BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly FieldInfo CurrentField =
            typeof(HealthSystem).GetField("currentHealth", BindingFlags.Instance | BindingFlags.NonPublic);

        void Start()
        {
            foreach (HealthSystem health in FindObjectsByType<HealthSystem>(FindObjectsSortMode.None))
            {
                if (MaxField != null) MaxField.SetValue(health, float.MaxValue);
                if (CurrentField != null) CurrentField.SetValue(health, float.MaxValue);
            }
        }
    }
}
