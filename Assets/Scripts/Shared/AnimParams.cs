using UnityEngine;

namespace Game.Shared
{
    public static class AnimParams
    {
        public static readonly int Speed       = Animator.StringToHash("Speed");
        public static readonly int AttackLight = Animator.StringToHash("AttackLight");
        public static readonly int AttackHeavy = Animator.StringToHash("AttackHeavy");
        public static readonly int Block       = Animator.StringToHash("Block");
        public static readonly int Parry       = Animator.StringToHash("Parry");
        public static readonly int Hit         = Animator.StringToHash("Hit");
        public static readonly int Die         = Animator.StringToHash("Die");

        public static readonly int Grounded    = Animator.StringToHash("Grounded");
        public static readonly int Jump        = Animator.StringToHash("Jump");
        public static readonly int ComboStep   = Animator.StringToHash("ComboStep");
    }
}
