using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Action;
using Core.Save_Management.SaveObjects;
using Core.Utils.Collections;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;

namespace Core.Combat.Scripts.Skills
{
	public static class SkillCalculator
	{
        public static ActionResult DoToTarget(ref SkillStruct skillStruct, in ReadOnlyProperties targetProperties, bool isRiposte)
		{
			CharacterStateMachine target = targetProperties.Target;
			if (target.Display.AssertSome(out DisplayModule targetDisplay) == false
			 || skillStruct.Caster.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled
			 || target.StateEvaluator.PureEvaluate() is CharacterState.Defeated)
				return ActionResult.FromMiss(skillStruct.Skill, skillStruct.Caster, target);

			CharacterState stateBeforeAction = target.StateEvaluator.PureEvaluate();
			bool eligibleForHitAnimation = 
				isRiposte == false 
                && skillStruct.Skill.IsNegative() 
                && stateBeforeAction is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled);

			Option<int> hitChance = FinalHitChance(ref skillStruct, targetProperties);
			
			if (hitChance.IsSome && Save.Random.Next(100) >= hitChance.Value)
			{
				CombatCueOptions options = CombatCueOptions.Default(skillStruct.Skill.IsNegative() ? "Dodge!" : "Miss!", ColorReferences.Accuracy, targetDisplay);
				
				if (CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager))
					cueManager.EnqueueAboveCharacter(ref options, targetDisplay);
                
				skillStruct.Caster.PlayBark(BarkType.DodgedAttack);
				ActionResult missResult = ActionResult.FromMiss(skillStruct.Skill, skillStruct.Caster, target);
				target.WasTargetedDuringSkillAnimation(missResult, eligibleForHitAnimation, stateBeforeAction);
				return missResult;
			}

			Option<int> criticalChance = FinalCriticalChance(ref skillStruct, targetProperties);
			bool crit = criticalChance.IsSome && Save.Random.Next(100) < criticalChance.Value;
			
			ListPool<StatusResult> statusResults = new();
			
            DoEffects(ref skillStruct, target, crit, statusResults);

            if (crit)
				DoOnCrit(skillStruct, targetDisplay, target);

			Option<int> damageDealt = DoDamage(ref skillStruct, targetProperties, target, crit);

