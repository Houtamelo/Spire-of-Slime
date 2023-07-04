using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using ListPool;
using Main_Database.Combat;
using Save_Management;
using UnityEngine;
using Utils.Extensions;

namespace Core.Main_Characters.Nema.Combat.Perks.AoE
{
    public class Loneliness : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
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

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            LonelinessInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class LonelinessInstance : PerkInstance, IChargeModifier
    {
        private const float ChargeModifierPerMissingTarget = 0.2f;
        public string SharedId => nameof(LonelinessInstance);
        public int Priority => 998;

        public LonelinessInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public LonelinessInstance(CharacterStateMachine owner, LonelinessRecord record) : base(owner, record)
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

        public override PerkRecord GetRecord() => new LonelinessRecord(Key);

        public void Modify(ref ChargeStruct chargeStruct)
        {
            const int expectedTargetCount = 4;
            ref ValueListPool<TargetProperties> targets = ref chargeStruct.TargetsProperties;
            int targetCount = targets.Count;
            ref float charge = ref chargeStruct.Charge;
            charge *= 1 - ChargeModifierPerMissingTarget * (expectedTargetCount - targetCount);
            
#if UNITY_EDITOR
            Debug.Assert(chargeStruct.Charge == charge, $"ChargeStruct charge is not equal to charge variable. ChargeStruct: {chargeStruct.Charge}, charge: {charge}");
#endif
        }
    }
}