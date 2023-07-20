using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using JetBrains.Annotations;

// ReSharper disable StringLiteralTypo

namespace Core.Combat.Scripts.Effects.Types.Summon
{
    public record SummonScript(ICharacterScript CharacterToSummon, float PointsMultiplier) : StatusScript
    {
        public ICharacterScript CharacterToSummon { get; protected set; } = CharacterToSummon;
        public float PointsMultiplier { get; protected set; } = PointsMultiplier;

        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null) 
            => new SummonToApply(CharacterToSummon, caster, target, crit, skill, ScriptOrigin: this);

        public static StatusResult ProcessModifiersAndTryApply(SummonToApply summonRecord)
        {
            ProcessModifiers(summonRecord);
            return TryApply(ref summonRecord);
        }

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            SummonToApply effectStruct = new(CharacterToSummon, caster, target, crit, skill, ScriptOrigin: this);
            return ProcessModifiersAndTryApply(effectStruct);
        }

        private static StatusResult TryApply([NotNull] ref SummonToApply effectStruct)
        {
            if (effectStruct.Caster.Display.IsNone)
                return StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: false);
            
            CombatManager combatManager = effectStruct.Caster.Display.Value.CombatManager;
            int count = 0;
            foreach (CharacterStateMachine character in combatManager.Characters.GetOnSide(effectStruct.Caster.PositionHandler.IsLeftSide))
            {
                if (character.StateEvaluator.PureEvaluate() is not CharacterState.Defeated)
                    count++;
            }

            if (count >= 4)
                StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: false);
            
            bool success = combatManager.Characters.SummonFromSkill(effectStruct);
            return new StatusResult(effectStruct.Caster, effectStruct.Target, success, statusInstance: null, generatesInstance: false, EffectType.Summon);
        }

        public static void ProcessModifiers(SummonToApply effectStruct)
        {
            effectStruct.Caster.StatusApplierModule.ModifyEffectApplying(ref effectStruct);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, [NotNull] CharacterStateMachine target)
        {
            if (target.Display.IsNone)
                return 0;

            CombatManager combatManager = target.Display.Value.CombatManager;
            
            int count = 0;
            foreach (CharacterStateMachine character in combatManager.Characters.GetOnSide(skillStruct.Caster.PositionHandler.IsLeftSide))
            {
                if (character.StateEvaluator.PureEvaluate() is not CharacterState.Defeated)
                    count++;
            }

            if (count >= 4)

                //potential bug 
                return -100000;

            SummonToApply record = new(CharacterToSummon, skillStruct.Caster, target, FromCrit: false, skillStruct.Skill, ScriptOrigin: this);
            ProcessModifiers(record);
            
            float resistancesMultiplier = HeuristicConstants.ProcessSummonResistance(record.CharacterToSummon.PoisonResistance);
            resistancesMultiplier += HeuristicConstants.ProcessSummonResistance(record.CharacterToSummon.DebuffResistance);
            resistancesMultiplier += HeuristicConstants.ProcessSummonResistance(record.CharacterToSummon.MoveResistance);
            resistancesMultiplier /= 3f;

            float staminaMultiplier = record.CharacterToSummon.Stamina * HeuristicConstants.Summon_StaminaMultiplier * (1f / (1f - ((record.CharacterToSummon.Resilience / 100f) * HeuristicConstants.Summon_ResilienceMultiplier)));
            float speedMultiplier = record.CharacterToSummon.Speed / 100f;
            float stunMitigationMultiplier = HeuristicConstants.ProcessSummonResistance(record.CharacterToSummon.StunMitigation);
            float accuracyMultiplier = ((record.CharacterToSummon.Accuracy / 100f) * HeuristicConstants.Summon_AccuracyMultiplier) + 1;
            float criticalChanceMultiplier = ((record.CharacterToSummon.CriticalChance / 100f) * HeuristicConstants.Summon_CriticalMultiplier) + 1;
            float dodgeMultiplier = ((record.CharacterToSummon.Dodge / 100f) * HeuristicConstants.Summon_DodgeMultiplier) + 1;
            float averageDamageMultiplier = HeuristicConstants.Summon_DamageMultiplier * (record.CharacterToSummon.Damage.lower + record.CharacterToSummon.Damage.upper) / 2f;
            
            return staminaMultiplier * resistancesMultiplier * speedMultiplier * stunMitigationMultiplier * accuracyMultiplier * criticalChanceMultiplier * dodgeMultiplier * averageDamageMultiplier * PointsMultiplier;
        }
        
        public override EffectType EffectType => EffectType.Summon;
        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        
        public override bool IsPositive => true;

        public override bool PlaysBarkAppliedOnCaster => true;
        public override bool PlaysBarkAppliedOnEnemy => true;
        public override bool PlaysBarkAppliedOnAlly => false;
    }
}