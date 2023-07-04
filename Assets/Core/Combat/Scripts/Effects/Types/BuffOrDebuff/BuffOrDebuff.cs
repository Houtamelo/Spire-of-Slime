using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Enums;
using UnityEngine;
using Utils.Patterns;
using Utils.Extensions;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
    public record BuffOrDebuffRecord(float Duration, bool IsPermanent, CombatStat Attribute, float Delta) : StatusRecord(Duration, IsPermanent)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (Delta == 0)
            {
                errors.AppendLine("Invalid ", nameof(BuffOrDebuffRecord), " data. ", nameof(Delta), " cannot be 0.");
                return false;
            }
            
            return true;
        }
    }

    public class BuffOrDebuff : StatusInstance, IBaseFloatAttributeModifier, IBaseIntAttributeModifier
    {
        public override bool IsPositive => EffectType == EffectType.Buff;

        public readonly CombatStat Attribute;
        public readonly float Delta;

        private BuffOrDebuff(float duration, bool isPermanent, CharacterStateMachine owner, CombatStat attribute, float delta) : base(duration: duration, isPermanent: isPermanent, owner: owner)
        {
            Attribute = attribute;
            Delta = delta;
            EffectType = delta > 0 ? EffectType.Buff : EffectType.Debuff;
        }

        public static Option<StatusInstance> CreateInstance(float duration, bool isPermanent, CharacterStateMachine owner, CharacterStateMachine caster, CombatStat attribute, float delta)
        {
            if ((duration <= 0 && !isPermanent) || delta == 0)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(BuffOrDebuff)} effect. Duration: {duration.ToString()}, IsPermanent: {isPermanent.ToString()}, Delta: {delta.ToString()}");
                return Option<StatusInstance>.None;
            }

            BuffOrDebuff buffOrDebuff = new(duration, isPermanent, owner, attribute, delta);
            buffOrDebuff.Subscribe();
            owner.StatusModule.AddStatus(buffOrDebuff, caster);
            return Option<StatusInstance>.Some(buffOrDebuff);
        }

        private BuffOrDebuff(BuffOrDebuffRecord record, CharacterStateMachine owner) : base(record, owner)
        {
            EffectType = record.Delta > 0 ? EffectType.Buff : EffectType.Debuff;
            Attribute = record.Attribute;
            Delta = record.Delta;
        }

        public static Option<StatusInstance> CreateInstance(BuffOrDebuffRecord record, CharacterStateMachine owner)
        {
            BuffOrDebuff buffOrDebuff = new(record, owner);
            owner.StatusModule.AddStatus(buffOrDebuff, owner);
            buffOrDebuff.Subscribe();
            return Option<StatusInstance>.Some(buffOrDebuff);
        }

        public override void RequestDeactivation()
        {
            this.Unsubscribe();
            base.RequestDeactivation();
        }


        public void Modify(ref float value, CharacterStateMachine self) => value += Delta;
        public void Modify(ref int value, CharacterStateMachine self) => value += (int) Delta;

        public override StatusRecord GetRecord() => new BuffOrDebuffRecord(Duration, IsPermanent, Attribute, Delta);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType { get; }
        public const int GlobalId = Arousal.Arousal.GlobalId + 1;
        public string SharedId => nameof(BuffOrDebuff);
        public int Priority => -1;
    }
}