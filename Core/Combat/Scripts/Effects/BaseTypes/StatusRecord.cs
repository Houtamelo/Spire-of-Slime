using System.Collections.Generic;
using System.Text;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public abstract record StatusRecord(float Duration, bool IsPermanent)
    {
        public abstract bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters);
    }
}