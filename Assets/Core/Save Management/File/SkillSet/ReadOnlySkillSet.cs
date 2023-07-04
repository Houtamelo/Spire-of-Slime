using System;
using Utils.Patterns;

namespace Save_Management
{
    public readonly ref struct ReadOnlySkillSet
    {
        public readonly Option<CleanString> One;
        public readonly Option<CleanString> Two;
        public readonly Option<CleanString> Three;
        public readonly Option<CleanString> Four;
        
        public ReadOnlySkillSet(CleanString one, CleanString two, CleanString three, CleanString four)
        {
            One = one.IsNullOrEmpty() ? Option.None : Option<CleanString>.Some(one);
            Two = two.IsNullOrEmpty() ? Option.None : Option<CleanString>.Some(two);
            Three = three.IsNullOrEmpty() ? Option.None : Option<CleanString>.Some(three);
            Four = four.IsNullOrEmpty() ? Option.None : Option<CleanString>.Some(four);
        }

        public Option<CleanString> this[int index] => index switch { 0 => One, 1 => Two, 2 => Three, 3 => Four, _ => throw new ArgumentOutOfRangeException(nameof(index), index, null) };
        
        public SkillSetStructEnumerator GetEnumerator() => new(this);

        public int HashSkills() => HashCode.Combine(One, Two, Three, Four);
    }
}