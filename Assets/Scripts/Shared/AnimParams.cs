using UnityEngine;

namespace Game.Shared
{
    public static class AnimParams
    {
        // ── Movement ───────────────────────────────
        public static readonly int Speed       = Animator.StringToHash("Speed");
        public static readonly int Grounded    = Animator.StringToHash("Grounded");
        public static readonly int Jump        = Animator.StringToHash("Jump");

        // ── Combat ─────────────────────────────────
        public static readonly int Attack      = Animator.StringToHash("Attack");
        public static readonly int ComboStep   = Animator.StringToHash("ComboStep");
        public static readonly int Block       = Animator.StringToHash("Block");
        public static readonly int Parry       = Animator.StringToHash("Parry");

        // ── Health ─────────────────────────────────
        public static readonly int Hit         = Animator.StringToHash("Hit");
        public static readonly int Die         = Animator.StringToHash("Die");
    }
}
