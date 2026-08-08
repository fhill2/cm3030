using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Game.Shared;
using Game.Core;
using Game.Health;

public class SwingTester : MonoBehaviour
{
    [SerializeField] private float damage = 50f;
    [SerializeField] private DamageType damageType = DamageType.Melee;
    [SerializeField] private float range = 2.5f;
    [SerializeField] private float radius = 1.2f;
    [SerializeField] private float heightOffset = 1f;

    void OnEnable()
    {
        EventManager.OnDamage += LogDamage;
        EventManager.OnDeath += LogDeath;
    }

    void OnDisable()
    {
        EventManager.OnDamage -= LogDamage;
        EventManager.OnDeath -= LogDeath;
    }

    void Update()
    {
        if (Mouse.current == null) return;
        if (Mouse.current.leftButton.wasPressedThisFrame) Strike();
    }

    public void Strike()
    {
        Collider[] hits = Physics.OverlapSphere(HitCentre(), radius);
        HashSet<IDamageable> already = new HashSet<IDamageable>();

        foreach (Collider hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target == null || !target.IsAlive) continue;
            if (hit.transform.root == transform.root) continue;
            if (!already.Add(target)) continue;

            target.TakeDamage(damage, damageType, gameObject);
        }
    }

    private Vector3 HitCentre()
    {
        return transform.position + Vector3.up * heightOffset + transform.forward * range;
    }

    private void LogDamage(DamageArgs e)
    {
        Debug.Log($"{e.Target.name} took {e.Amount} ({e.Type})");
    }

    private void LogDeath(DeathArgs e)
    {
        Debug.Log($"{e.Entity.name} DIED");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(HitCentre(), radius);
    }
}