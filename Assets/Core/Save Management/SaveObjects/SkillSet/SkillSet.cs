using System;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;

namespace Core.Save_Management.SaveObjects
{
    public record SkillSet(CleanString One, CleanString Two, CleanString Three, CleanString Four) : IReadOnlySkillSet, IDeepCloneable<SkillSet>
    {
        public CleanString One { get; set; } = One;
        public CleanString Two { get; set; } = Two;
        public CleanString Three { get; set; } = Three;
        public CleanString Four { get; set; } = Four;

        public CleanString Get(int index)
        {
            if (index is < 0 or > 3)
            {
                Debug.LogWarning("Trying to get index out of range, returning empty string.");
                return string.Empty;
            }

            return index switch
            {
                0 => One,
                1 => Two,
                2 => Three,
                3 => Four,
                _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
            };
        }

        public void Set(int index, CleanString value)
        {
            switch (index)
            {
                case < 0 or > 3: Debug.LogWarning("Trying to set index out of range, ignoring."); return;
                case 0: One = value; break;
                case 1: Two = value; break;
                case 2: Three = value; break;
                case 3: Four = value; break;
            }
        }

        public SkillSetEnumerator GetEnumerator() => new(this);
        public SkillSet DeepClone() => new(One, Two, Three, Four);
    }
}