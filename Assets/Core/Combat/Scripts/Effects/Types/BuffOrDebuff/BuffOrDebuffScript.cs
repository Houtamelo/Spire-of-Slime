using System;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
    public record BuffOrDebuffScript(bool Permanent, TSpan BaseDuration, int BaseApplyChance, CombatStat Stat, int BaseDelta) : StatusScriptDurationBased(Permanent, BaseDuration)
    {
        public int BaseApplyChance { get; protected set; } = BaseApplyChance;
        public CombatStat Stat { get; set; } = Stat;
        public int BaseDelta { get; set; } = BaseDelta;

        [NotNull]
        public override StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null) 
            => new BuffOrDebuffToApply(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent, BaseApplyChance, Stat, BaseDelta);

        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            BuffOrDebuffToApply buffOrDebuffStruct = new(caster, target, crit, skill, ScriptOrigin: this, BaseDuration, Permanent, BaseApplyChance, Stat, BaseDelta);
            return ProcessModifiersAndTryApply(buffOrDebuffStruct);
        }

        public static void ProcessModifiers(BuffOrDebuffToApply effectStruct)
        {
            IStatusApplierModule applierModule = effectStruct.Caster.StatusApplierModule;
            if (effectStruct.Caster == effectStruct.Target || effectStruct.Delta > 0)
            {
                effectStruct.ApplyChance = 100;
            }
            else
            {
                effectStruct.ApplyChance += applierModule.BaseDebuffApplyChance;
                effectStruct.ApplyChance -= effectStruct.Target.ResistancesModule.GetDebuffResistance();
            }

            if (effectStruct.FromCrit)
                effectStruct.ApplyChance += BonusApplyChanceOnCrit;

            applierModule.ModifyEffectApplying(ref effectStruct);
            
            IStatusReceiverModule receiverModule = effectStruct.Target.StatusReceiverModule;
            receiverModule.ModifyEffectReceiving(ref effectStruct);
            
            if (effectStruct.FromCrit)
            {
                TSpan duration = effectStruct.Duration;
                duration.Multiply(DurationMultiplierOnCrit);
                effectStruct.Duration = duration;
            }
        }

        public static StatusResult ProcessModifiersAndTryApply(BuffOrDebuffToApply effectStruct)
        {
            ProcessModifiers(effectStruct);
            return TryApply(ref effectStruct);
        }

        private static StatusResult TryApply([NotNull] ref BuffOrDebuffToApply effectStruct)
        {
            FullCharacterState targetState = effectStruct.Target.StateEvaluator.FullPureEvaluate();
            if (targetState.Defeated || targetState.Corpse || targetState.Grappled)
                return StatusResult.Failure(effectStruct.Caster, effectStruct.Target, generatesInstance: false);
            
            bool success = Save.Random.Next(100) < effectStruct.ApplyChance;
            Option<StatusInstance> option = success ? BuffOrDebuff.CreateInstance(effectStruct.Duration, effectStruct.Permanent, 
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

        public override float ComputePoints(ref SkillStruct skillStruct, [NotNull] CharacterStateMachine target)
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
                    case CombatStat.StunMitigation:
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
            
            float applyChancePercentage = Mathf.Clamp01(effectStruct.ApplyChance / 100f);
            float deltaPercentage = (effectStruct.Delta / 100f);
            float durationMultiplier = effectStruct.Permanent ? HeuristicConstants.PermanentMultiplier : (effectStruct.Duration.FloatSeconds * HeuristicConstants.DurationMultiplier);
            float points = deltaPercentage * durationMultiplier * HeuristicConstants.BuffOrDebuffMultiplier * applyChancePercentage;
            return points;
        }

        [NotNull]
        public override string Description => StatusScriptDescriptions.Get(this);
        public override EffectType EffectType => BaseDelta > 0 ? EffectType.Buff : EffectType.Debuff;
        
        public override bool IsPositive => BaseDelta > 0;
        
        public override bool PlaysBarkAppliedOnCaster => EffectType == EffectType.Buff;
        public override bool PlaysBarkAppliedOnEnemy => EffectType == EffectType.Debuff;
        public override bool PlaysBarkAppliedOnAlly => EffectType == EffectType.Buff;
    }
}