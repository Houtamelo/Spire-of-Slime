using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Heal;
using Core.Combat.Scripts.Effects.Types.OvertimeHeal;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Main_Characters.Nema.Combat.Perks.Healer
{
    public class Affection : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            AffectionInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class AffectionInstance : PerkInstance, IHealModifier, IOvertimeHealModifier
    {
        private const int HealPowerMultiplier = 140;
        private const int OvertimeHealPowerMultiplier = 125;

        public AffectionInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public AffectionInstance(CharacterStateMachine owner, [NotNull] AffectionRecord record) : base(owner, record)
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

        [NotNull]
        public override PerkRecord GetRecord() => new AffectionRecord(Key);

        public void Modify([NotNull] ref HealToApply effectStruct)
        {
            foreach (StatusInstance status in effectStruct.Target.StatusReceiverModule.GetAll)
            {
                if (status.EffectType is EffectType.Debuff or EffectType.Poison && status.IsActive)
                {
                    effectStruct.Power = (effectStruct.Power * HealPowerMultiplier) / 100;
                    return;
                }
            }
        }

        public void Modify([NotNull] ref OvertimeHealToApply effectStruct)
        {
            foreach (StatusInstance status in effectStruct.Target.StatusReceiverModule.GetAll)
            {
                if (status.EffectType is EffectType.Debuff or EffectType.Poison && status.IsActive)
                {
                    effectStruct.HealPerSecond = (OvertimeHealPowerMultiplier * effectStruct.HealPerSecond) / 100;
                    return;
                }
            }
        }

        [NotNull] public string SharedId => nameof(AffectionInstance);
        public int Priority => 5;
    }
}