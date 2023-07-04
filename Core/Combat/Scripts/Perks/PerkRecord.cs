using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers.Enumerators;
using Save_Management;

namespace Core.Combat.Scripts.Perks
{
    public abstract record PerkRecord(CleanString Key)
    {
        public abstract bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters);
        public abstract PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters);
    }
}