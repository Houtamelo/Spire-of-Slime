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
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;

namespace Core.Main_Characters.Ethel.Combat.Perks.Crit
{
    public class StaggeringForce : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            StaggeringForceInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class StaggeringForceInstance : PerkInstance, ISkillModifier
    {
        private static readonly TSpan Duration = TSpan.FromSeconds(4.0);
        private const int Modifier = -10;
        private const int ApplyChance = 100;

        private static readonly CritOnlyBuffOrDebuffScript ComposureDebuff = new(Permanent: false, Duration, ApplyChance, CombatStat.Composure, BaseDelta: Modifier);
        private static readonly CritOnlyBuffOrDebuffScript ResilienceDebuff = new(Permanent: false, Duration, ApplyChance, CombatStat.Resilience, BaseDelta: Modifier);
        private static readonly CritOnlyBuffOrDebuffScript DebuffResistanceDebuff = new(Permanent: false, Duration, ApplyChance, CombatStat.DebuffResistance, BaseDelta: Modifier);
        private static readonly CritOnlyBuffOrDebuffScript MoveResistanceDebuff = new(Permanent: false, Duration, ApplyChance, CombatStat.MoveResistance, BaseDelta: Modifier);
        private static readonly CritOnlyBuffOrDebuffScript PoisonResistanceDebuff = new(Permanent: false, Duration, ApplyChance, CombatStat.PoisonResistance, BaseDelta: Modifier);
        private static readonly CritOnlyBuffOrDebuffScript StunMitigationDebuff = new(Permanent: false, Duration, ApplyChance, CombatStat.StunMitigation, BaseDelta: Modifier);

        public StaggeringForceInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public StaggeringForceInstance(CharacterStateMachine owner, [NotNull] StaggeringForceRecord record) : base(owner, record)
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

        [NotNull]
        public override PerkRecord GetRecord() => new StaggeringForceRecord(Key);

        public void Modify(ref SkillStruct skillStruct)
        {
            ref CustomValuePooledList<TargetProperties> targetProperties = ref skillStruct.TargetProperties;
            
            if (skillStruct.Skill.IsPositive || targetProperties.Count == 0 || skillStruct.Caster.PositionHandler.IsLeftSide == targetProperties[0].Target.PositionHandler.IsLeftSide)
                return;

            ref CustomValuePooledList<IActualStatusScript> targetEffects = ref skillStruct.TargetEffects;
            targetEffects.Add(ComposureDebuff);
            targetEffects.Add(ResilienceDebuff);
            targetEffects.Add(DebuffResistanceDebuff);
            targetEffects.Add(MoveResistanceDebuff);
            targetEffects.Add(PoisonResistanceDebuff);
            targetEffects.Add(StunMitigationDebuff);
        }

        [NotNull]
        public string SharedId => nameof(StaggeringForceInstance);
        public int Priority => 0;
    }
}