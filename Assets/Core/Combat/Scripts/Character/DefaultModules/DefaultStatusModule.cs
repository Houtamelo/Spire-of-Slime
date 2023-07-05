using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Collections;
using Core.Utils.Extensions;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultStatusModule : DefaultStatusReceiverModule, IStatusModule
    {
        private readonly CharacterStateMachine _owner;

        public DefaultStatusModule(CharacterStateMachine owner) => _owner = owner;

        /// <summary> Be careful while accessing this - always enumerate with Fixed </summary>
        private readonly HashSet<StatusInstance> _statuses = new();
        public FixedEnumerable<StatusInstance> GetAll => _statuses.FixedEnumerate();
        
        /// <summary> Be careful while accessing this - always enumerate with Fixed </summary>
        private readonly HashSet<StatusInstance> _relatedStatuses = new();
        public FixedEnumerable<StatusInstance> GetAllRelated => _relatedStatuses.FixedEnumerate();

        public bool HasActiveStatusOfType(EffectType effectType)
        {
            foreach (StatusInstance status in GetAll)
                if (status.EffectType == effectType && status.IsDeactivated == false)
                    return true;

            return false;
        }

        public void AddStatus(StatusInstance statusInstance, CharacterStateMachine caster)
        {
            if (statusInstance.EffectType.OnlyOneAllowed())
                DeactivateStatusByType(statusInstance.EffectType);
            
            StatusResult result = StatusResult.Success(caster, _owner, statusInstance, true, statusInstance.EffectType);
            _statuses.Add(statusInstance);
            
            caster.Events.OnStatusApplied(ref result);
            _owner.Events.OnStatusReceived(ref result);

            if (statusInstance.IsDeactivated || statusInstance.EffectType.HasIcon() == false)
                return;
            
            if (_owner.Display.AssertSome(out CharacterDisplay display))
                display.CreateIconForStatus(statusInstance);
        }

        public void RemoveStatus(StatusInstance effectInstance)
        {
            _statuses.Remove(effectInstance);
            if (_owner.Display.AssertSome(out CharacterDisplay display))
                display.StatusIconRemoved(effectInstance);
        }

        public bool DeactivateStatusByType(EffectType effectType)
        {
            bool found = false;
            foreach (StatusInstance statusInstance in GetAll)
            {
                if (statusInstance.EffectType != effectType)
                    continue;
                
                statusInstance.RequestDeactivation();
                found = true;
            }
            
            return found;
        }

        public void RemoveAll()
        {
            foreach (StatusInstance status in GetAll) 
                status.RequestDeactivation();

            _statuses.Clear();
        }

        public override void TrackRelatedStatus(StatusInstance statusInstance) => _relatedStatuses.Add(statusInstance);

        public override void UntrackRelatedStatus(StatusInstance statusInstance) => _relatedStatuses.Remove(statusInstance);

        public void Tick(float timeStep)
        {
            foreach (StatusInstance status in GetAll)
                status.Tick(timeStep);
        }

        public void ShowStatusTooltip(string description)
        {
            if (_owner.Display.TrySome(out CharacterDisplay display))
                display.ShowStatusTooltip(description);
        }

        public void HideStatusTooltip()
        {
            if (_owner.Display.TrySome(out CharacterDisplay display))
                display.HideStatusTooltip();
        }
    }
}