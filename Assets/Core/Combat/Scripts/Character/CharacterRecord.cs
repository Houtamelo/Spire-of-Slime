using System;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Combat.Scripts
{
    public record CharacterRecord(CleanString ScriptKey, Guid Guid,
                                  StaminaRecord StaminaModule,
                                  ChargeRecord ChargeModule, RecoveryRecord RecoveryModule,
                                  StunRecord StunModule, DownedRecord DownedModule,
                                  SkillModuleRecord SkillModule, AIRecord AIModule, EventsHandlerRecord EventsModule,
                                  PositionHandlerRecord PositionModule,
                                  StatsRecord StatsModule,
                                  ResistancesRecord ResistancesModule,
                                  LustRecord LustModule,
                                  StatusReceiverRecord StatusReceiverModule, StatusApplierRecord StatusApplierModule,
                                  PerksModuleRecord PerksModule,
                                  StateEvaluatorRecord StateEvaluatorModule)
    {
        
        [NotNull]
        public static CharacterRecord FromState([NotNull] CharacterStateMachine character)
        {
            StaminaRecord stamina = character.StaminaModule.TrySome(out IStaminaModule staminaModule) ? staminaModule.GetRecord() : null;
            ChargeRecord charge = character.ChargeModule.GetRecord();
            RecoveryRecord recovery = character.RecoveryModule.GetRecord();
            StunRecord stun = character.StunModule.GetRecord();
            DownedRecord downed = character.DownedModule.TrySome(out IDownedModule downedModule) ? downedModule.GetRecord() : null;
            SkillModuleRecord skillModule = character.SkillModule.GetRecord();
            AIRecord aiModule = character.AIModule.GetRecord();
            EventsHandlerRecord eventsRecord = character.Events.GetRecord();
            PositionHandlerRecord position = character.PositionHandler.GetRecord();
            StatsRecord stats = character.StatsModule.GetRecord();
            ResistancesRecord resistances = character.ResistancesModule.GetRecord();
            LustRecord lust = character.LustModule.TrySome(out ILustModule lustModule) ? lustModule.GetRecord() : null;
            StatusReceiverRecord statusReceiver = character.StatusReceiverModule.GetRecord();
            StatusApplierRecord statusApplier = character.StatusApplierModule.GetRecord();
            PerksModuleRecord perks = character.PerksModule.GetRecord();
            StateEvaluatorRecord stateEvaluator = character.StateEvaluator.GetRecord();

            return new CharacterRecord(character.Script.Key, character.Guid, stamina, charge, recovery, stun, downed, skillModule, aiModule, eventsRecord,
                                       position, stats, resistances, lust, statusReceiver, statusApplier, perks, stateEvaluator);
        }
        
        public bool IsDataValid(StringBuilder errors, CharacterRecord[] allCharacters)
        {
            if (CharacterDatabase.GetCharacter(ScriptKey).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Character script not found in database: ", ScriptKey.ToString());
                return false;
            }

            if (StaminaModule != null && StaminaModule.IsDataValid(errors, allCharacters) == false)
                return false;

            if (ChargeModule == null || ChargeModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Charge module is null");
                return false;
            }
            
            if (RecoveryModule == null || RecoveryModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Recovery module is null");
                return false;
            }
            
            if (StunModule == null || StunModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Stun module is null");
                return false;
            }

            if (DownedModule != null && DownedModule.IsDataValid(errors, allCharacters) == false)
                return false;

            if (AIModule == null || AIModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". AI module is null");
                return false;
            }

            if (EventsModule == null || EventsModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Events module is null");
                return false;
            }
            
            if (PositionModule == null || PositionModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Position module is null");
                return false;
            }
            
            if (StatsModule == null || StatsModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Stats module is null");
                return false;
            }
            
            if (ResistancesModule == null || ResistancesModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Resistances module is null");
                return false;
            }

            if (LustModule != null && LustModule.IsDataValid(errors, allCharacters) == false)
                return false;
            
            if (StatusReceiverModule == null || StatusReceiverModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Status receiver module is null");
                return false;
            }
            
            if (StatusApplierModule == null || StatusApplierModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Status applier module is null");
                return false;
            }
            
            if (PerksModule == null || PerksModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Perks module is null");
                return false;
            }
            
            if (StateEvaluatorModule == null || StateEvaluatorModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". State evaluator module is null");
                return false;
            }

            if (SkillModule == null || SkillModule.IsDataValid(errors, allCharacters) == false)
            {
                errors.AppendLine("Invalid ", nameof(CharacterRecord), ". Skill module is null");
                return false;
            }
            
            return true;
        }
    }
}