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

    // Every weapon and shield prefab under Resources/Equipment, loaded once.
    // The market lists from here and WaveSpawner picks enemy gear from here.
    public static class EquipmentCatalog
    {
        private static List<EquipmentEntry> entries;

        public static IReadOnlyList<EquipmentEntry> Entries => EnsureLoaded();

        public static IReadOnlyList<EquipmentEntry> Weapons => Filter(EquipmentKind.Weapon);
        public static IReadOnlyList<EquipmentEntry> Shields => Filter(EquipmentKind.Shield);

        // Statics survive a scene reload, so clear the cache on a new run.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            entries = null;
        }

        private static List<EquipmentEntry> EnsureLoaded()
        {
            if (entries != null) return entries;

            entries = new List<EquipmentEntry>();

            foreach (GameObject prefab in Resources.LoadAll<GameObject>("Equipment/Weapons"))
            {
                WeaponDef def = prefab.GetComponent<Weapon>()?.Def;
                if (def == null) continue;

                entries.Add(new EquipmentEntry
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

                entries.Add(new EquipmentEntry
                {
                    Prefab = prefab,
                    Name = def.DisplayName,
                    Kind = EquipmentKind.Shield,
                    Level = def.Level,
                    Cost = def.Cost,
                });
            }

            return entries;
        }

        private static List<EquipmentEntry> Filter(EquipmentKind kind)
        {
            List<EquipmentEntry> result = new List<EquipmentEntry>();
            foreach (EquipmentEntry entry in EnsureLoaded())
                if (entry.Kind == kind) result.Add(entry);
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

        // Picks evenly at random from everything in the level range, in one
        // pass, without building a temporary list.
        private static GameObject Pick(IReadOnlyList<EquipmentEntry> pool, int minLevel, int maxLevel)
        {
            if (pool == null || pool.Count == 0) return null;

            int matches = 0;
            GameObject pick = null;

            foreach (EquipmentEntry entry in pool)
            {
                if (entry.Level < minLevel) continue;
                if (maxLevel > 0 && entry.Level > maxLevel) continue;

                matches++;
                if (pick == null || Random.Range(0, matches) == 0) pick = entry.Prefab;
            }

            return pick;
        }
    }
}