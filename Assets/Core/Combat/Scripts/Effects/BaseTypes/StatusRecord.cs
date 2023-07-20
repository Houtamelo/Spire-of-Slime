using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Utils.Math;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public abstract record StatusRecord(TSpan Duration, bool Permanent)
    {
        public abstract bool IsDataValid<T>(StringBuilder errors, T allCharacters) where T : class, IEnumerable<CharacterRecord>;
        
        public abstract void Deserialize(CharacterStateMachine owner, DirectCharacterEnumerator allCharacters);
    }
}