using System;
using System.Collections.Generic;

namespace Core.Combat.Scripts.Effects.Interfaces
{
    public interface IModifier
    {
        string SharedId { get; }
        int Priority { get; }
    }

    public class ModifierComparer : IComparer<IModifier>
    {
        public int Compare(IModifier x, IModifier y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (ReferenceEquals(null, y))
                return 1;
            if (ReferenceEquals(null, x))
                return -1;
            int priorityComparison = x.Priority.CompareTo(y.Priority);
            if (priorityComparison != 0)
                return priorityComparison;
            
            return string.Compare(x.SharedId, y.SharedId, StringComparison.Ordinal);
        }
        
        private ModifierComparer(){}
        
        public static readonly ModifierComparer Instance = new();
    }
}