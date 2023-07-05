using System;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Combat.Scripts
{
    public record CharacterRecord(CleanString ScriptKey, Guid Guid,
                                  uint CurrentStamina, uint BaseMaxStamina, uint MaxStamina, float BaseResilience,
                                  float ChargeRemaining, float ChargeInitialDuration, float RecoveryRemaining, float RecoveryInitialDuration,
                                  float StunRemaining, float StunInitialDuration, float TimeSinceStunStarted, uint StunRecoverySteps,
                                  PlanRecord SkillAction, bool IsLeftSide,
                                  float BaseAccuracy, float BaseCriticalChance, float BaseDodge, float BaseSpeed,
                                  float BasePoisonResistance, float BaseDebuffResistance, float BaseMoveResistance, float BaseStunRecoverySpeed,
                                  float BasePower, uint BaseDamageLower, uint BaseDamageUpper,
                                  StatusRecord[] Statuses, PerkRecord[] Perks, bool IsGirl,
                                  float DownedRemaining, float DownedInitialDuration,
                                  bool IsDefeated, bool IsCorpse,
                                  uint Lust, uint OrgasmCount, uint OrgasmLimit, float BaseComposure, ClampedPercentage Temptation,
                                  float BaseArousalApplyChance, float BaseDebuffApplyChance, float BaseMoveApplyChance, float BasePoisonApplyChance)
    {
        
        public static CharacterRecord FromState(CharacterStateMachine character)
        {
            uint currentStamina;
            uint baseMaxStamina;
            uint maxStamina;
            float baseResilience;
            if (character.StaminaModule.TrySome(out IStaminaModule staminaModule))
            {
                currentStamina = staminaModule.GetCurrent();
                baseMaxStamina = staminaModule.BaseMax;
                maxStamina = staminaModule.ActualMax;
                baseResilience = staminaModule.BaseResilience;
            }
            else
            {
                currentStamina = 0;
                baseMaxStamina = 0;
                maxStamina = 0;
                baseResilience = 0;
            }

            IChargeModule chargeModule = character.ChargeModule;
            float chargeRemaining = chargeModule.GetRemaining();
            float chargeInitialDuration = chargeModule.GetInitialDuration();
            
            IRecoveryModule recoveryModule = character.RecoveryModule;
            float recoveryRemaining = recoveryModule.GetRemaining();
            float recoveryInitialDuration = recoveryModule.GetInitialDuration();

            IStunModule stunModule = character.StunModule;
            float stunRemaining = stunModule.GetRemaining();
            float stunInitialDuration = stunModule.GetInitialDuration();
            float timeSinceStunStart = stunModule.GetTimeSinceStunStarted();
            uint stunRecoverySteps = stunModule.StunRecoverySteps;

            IStatsModule statsModule = character.StatsModule;
            float baseAccuracy = statsModule.BaseAccuracy;
            float baseCriticalChance = statsModule.BaseCriticalChance;
            float baseDodge = statsModule.BaseDodge;
            float baseSpeed = statsModule.BaseSpeed;
            float basePower = statsModule.BasePower;
            uint baseDamageLower = statsModule.BaseDamageLower;
            uint baseDamageUpper = statsModule.BaseDamageUpper;
            
            IResistancesModule resistancesModule = character.ResistancesModule;
            float basePoisonResistance = resistancesModule.BasePoisonResistance;
            float baseDebuffResistance = resistancesModule.BaseDebuffResistance;
            float baseMoveResistance = resistancesModule.BaseMoveResistance;
            float baseStunRecoverySpeed = resistancesModule.BaseStunRecoverySpeed;
            
            IStatusApplierModule statusApplierModule = character.StatusApplierModule;
            float baseArousalApplyChance = statusApplierModule.BaseArousalApplyChance;
            float baseDebuffApplyChance = statusApplierModule.BaseDebuffApplyChance;
            float baseMoveApplyChance = statusApplierModule.BaseMoveApplyChance;
            float basePoisonApplyChance = statusApplierModule.BasePoisonApplyChance;

            PlanRecord planRecord = null;
            if (character.SkillModule.PlannedSkill.TrySome(out PlannedSkill plannedSkill))
                PlanRecord.FromInstance(plannedSkill).TrySome(out planRecord);

            bool isGirl;
            uint lust;
            uint orgasmCount;
            uint orgasmLimit;
            float baseComposure;
            ClampedPercentage temptation;
            if (character.LustModule.TrySome(out ILustModule lustModule))
            {
                isGirl = character.LustModule.IsSome;
                lust = lustModule.GetLust();
                orgasmCount = lustModule.GetOrgasmCount();
                orgasmLimit = lustModule.OrgasmLimit;
                baseComposure = lustModule.BaseComposure;
                temptation = lustModule.GetTemptation();
            }
            else
            {
                isGirl = false;
                lust = 0;
                orgasmCount = 0;
                orgasmLimit = 0;
                baseComposure = 0f;
                temptation = 0f;
            }

            float downedRemaining;
            float downedInitialDuration;
            if (character.DownedModule.TrySome(out IDownedModule downedModule))
            {
                downedRemaining = downedModule.GetRemaining();
                downedInitialDuration = downedModule.GetInitialDuration();
            }
            else
            {
                downedRemaining = 0f;
                downedInitialDuration = 0f;
            }

            CharacterState state = character.StateEvaluator.PureEvaluate();
            bool isDefeated = state is CharacterState.Defeated;
            bool isCorpse = state is CharacterState.Corpse;

            using FixedEnumerable<StatusInstance> statusSet = character.StatusModule.GetAll;
            StatusRecord[] statuses = new StatusRecord[statusSet.Length];
            int index = 0;
            foreach (StatusInstance status in statusSet)
            {
                statuses[index] = status.GetRecord();
                index++;
            }
            
            using FixedEnumerable<PerkInstance> perkSet = character.PerksModule.GetAll;
            PerkRecord[] perks = new PerkRecord[perkSet.Length];
            index = 0;
            foreach (PerkInstance perk in perkSet)
            {
                perks[index] = perk.GetRecord();
                index++;
            }

            return new CharacterRecord(character.Script.Key, character.Guid,
                                       currentStamina, baseMaxStamina, maxStamina, baseResilience,
                                       chargeRemaining, chargeInitialDuration, recoveryRemaining, recoveryInitialDuration,
                                       stunRemaining, stunInitialDuration, timeSinceStunStart, stunRecoverySteps,
                                       planRecord, character.PositionHandler.IsLeftSide,
                                       baseAccuracy, baseCriticalChance, baseDodge, baseSpeed,
                                       basePoisonResistance, baseDebuffResistance, baseMoveResistance, baseStunRecoverySpeed,
                                       basePower, baseDamageLower, baseDamageUpper,
                                       statuses, perks, isGirl,
                                       downedRemaining, downedInitialDuration,
                                       isDefeated, isCorpse,
                                       lust, orgasmCount, orgasmLimit, baseComposure, temptation,
                                       baseArousalApplyChance, baseDebuffApplyChance, baseMoveApplyChance, basePoisonApplyChance);
        }
        
        public bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (CharacterDatabase.GetCharacter(ScriptKey).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Character script not found in database: ", ScriptKey.ToString());
                return false;
            }
            
            if (Statuses == null)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Statuses array is null");
                return false;
            }

            for (int index = 0; index < Statuses.Length; index++)
            {
                StatusRecord record = Statuses[index];
                if (record == null)
                {
                    errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Status record at index: ", index.ToString(), " is null");
                    return false;
                }

                if (record.IsDataValid(errors, allCharacters) == false)
                    return false;
            }

            if (Perks == null)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Perks array is null");
                return false;
            }

            for (int index = 0; index < Perks.Length; index++)
            {
                PerkRecord perk = Perks[index];
                if (perk == null)
                {
                    errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Perk record at index: ", index.ToString(), " is null");
                    return false;
                }
                
                if (perk.IsDataValid(errors, allCharacters) == false)
                    return false;
            }

            return true;
        }
    }
}