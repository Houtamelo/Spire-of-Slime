using System;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Managers.Enumerators;
using UnityEngine;
using Utils.Patterns;
using Utils.Extensions;

namespace Core.Combat.Scripts.Effects.Types.Guarded
{
    public record GuardedRecord(float Duration, bool IsPermanent, Guid Caster) : StatusRecord(Duration, IsPermanent)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            foreach (CharacterRecord character in allCharacters)
            {
                if (character.Guid == Caster)
                    return true;
            }
            
            errors.AppendLine("Invalid ", nameof(GuardedRecord), " data. ", nameof(Caster), "'s Guid: ", Caster.ToString(), " could not be mapped to a character.");
            return false;
        }
    }

    public class Guarded : StatusInstance
    {
        public override bool IsPositive => true;

        public readonly CharacterStateMachine Caster;

        private Guarded(float duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster) : base(duration: duration, isPermanent: isPermanent, owner: owner)
        {
            Caster = caster;
        }

        public static Option<StatusInstance> CreateInstance(float duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster)
        {
            if (duration <= 0 && !isPermanent)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(Guarded)}. Duration:{duration.ToString()}, IsPermanent: false");
                return Option<StatusInstance>.None;
            }

            Guarded instance = new(duration, isPermanent, owner, caster);
            owner.StatusModule.AddStatus(instance, caster);
            return instance;
        }

        private Guarded(GuardedRecord record, CharacterStateMachine owner, CharacterStateMachine caster) : base(record, owner)
        {
            Caster = caster;
        }

        public static Option<StatusInstance> CreateInstance(GuardedRecord record, CharacterStateMachine owner, ref CharacterEnumerator allCharacters)
        {
            CharacterStateMachine caster = null;
            foreach (CharacterStateMachine character in allCharacters)
            {
                if (character.Guid == record.Caster)
                {
                    caster = character;
                    break;
                }
            }
            
            if (caster == null)
                return Option<StatusInstance>.None;

            Guarded instance = new(record, owner, caster);
            owner.StatusModule.AddStatus(instance, caster);
            return Option<StatusInstance>.Some(instance);
        }

        public override void CharacterDefeated(CharacterStateMachine character, bool becomesCorpseOnDefeat)
        {
            if (character == Caster || character == Owner)
                RequestDeactivation();
        }
        
        public override StatusRecord GetRecord() => new GuardedRecord(Duration, IsPermanent, Caster.Guid);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.Guarded;
        public const int GlobalId = BuffOrDebuff.BuffOrDebuff.GlobalId + 1;
    }
}