using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using ListPool;
using UnityEngine;

namespace Core.Main_Characters.Ethel.Combat.Perks.Crit
{
    public class StaggeringForce : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            StaggeringForceInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record StaggeringForceRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(StaggeringForceRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            StaggeringForceInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class StaggeringForceInstance : PerkInstance, ISkillModifier
    {
        public string SharedId => nameof(StaggeringForceInstance);
        public int Priority => 0;

        private static readonly CritOnlyBuffOrDebuffScript ComposureDebuff = new(false, Duration, ApplyChance, CombatStat.Composure, Modifier);
        private static readonly CritOnlyBuffOrDebuffScript ResilienceDebuff = new(false, Duration, ApplyChance, CombatStat.Resilience, Modifier);
        private static readonly CritOnlyBuffOrDebuffScript DebuffResistanceDebuff = new(false, Duration, ApplyChance, CombatStat.DebuffResistance, Modifier);
        private static readonly CritOnlyBuffOrDebuffScript MoveResistanceDebuff = new(false, Duration, ApplyChance, CombatStat.MoveResistance, Modifier);
        private static readonly CritOnlyBuffOrDebuffScript PoisonResistanceDebuff = new(false, Duration, ApplyChance, CombatStat.PoisonResistance, Modifier);
        private static readonly CritOnlyBuffOrDebuffScript StunSpeedDebuff = new(false, Duration, ApplyChance, CombatStat.StunSpeed, Modifier);

        private const float Modifier = -0.1f;
        private const float Duration = 4f;
        private const int ApplyChance = 1;

        public StaggeringForceInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public StaggeringForceInstance(CharacterStateMachine owner, StaggeringForceRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.SkillModule.SkillModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SkillModule.SkillModifiers.Remove(this);
        }

        public override PerkRecord GetRecord() => new StaggeringForceRecord(Key);

        public void Modify(ref SkillStruct skillStruct)
        {
            ref ValueListPool<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            ref TargetProperties targetProperty = ref targetProperties[0];
            if (skillStruct.Skill.AllowAllies || targetProperties.Count == 0 || skillStruct.Caster.PositionHandler.IsLeftSide == targetProperty.Target.PositionHandler.IsLeftSide)
                return;

            ref ValueListPool<IActualStatusScript> targetEffects = ref skillStruct.TargetEffects;
            targetEffects.Add(ComposureDebuff);
            targetEffects.Add(ResilienceDebuff);
            targetEffects.Add(DebuffResistanceDebuff);
            targetEffects.Add(MoveResistanceDebuff);
            targetEffects.Add(PoisonResistanceDebuff);
            targetEffects.Add(StunSpeedDebuff);
            //targetEffects.Add(ArousalResistanceDebuff);

            #region Assertion

#if UNITY_EDITOR
            Debug.Assert(skillStruct.TargetEffects.Contains(ComposureDebuff),         "ComposureDebuff not added");
            Debug.Assert(skillStruct.TargetEffects.Contains(ResilienceDebuff),        "ResilienceDebuff not added");
            Debug.Assert(skillStruct.TargetEffects.Contains(DebuffResistanceDebuff),  "DebuffResistanceDebuff not added");
            Debug.Assert(skillStruct.TargetEffects.Contains(MoveResistanceDebuff),    "MoveResistanceDebuff not added");
            Debug.Assert(skillStruct.TargetEffects.Contains(PoisonResistanceDebuff),  "PoisonResistanceDebuff not added");
            Debug.Assert(skillStruct.TargetEffects.Contains(StunSpeedDebuff),         "StunSpeedDebuff not added");
            //Debug.Assert(skillStruct.TargetEffects.Contains(ArousalResistanceDebuff), "ArousalResistanceDebuff not added");
#endif

            #endregion
        }
    }
}