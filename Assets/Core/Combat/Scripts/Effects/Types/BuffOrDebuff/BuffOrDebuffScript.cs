using System;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using UnityEngine;
using Utils.Patterns;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
    public record BuffOrDebuffScript(bool Permanent, float BaseDuration, float BaseApplyChance, CombatStat Stat, float BaseDelta) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public float BaseApplyChance { get; protected set; } = BaseApplyChance;
        public CombatStat Stat { get; set; } = Stat;
        public float BaseDelta { get; set; } = BaseDelta;

        public override bool IsPositive => BaseDelta > 0;
        
        public override bool PlaysBarkAppliedOnCaster => EffectType == EffectType.Buff;
        public override bool PlaysBarkAppliedOnEnemy => EffectType == EffectType.Debuff;
        public override bool PlaysBarkAppliedOnAlly => EffectType == EffectType.Buff;

        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null) 
            => new BuffOrDebuffToApply(caster, target, crit, skill, this, BaseDuration, Permanent, BaseApplyChance, Stat, BaseDelta);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            BuffOrDebuffToApply buffOrDebuffStruct = new(caster, target, crit, skill, this, BaseDuration, Permanent, BaseApplyChance, Stat, BaseDelta);
            return ProcessModifiersAndTryApply(buffOrDebuffStruct);
        }

        public static void ProcessModifiers(BuffOrDebuffToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            if (effectStruct.Caster == effectStruct.Target || effectStruct.Delta > 0)
                effectStruct.ApplyChance = 1;
            else
            {
                effectStruct.ApplyChance += applierModule.BaseDebuffApplyChance;
                effectStruct.ApplyChance -= effectStruct.Target.ResistancesModule.GetDebuffResistance();
            }

            if (effectStruct.FromCrit)
                effectStruct.ApplyChance += BonusApplyChanceOnCrit;

            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusReceiverModule receiverModule = effectStruct.Target.StatusModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
                effectStruct.Duration *= DurationMultiplierOnCrit;
        }

        public static StatusResult ProcessModifiersAndTryApply(BuffOrDebuffToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply(ref BuffOrDebuffToApply effectStruct)
        {
            FullCharacterState targetState = effectStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: false);
            
            bool success = Random.value < effectStruct.ApplyChance;
            Option<StatusInstance> option = success ? BuffOrDebuff.CreateInstance(effectStruct.Duration, effectStruct.IsPermanent, 
                effectStruct.Target, effectStruct.Caster, effectStruct.Stat, effectStruct.Delta) : Option.None;

            EffectType effectType = effectStruct.Delta > 0 ? EffectType.Buff : EffectType.Debuff;
            
            if (StatusVFXManager.AssertInstance(out StatusVFXManager statusEffectVFXManager))
                statusEffectVFXManager.Enqueue(StatusCueHandler.FromAppliedStatus(effectStruct.Target, StatusCueHandler.StandardValidator, effectType, option.IsSome));

            return new StatusResult(effectStruct.Caster, effectStruct.Target, success: option.IsSome, statusInstance: option.SomeOrDefault(), generatesInstance: true, effectType);
        }

        public void ApplyEffectWithoutModifying(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill)
        {
            BuffOrDebuffToApply effectStruct = new(caster, target, crit, skill, this, BaseDuration, Permanent, BaseApplyChance, Stat, BaseDelta);
            TryApply(ref effectStruct);
        }

        public override float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
            CharacterState state = target.StateEvaluator.PureEvaluate();
            if (state is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled)
                return 0f;
            
            BuffOrDebuffToApply effectStruct = new(skillStruct.Caster, target, FromCrit: false, skillStruct.Skill, ScriptOrigin: this, BaseDuration,
                                                  Permanent, BaseApplyChance, Stat, BaseDelta);
            ProcessModifiers(effectStruct);
            if (target.Display.IsSome)
            {
                CombatManager combatManager = target.Display.Value.CombatManager;
                switch (Stat)
                {
                    case CombatStat.ArousalApplyChance:
                    {
                        if (state is CharacterState.Downed)
                            return 0f;
                        
                        if (target.AnySkillWithEffect(EffectType.Arousal) == false)
                            return 0f;

                        break;
                    }
                    case CombatStat.Composure:
                    {
                        if (target.LustModule.IsNone)
                            return 0f;

                        bool noSkillsThatTempt = true;
                        foreach (CharacterStateMachine enemy in combatManager.Characters.GetEnemies(target))
                        {
                            if (enemy.StateEvaluator.PureEvaluate() is CharacterState.Defeated || enemy.AnySkillWithEffect(EffectType.Temptation) == false)
                                continue;

                            noSkillsThatTempt = false;
                            break;
                        }

                        if (noSkillsThatTempt)
                            return 0f;

                        break;
                    }
                    case CombatStat.DebuffResistance:
                    {
                        if (state is CharacterState.Downed)
                            return 0f;
                        
                        bool noSkillsWithDebuffs = true;
                        foreach (CharacterStateMachine enemy in combatManager.Characters.GetEnemies(target))
                        {
                            if (enemy.StateEvaluator.PureEvaluate() is CharacterState.Defeated || enemy.AnySkillWithEffect(EffectType.Debuff) == false)
                                continue;

                            noSkillsWithDebuffs = false;
                            break;
                        }

                        if (noSkillsWithDebuffs)
                            return 0f;

                        break;
                    }
                    case CombatStat.DebuffApplyChance:
                    {
                        if (state is CharacterState.Downed)
                            return 0f;
                        
                        if (target.AnySkillWithEffect(EffectType.Debuff) == false)
                            return 0f;
                        
                        break;
                    }
                    case CombatStat.PoisonResistance:
                    {
                        if (state is CharacterState.Downed)
                            return 0f;
                        
                        bool noSkillsWithPoison = true;
                        foreach (CharacterStateMachine enemy in combatManager.Characters.GetEnemies(target))
                        {
                            if (enemy.StateEvaluator.PureEvaluate() is CharacterState.Defeated || enemy.AnySkillWithEffect(EffectType.Poison) == false)
                                continue;

                            noSkillsWithPoison = false;
                            break;
                        }

                        if (noSkillsWithPoison)
                            return 0;

                        break;
                    }
                    case CombatStat.PoisonApplyChance:
                    {
                        if (state is CharacterState.Downed)
                            return 0f;

                        if (target.AnySkillWithEffect(EffectType.Poison) == false)
                            return 0f;
                        
                        break;
                    }
                    case CombatStat.MoveResistance:
                    {
                        if (state is CharacterState.Downed)
                            return 0f;
                        
                        bool noSkillsWithMove = true;
                        foreach (CharacterStateMachine enemy in combatManager.Characters.GetEnemies(target))
                        {
                            if (enemy.StateEvaluator.PureEvaluate() is CharacterState.Defeated || enemy.AnySkillWithEffect(EffectType.Move) == false)
                                continue;

                            noSkillsWithMove = false;
                            break;
                        }

                        if (noSkillsWithMove)
                            return 0;

                        break;
                    }
                    case CombatStat.MoveApplyChance:
                    {
                        if (state is CharacterState.Downed)
                            return 0f;

                        if (target.AnySkillWithEffect(EffectType.Move) == false)
                            return 0f;
                        
                        break;
                    }
                    case CombatStat.StunSpeed:
                    {
                        bool noSkillsWithStun = true;
                        foreach (CharacterStateMachine enemy in combatManager.Characters.GetEnemies(target))
                        {
                            if (enemy.StateEvaluator.PureEvaluate() is CharacterState.Defeated || enemy.AnySkillWithEffect(EffectType.Stun) == false)
                                continue;

                            noSkillsWithStun = false;
                            break;
                        }

                        if (noSkillsWithStun)
                            return 0;
                        break;
                    }
                    case CombatStat.Accuracy:
                    case CombatStat.Dodge:
                    case CombatStat.Resilience:
                    case CombatStat.Speed:
                    case CombatStat.CriticalChance:
                    case CombatStat.DamageMultiplier:
                        if (state is CharacterState.Downed)
                            return 0f;
                        break;
                    default: throw new ArgumentException($"Unhandled stat {Stat}");
                }
            }
            
            float applyChance = Mathf.Clamp(effectStruct.ApplyChance, 0f, 1f);
            float durationMultiplier = effectStruct.IsPermanent ? HeuristicConstants.PermanentMultiplier : effectStruct.Duration * HeuristicConstants.DurationMultiplier;
            float points = effectStruct.Delta * durationMultiplier * HeuristicConstants.BuffOrDebuffMultiplier * applyChance;
            return points;
        }

        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => BaseDelta > 0 ? EffectType.Buff : EffectType.Debuff;
    }
}