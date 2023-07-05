using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.Types.Marked
{
    public record MarkedRecord(float Duration, bool IsPermanent) : StatusRecord(Duration, IsPermanent)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters) => true;
    }

    public class Marked : StatusInstance
    {
        public override bool IsPositive => false;

        private Marked(float duration, bool isPermanent, CharacterStateMachine owner) : base(duration: duration, isPermanent: isPermanent, owner: owner)
        {
        }

        public static Option<StatusInstance> CreateInstance(float duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster)
        {
            if (duration <= 0 && !isPermanent)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(Marked)}. Duration:{duration.ToString()}, IsPermanent: false");
                return Option<StatusInstance>.None;
            }
            
            Marked instance = new(duration, isPermanent, owner);
            owner.StatusModule.AddStatus(instance, caster);
            return Option<StatusInstance>.Some(instance);
        }

        private Marked(MarkedRecord record, CharacterStateMachine owner) : base(record, owner)
        {
        }

        public static Option<StatusInstance> CreateInstance(MarkedRecord record, CharacterStateMachine owner)
        {
            Marked instance = new(record, owner);
            owner.StatusModule.AddStatus(instance, owner);
            return Option<StatusInstance>.Some(instance);
        }
        
        public override StatusRecord GetRecord() => new MarkedRecord(Duration, IsPermanent);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Marked;
        public const int GlobalId = Guarded.Guarded.GlobalId + 1;
    }
}