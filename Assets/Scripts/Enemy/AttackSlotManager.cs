using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// Coordinates enemies in AttackState so a group reads as a Witcher-3-style
    /// surround rather than an independent free-for-all. Two jobs:
    ///
    /// 1. Hands out a limited number of "attack slots" so only a handful of
    ///    enemies press the attack at once — everyone else waits their turn.
    /// 2. Assigns everyone waiting an evenly-spaced spot in a ring around the
    ///    player (instead of each enemy picking its own random angle, which is
    ///    what caused them to clump together), and slowly rotates the whole
    ///    ring together so it stays lively without ever bunching back up.
    ///
    /// Static, same pattern as EventManager: any enemy can call in without
    /// needing a reference to a scene object. SceneInitializer clears it on
    /// scene load so state from a previous Play session can't leak into the
    /// next one.
    /// </summary>
    public static class AttackSlotManager
    {
        /// <summary>
        /// How many enemies may actively attack at the same time. Change this
        /// number to tune difficulty — 1 keeps fights very one-on-one even in
        /// a big group, higher numbers make groups feel more aggressive.
        /// </summary>
        public static int MaxActiveAttackers = 1;

        /// <summary>How fast the whole surrounding ring slowly rotates, in radians/sec.</summary>
        private const float RingRotationSpeed = 0.15f;

        private static readonly HashSet<NpcFSM> activeAttackers = new HashSet<NpcFSM>();

        // Every enemy currently in AttackState (attacking or waiting), in the
        // order they joined. Position in this list is what spaces enemies
        // evenly around the player — see GetRingAngle.
        private static readonly List<NpcFSM> ring = new List<NpcFSM>();

        /// <summary>
        /// Attempts to reserve a slot for this enemy. Returns true if it
        /// already holds one or successfully claimed a free one; false if
        /// every slot is taken by someone else.
        /// </summary>
        public static bool TryClaimSlot(NpcFSM npc)
        {
            if (activeAttackers.Contains(npc)) return true;
            if (activeAttackers.Count >= MaxActiveAttackers) return false;

            activeAttackers.Add(npc);
            return true;
        }

        /// <summary>Gives up a held slot so another waiting enemy can step in.</summary>
        public static void ReleaseSlot(NpcFSM npc)
        {
            activeAttackers.Remove(npc);
        }

        /// <summary>Joins the ring used to space waiting enemies out. Call on entering AttackState.</summary>
        public static void JoinRing(NpcFSM npc)
        {
            if (!ring.Contains(npc)) ring.Add(npc);
        }

        /// <summary>Leaves the ring. Call on exiting AttackState for any reason.</summary>
        public static void LeaveRing(NpcFSM npc)
        {
            ring.Remove(npc);
        }

        /// <summary>
        /// The angle (radians) this enemy should orbit at right now, evenly
        /// spaced from every other ring member and slowly rotating over time
        /// so the formation stays dynamic instead of freezing in place.
        /// </summary>
        public static float GetRingAngle(NpcFSM npc)
        {
            int index = ring.IndexOf(npc);
            if (index < 0 || ring.Count == 0) return 0f;

            float baseRotation = Time.time * RingRotationSpeed;
            float slotSpacing = (Mathf.PI * 2f / ring.Count) * index;
            return baseRotation + slotSpacing;
        }

        /// <summary>Clears all held slots and ring membership. Call on scene load.</summary>
        public static void ClearAll()
        {
            activeAttackers.Clear();
            ring.Clear();
        }
    }
}
