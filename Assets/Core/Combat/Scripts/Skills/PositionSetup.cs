using System;
using UnityEngine;

namespace Core.Combat.Scripts.Skills
{
    [Serializable]
    public struct PositionSetup
    {
        public const int Length = 4;
        
        [SerializeField] private bool one, two, three, four;

        public bool this[int index]
        {
            get
            {
                return index switch
                {
                    0 => one,
                    1 => two,
                    2 => three,
                    3 => four,
                    _ => throw new ArgumentOutOfRangeException(nameof(index), index, null)
                };
            }
            set
            {
                switch (index)
                {
                    case 0: one = value; break;
                    case 1: two = value; break;
                    case 2: three = value; break;
                    case 3: four = value; break;
                    default: throw new ArgumentOutOfRangeException(nameof(index), index, null);
                }
            }
        }
        
        public PositionSetup(bool one, bool two, bool three, bool four)
        {
            this.one = one;
            this.two = two;
            this.three = three;
            this.four = four;
        }
    }
}