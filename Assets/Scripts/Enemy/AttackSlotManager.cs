using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemy
{
    // Coordinates enemies in AttackState so a group surrounds the player
    // rather than all swinging at once. Two jobs: hand out a limited number of
    // attack slots, and give everyone waiting an evenly spaced spot in a ring that slowly rotates.
    public static class AttackSlotManager
    {
        // Difficulty dial. 1 keeps fights one-on-one even in a big group.
        public static int MaxActiveAttackers = 1;

        // Radians per second the whole ring turns.
        private const float RingRotationSpeed = 0.15f;

        private static readonly HashSet<NpcFSM> activeAttackers = new HashSet<NpcFSM>();

        // Everyone in AttackState, attacking or waiting, in join order.
        private static readonly List<NpcFSM> ring = new List<NpcFSM>();

        // True if this enemy already holds a slot or just claimed a free one.
        public static bool TryClaimSlot(NpcFSM npc)
        {
            if (activeAttackers.Contains(npc)) return true;
            if (activeAttackers.Count >= MaxActiveAttackers) return false;

            activeAttackers.Add(npc);
            return true;
        }

        public static void ReleaseSlot(NpcFSM npc)
        {
            activeAttackers.Remove(npc);
        }

        // Called on entering AttackState.
        public static void JoinRing(NpcFSM npc)
        {
            if (!ring.Contains(npc)) ring.Add(npc);
        }

        // Called on leaving AttackState for any reason.
        public static void LeaveRing(NpcFSM npc)
        {
            ring.Remove(npc);
        }

        // Angle in radians this enemy should orbit at, spaced from every other
        // ring member and drifting over time so the formation doesn't freeze.
        public static float GetRingAngle(NpcFSM npc)
        {
            int index = ring.IndexOf(npc);
            if (index < 0 || ring.Count == 0) return 0f;

            float baseRotation = Time.time * RingRotationSpeed;
            float slotSpacing = (Mathf.PI * 2f / ring.Count) * index;
            return baseRotation + slotSpacing;
        }

        public static void ClearAll()
        {
            activeAttackers.Clear();
            ring.Clear();
        }
    }
}