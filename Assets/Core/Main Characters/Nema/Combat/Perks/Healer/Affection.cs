using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Heal;
using Core.Combat.Scripts.Effects.Types.OvertimeHeal;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Main_Database.Combat;
using Save_Management;
using Utils.Extensions;
using Utils.Math;

namespace Core.Main_Characters.Nema.Combat.Perks.Healer
{
    public class Affection : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            AffectionInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record AffectionRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(AffectionRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            AffectionInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class AffectionInstance : PerkInstance, IHealModifier, IOvertimeHealModifier
    {
        public string SharedId => nameof(AffectionInstance);
        public int Priority => 5;
        
        private const float HealPowerMultiplier = 1.4f;
        private const float OvertimeHealPowerMultiplier = 1.25f;

        public AffectionInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public AffectionInstance(CharacterStateMachine owner, AffectionRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            IStatusApplierModule applierModule = Owner.StatusApplierModule;
            applierModule.HealApplyModifiers.Add(this);
            applierModule.OvertimeHealApplyModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            IStatusApplierModule applierModule = Owner.StatusApplierModule;
            applierModule.HealApplyModifiers.Remove(this);
            applierModule.OvertimeHealApplyModifiers.Remove(this);
        }

        public override PerkRecord GetRecord() => new AffectionRecord(Key);

        public void Modify(ref HealToApply effectStruct)
        {
            foreach (StatusInstance status in effectStruct.Target.StatusModule.GetAll)
            {
                if (status.EffectType is EffectType.Debuff or EffectType.Poison && status.IsActive)
                {
                    effectStruct.Power *= HealPowerMultiplier;
                    return;
                }
            }
        }

        public void Modify(ref OvertimeHealToApply effectStruct)
        {
            foreach (StatusInstance status in effectStruct.Target.StatusModule.GetAll)
            {
                if (status.EffectType is EffectType.Debuff or EffectType.Poison && status.IsActive)
                {
                    effectStruct.HealPerTime = (OvertimeHealPowerMultiplier * effectStruct.HealPerTime).CeilToUInt();
                    return;
                }
            }
        }
    }
}