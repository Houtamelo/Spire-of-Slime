using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using ListPool;

namespace Core.Main_Characters.Ethel.Combat.Perks.Bruiser
{
    public class DisruptiveManeuvers : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            DisruptiveManeuversInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record DisruptiveManeuversRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(DisruptiveManeuversRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance(CharacterStateMachine owner, DirectCharacterEnumerator allCharacters) 
            => new DisruptiveManeuversInstance(owner, Key);
    }
    
    public class DisruptiveManeuversInstance : PerkInstance, ISelfAttackedListener
    {
        private const int ApplyChance = 100;
        private static readonly BuffOrDebuffScript Debuff = new(Permanent: false, BaseDuration: default, ApplyChance, CombatStat.Accuracy, BaseDelta: default); // default values are overriden in OnActionComplete()

        public DisruptiveManeuversInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.Events.SelfAttackedListeners.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.Events.SelfAttackedListeners.Remove(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new DisruptiveManeuversRecord(Key);

        private bool _recursionLock;
        
        public void OnSelfAttacked(ref ActionResult actionResult)
        {
            if (_recursionLock)
                return;
            
            if (actionResult.Caster == Owner || actionResult.Caster.PositionHandler.IsLeftSide == Owner.PositionHandler.IsLeftSide)
                return;
            
            Option<ListPool<StatusResult>> statusResults = actionResult.StatusResults;
            if (statusResults.IsNone)
                return;

            foreach (StatusResult statusResult in statusResults.Value)
            {
                Option<StatusInstance> instance = statusResult.StatusInstance;
                if (statusResult.IsFailure || instance.IsNone || instance.Value.IsPositive || statusResult.Caster == Owner ||
                    statusResult.Caster.PositionHandler.IsLeftSide == Owner.PositionHandler.IsLeftSide || instance.Value is not BuffOrDebuff buffOrDebuff || buffOrDebuff.Permanent)
                    continue;

                _recursionLock = true;
                CombatStat countered = buffOrDebuff.Attribute;
                CombatStat toApplyOnTarget = CombatUtils.GetRandomCombatStatExcept(countered);
                CombatStat toApplyOnCaster = CombatUtils.GetRandomCombatStatExcept(countered, toApplyOnTarget);

                BuffOrDebuffToApply targetStruct = (BuffOrDebuffToApply) Debuff.GetStatusToApply(statusResult.Caster, statusResult.Target, false, null);
                targetStruct.Stat = toApplyOnTarget;
                targetStruct.Duration = buffOrDebuff.Duration;
                targetStruct.Delta = buffOrDebuff.GetDelta;
                
                BuffOrDebuffToApply casterStruct = (BuffOrDebuffToApply) Debuff.GetStatusToApply(statusResult.Caster, statusResult.Caster, false, null);
                casterStruct.Stat = toApplyOnCaster;
                casterStruct.Duration = buffOrDebuff.Duration;
                casterStruct.Delta = buffOrDebuff.GetDelta;
                
                BuffOrDebuffScript.ProcessModifiersAndTryApply(targetStruct);
                BuffOrDebuffScript.ProcessModifiersAndTryApply(casterStruct);
                _recursionLock = false;
            }
        }
    }
}