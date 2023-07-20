using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;

namespace Core.Main_Characters.Nema.Combat.Perks.AoE
{
    public class Loneliness : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            LonelinessInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record LonelinessRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(LonelinessRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            LonelinessInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class LonelinessInstance : PerkInstance, IChargeModifier
    {
        private const double ChargeModifierPerMissingTarget = 0.2;

        public LonelinessInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public LonelinessInstance(CharacterStateMachine owner, [NotNull] LonelinessRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.SkillModule.ChargeModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SkillModule.ChargeModifiers.Remove(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new LonelinessRecord(Key);

        public void Modify(ref ChargeStruct chargeStruct)
        {
            const int expectedTargetCount = 4;
            ref CustomValuePooledList<TargetProperties> targets = ref chargeStruct.TargetsProperties;
            int targetCount = targets.Count;
            ref TSpan charge = ref chargeStruct.Charge;
            charge.Multiply(1 - (ChargeModifierPerMissingTarget * (expectedTargetCount - targetCount)));
        }

        [NotNull]
        public string SharedId => nameof(LonelinessInstance);
        public int Priority => 998;
    }
}