using System.Collections.Generic;
using UnityEngine;

namespace Game.Combat
{
    public enum EquipmentKind
    {
        Weapon,
        Shield
    }

    public struct EquipmentEntry
    {
        public GameObject Prefab;
        public string Name;
        public EquipmentKind Kind;
        public int Level;
        public int Cost;
        public float Damage;
        public float Speed;
    }

    public static class EquipmentCatalog
    {
        private static List<EquipmentEntry> s_entries;

        public static IReadOnlyList<EquipmentEntry> Entries => EnsureLoaded();

        public static IReadOnlyList<EquipmentEntry> Weapons => Filter(EquipmentKind.Weapon);
        public static IReadOnlyList<EquipmentEntry> Shields => Filter(EquipmentKind.Shield);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_entries = null;
        }

        private static List<EquipmentEntry> EnsureLoaded()
        {
            if (s_entries != null) return s_entries;

            s_entries = new List<EquipmentEntry>();

            foreach (GameObject prefab in Resources.LoadAll<GameObject>("Equipment/Weapons"))
            {
                WeaponDef def = prefab.GetComponent<Weapon>()?.Def;
                if (def == null) continue;

                s_entries.Add(new EquipmentEntry
                {
                    Prefab = prefab,
                    Name = def.DisplayName,
                    Kind = EquipmentKind.Weapon,
                    Level = def.Level,
                    Cost = def.Cost,
                    Damage = def.Damage,
                    Speed = def.Speed,
                });
            }

            foreach (GameObject prefab in Resources.LoadAll<GameObject>("Equipment/Shields"))
            {
                ShieldDef def = prefab.GetComponent<Shield>()?.Def;
                if (def == null) continue;

                s_entries.Add(new EquipmentEntry
                {
                    Prefab = prefab,
                    Name = def.DisplayName,
                    Kind = EquipmentKind.Shield,
                    Level = def.Level,
                    Cost = def.Cost,
                });
            }

            return s_entries;
        }

        private static List<EquipmentEntry> Filter(EquipmentKind kind)
        {
            var result = new List<EquipmentEntry>();
            foreach (EquipmentEntry e in EnsureLoaded())
                if (e.Kind == kind) result.Add(e);
            return result;
        }

        public static GameObject PickWeapon(int minLevel, int maxLevel)
        {
            return Pick(Weapons, minLevel, maxLevel);
        }

        public static GameObject PickShield(int minLevel, int maxLevel)
        {
            return Pick(Shields, minLevel, maxLevel);
        }

        private static GameObject Pick(IReadOnlyList<EquipmentEntry> pool, int minLevel, int maxLevel)
        {
            if (pool == null || pool.Count == 0) return null;

            int matches = 0;
            GameObject pick = null;

            foreach (EquipmentEntry e in pool)
            {
                if (e.Level < minLevel) continue;
                if (maxLevel > 0 && e.Level > maxLevel) continue;

                matches++;
                if (pick == null || Random.Range(0, matches) == 0) pick = e.Prefab;
            }

            return pick;
        }
    }
}
