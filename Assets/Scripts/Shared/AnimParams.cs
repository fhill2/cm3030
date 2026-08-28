using UnityEngine;

namespace Game.Shared
{
    public static class AnimParams
    {
        // ── Movement ───────────────────────────────
        public static readonly int Speed       = Animator.StringToHash("Speed");
        public static readonly int MoveX       = Animator.StringToHash("MoveX");
        public static readonly int MoveZ       = Animator.StringToHash("MoveZ");
        public static readonly int Grounded    = Animator.StringToHash("Grounded");
        public static readonly int Jump        = Animator.StringToHash("Jump");

        // ── Combat ─────────────────────────────────
        public static readonly int Attack      = Animator.StringToHash("Attack");
        public static readonly int ComboStep   = Animator.StringToHash("ComboStep");
        public static readonly int InCombat    = Animator.StringToHash("InCombat");
        public static readonly int Block       = Animator.StringToHash("Block");
        public static readonly int Sprint      = Animator.StringToHash("Sprint");
        public static readonly int Parry       = Animator.StringToHash("Parry");
        public static readonly int Buff        = Animator.StringToHash("Buff");
        public static readonly int Drink       = Animator.StringToHash("Drink");
        public static readonly int Throw       = Animator.StringToHash("Throw");
        public static readonly int Collect     = Animator.StringToHash("Collect");

        // ── Health ─────────────────────────────────
        public static readonly int Hit         = Animator.StringToHash("Hit");
        public static readonly int GetHitIndex = Animator.StringToHash("GetHitIndex");
        public static readonly int DeathIndex  = Animator.StringToHash("DeathIndex");
        public static readonly int Dead        = Animator.StringToHash("Dead");
    }
}