			ActionResult result = ActionResult.FromHit(skillStruct.Skill, skillStruct.Caster, target, damageDealt, crit, statusResults);
			target.WasTargetedDuringSkillAnimation(result, eligibleForHitAnimation, stateBeforeAction);
			return result;
		}

        private static Option<int> DoDamage(ref SkillStruct skillStruct, ReadOnlyProperties targetProperties, CharacterStateMachine target, bool crit)
        {
	        Option<int> damageDealt;

	        if (targetProperties.Power.IsSome && target.StaminaModule.IsSome && FinalDamage(ref skillStruct, targetProperties, crit).TrySome(out (int lower, int upper) damage))
	        {
		        int amplitude = damage.upper - damage.lower;
		        damageDealt = damage.lower + Save.Random.Next(max: amplitude + 1);
	        }
	        else
	        {
		        damageDealt = Option.None;
	        }

	        if (damageDealt.IsSome)
		        target.StaminaModule.Value.ReceiveDamage(damageDealt.Value, DamageType.Brute, skillStruct.Caster);

	        return damageDealt;
        }

        private static void DoEffects(ref SkillStruct skillStruct, CharacterStateMachine target, bool crit, ListPool<StatusResult> statusResults)
        {
	        ref CustomValuePooledList<IActualStatusScript> targetEffects = ref skillStruct.TargetEffects;
	        
	        if (skillStruct.Skill.IsPositive && target.PositionHandler.IsLeftSide == skillStruct.Caster.PositionHandler.IsLeftSide)
	        {
		        bool buffBark = false;

		        foreach (IActualStatusScript effectScript in targetEffects)
		        {
			        StatusResult statusResult = effectScript.ApplyEffect(skillStruct.Caster, target, crit, skillStruct.Skill);
			        statusResults.Add(statusResult);

			        if (statusResult.IsSuccess && effectScript.PlaysBarkAppliedOnAlly)
				        buffBark = true;
		        }

		        if (buffBark)
			        skillStruct.Caster.PlayBark(BarkType.BuffOrHealAlly);
	        }
	        else if (skillStruct.Skill.IsPositive == false && target.PositionHandler.IsLeftSide != skillStruct.Caster.PositionHandler.IsLeftSide)
	        {
		        bool debuffBark = false;

		        foreach (IActualStatusScript effectScript in targetEffects)
		        {
			        StatusResult statusResult = effectScript.ApplyEffect(skillStruct.Caster, target, crit, skillStruct.Skill);
			        statusResults.Add(statusResult);

			        if (statusResult.IsSuccess && effectScript.PlaysBarkAppliedOnEnemy)
				        debuffBark = true;
		        }

		        if (debuffBark)
		        {
			        skillStruct.Caster.PlayBark(BarkType.DealtDebuff);
			        target.PlayBark(BarkType.ReceivedDebuff);
		        }
	        }
        }

        private static void DoOnCrit(SkillStruct skillStruct, [NotNull] DisplayModule targetDisplay, CharacterStateMachine target)
        {
	        if (targetDisplay.GetCuePosition().TrySome(out Vector3 cuePosition) && CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager))
	        {
		        CombatCueOptions options = new(canShowOnTopOfOthers: true, text: "Critical!", ColorReferences.CriticalChance, cuePosition + new Vector3(x: 0, y: 0.1f),
			         speed: Vector3.zero, duration: 1.5f, fontSize: CombatCueOptions.DefaultFontSize * 1.5f, fadeOnComplete: true, shake: true);

		        cueManager.IndependentCue(options: ref options);
	        }

	        if (skillStruct.Skill.IsPositive)
		        return;

	        CombatManager combatManager = targetDisplay.CombatManager;
	        skillStruct.Caster.PlayBark(BarkType.DealtCrit);
	        target.PlayBark(BarkType.ReceivedCrit);

	        foreach (CharacterStateMachine ally in combatManager.Characters.GetOnSide(skillStruct.Caster))
	        {
		        if (ally.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Downed and not CharacterState.Grappled)
			        ally.PlayBark(BarkType.AllyDealtCrit);
	        }

	        foreach (CharacterStateMachine enemy in combatManager.Characters.GetOnSide(skillStruct.Caster.PositionHandler.IsRightSide))
	        {
		        if (enemy.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Downed and not CharacterState.Grappled)
			        enemy.PlayBark(BarkType.AlyReceivedCrit);
	        }
        }

        public static ActionResult DoToCaster(ref SkillStruct skillStruct)
        {
	        ref CustomValuePooledList<IActualStatusScript> casterEffects = ref skillStruct.CasterEffects;
	        if (casterEffects.Count == 0)
		        return ActionResult.FromHit(skillStruct.Skill, skillStruct.Caster, target: skillStruct.Caster, damageDealt: Option<int>.None, crit: false, Option<ListPool<StatusResult>>.None);
	        
	        Option<int> criticalChance = skillStruct.Skill.CriticalChance;
	        bool isCrit = criticalChance.IsSome && Save.Random.Next(100) < criticalChance.Value;

	        ListPool<StatusResult> statusResults = new();

	        bool anySuccessBarks = false;

	        for (int i = 0; i < casterEffects.Count; i++)
	        {
		        IActualStatusScript effectScript = casterEffects[i];
                StatusToApply effectRecord = effectScript.GetStatusToApply(skillStruct.Caster, target: skillStruct.Caster, isCrit, skillStruct.Skill);
                
		        StatusResult statusResult = effectRecord.ApplyEffect();
		        statusResults.Add(statusResult);
		        
		        if (statusResult.IsSuccess && effectScript.PlaysBarkAppliedOnCaster)
			        anySuccessBarks = true;
	        }
			
	        if (anySuccessBarks)
		        skillStruct.Caster.PlayBark(BarkType.BuffOrHealSelf);
            
	        if (isCrit 
	         && CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager)
	         && skillStruct.Caster.Display.TrySome(out DisplayModule casterDisplay)
	         && casterDisplay.GetCuePosition().TrySome(out Vector3 cuePosition))
	        {
		        CombatCueOptions options = new(canShowOnTopOfOthers: true, text: "Critical!", ColorReferences.CriticalChance, position: cuePosition + new Vector3(x: 0, y: 0.1f), 
		                                       speed: Vector3.up, duration: 1.5f, fontSize: CombatCueOptions.DefaultFontSize * 1.5f, fadeOnComplete: true, shake: true);
                
		        cueManager.IndependentCue(ref options);
	        }

	        return ActionResult.FromHit(skillStruct.Skill, skillStruct.Caster, target: skillStruct.Caster, damageDealt: 0, isCrit, statusResults);
        }

        [System.Diagnostics.Contracts.Pure]
        public static Option<int> FinalCriticalChance(ref SkillStruct skillStruct, in ReadOnlyProperties targetProperties)
        {
	        if (targetProperties.CriticalChanceModifier.IsNone)
		        return Option.None;
            
	        int criticalChance = targetProperties.CriticalChanceModifier.Value + skillStruct.Caster.StatsModule.GetCriticalChance();
	        criticalChance = Mathf.Clamp(criticalChance, min: 0, max: 100);
	        return criticalChance;
        }

        [System.Diagnostics.Contracts.Pure]
        public static Option<int> FinalCriticalChance(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
	        Option<ReadOnlyProperties> targetProperties = skillStruct.GetReadOnlyProperties(target);
	        if (targetProperties.IsNone)
	        {
		        Debug.LogWarning($"Skill struct does not have the following target: {target.Script.CharacterName}", target.Display.SomeOrDefault());
		        return Option.None;
	        }
            
	        return FinalCriticalChance(ref skillStruct, targetProperties.Value);
        }

        [System.Diagnostics.Contracts.Pure]
        public static Option<int> FinalHitChance(ref SkillStruct skillStruct, in ReadOnlyProperties targetProperties)
        {
	        if (targetProperties.AccuracyModifier.IsNone)
		        return Option.None;
            
	        int hitChance = targetProperties.AccuracyModifier.Value + skillStruct.Caster.StatsModule.GetAccuracy() - targetProperties.Target.StatsModule.GetDodge();
	        hitChance = Mathf.Clamp(hitChance, min: 0, max: 100);
	        return hitChance;
        }

        [System.Diagnostics.Contracts.Pure]
        public static Option<int> FinalHitChance(ref SkillStruct skillStruct, CharacterStateMachine target)
        {
	        Option<ReadOnlyProperties> targetProperties = skillStruct.GetReadOnlyProperties(target);
	        if (targetProperties.IsNone)
	        {
		        Debug.LogWarning($"Skill struct does not have the following target: {target.Script.CharacterName}", target.Display.SomeOrDefault());
		        return Option.None;
	        }
            
	        return FinalHitChance(ref skillStruct, targetProperties.Value);
        }

        [System.Diagnostics.Contracts.Pure]
        public static Option<(int lowerDamage, int upperDamage)> FinalDamage(ref SkillStruct skillStruct, in ReadOnlyProperties targetProperties, bool crit)
        {
	        if (targetProperties.Target.StaminaModule.IsNone)
		        return Option.None;
            
	        if (targetProperties.Target.StateEvaluator.PureEvaluate() is CharacterState.Downed or CharacterState.Corpse)
		        return Option.None;
            
	        if (targetProperties.Power.TrySome(out int skillPower) == false)
		        return Option.None;

	        targetProperties.ResiliencePiercingModifier.TrySome(out int resilienceReduction);

	        (int lower, int upper) damage = skillStruct.Caster.StatsModule.GetBaseDamageRaw();
	        int characterPower = skillStruct.Caster.StatsModule.GetPower();
	        int targetResilience = Mathf.Clamp(targetProperties.Target.StaminaModule.Value.GetResilience() - resilienceReduction, 0, 100);

	        int effectivePower = characterPower * skillPower;

	        damage.lower = (damage.lower * effectivePower * (100 - targetResilience)) / 1000000;
	        damage.upper = (damage.upper * effectivePower * (100 - targetResilience)) / 1000000;

	        damage = IStatsModule.ClampRawDamage(damage.lower, damage.upper);

	        if (crit)
	        {
		        damage.upper = (damage.upper * 150) / 100;
		        damage.lower = damage.upper;
	        }
            
	        return damage;
        }

        [System.Diagnostics.Contracts.Pure]
        public static Option<(int lowerDamage, int upperDamage)> FinalDamage(ref SkillStruct skillStruct, CharacterStateMachine target, bool crit)
        {
	        Option<ReadOnlyProperties> targetProperties = skillStruct.GetReadOnlyProperties(target);
	        if (targetProperties.IsNone)
	        {
		        Debug.LogWarning($"Skill struct does not have the following target: {target.Script.CharacterName}", target.Display.SomeOrDefault());
		        return Option.None;
	        }
            
	        return FinalDamage(ref skillStruct, targetProperties.Value, crit);
        }
	}
}