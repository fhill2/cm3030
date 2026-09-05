using System.Collections.Generic;
using UnityEngine;
using Game.Shared;

namespace Game.Combat
{
    public static class CombatProbe
    {
        public static bool IsIgnored(Transform other, Transform ignoreRoot)
        {
            if (other == null) return true;
            if (ignoreRoot == null) return false;

            Transform root = ignoreRoot.root;
            return other == ignoreRoot
                   || other.IsChildOf(root)
                   || root.IsChildOf(other);
        }

        public static IDamageable Resolve(Collider collider, Transform ignoreRoot)
        {
            if (collider == null || IsIgnored(collider.transform, ignoreRoot)) return null;
            return collider.GetComponentInParent<IDamageable>();
        }

        public static IDamageable Sweep(Vector3 origin, Vector3 direction, float distance,
            float radius, Transform ignoreRoot)
        {
            if (Physics.SphereCast(origin, radius, direction, out RaycastHit hit,
                    distance, ~0, QueryTriggerInteraction.Ignore))
            {
                IDamageable target = Resolve(hit.collider, ignoreRoot);
                if (target != null && target.IsAlive) return target;
            }

            return null;
        }

        public static List<IDamageable> Arc(Vector3 origin, Vector3 forward, float range,
            float halfAngle, Transform ignoreRoot)
        {
            var hits = Physics.OverlapSphere(origin, range, ~0, QueryTriggerInteraction.Ignore);
            var targets = new List<IDamageable>();

            foreach (Collider collider in hits)
            {
                IDamageable target = Resolve(collider, ignoreRoot);
                if (target == null || !target.IsAlive) continue;
                if (targets.Contains(target)) continue;

                Vector3 toTarget = collider.transform.position - origin;
                toTarget.y = 0f;
                if (toTarget.sqrMagnitude < 0.001f) continue;

                if (Vector3.Angle(new Vector3(forward.x, 0f, forward.z), toTarget) <= halfAngle)
                    targets.Add(target);
            }

            return targets;
        }
    }
}
