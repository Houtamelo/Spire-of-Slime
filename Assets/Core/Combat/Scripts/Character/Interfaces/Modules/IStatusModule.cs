using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Utils.Collections;

namespace Core.Combat.Scripts.Interfaces.Modules
{
    public interface IStatusModule : IStatusReceiverModule
    {
        FixedEnumerable<StatusInstance> GetAll { get; }
        /// <summary> Related statuses are the ones who depend on one or more characters (like how guard depends on caster and target) </summary>
        FixedEnumerable<StatusInstance> GetAllRelated { get; }
        bool HasActiveStatusOfType(EffectType effectType);

        void AddStatus(StatusInstance statusInstance, CharacterStateMachine caster);

        void RemoveStatus(StatusInstance effectInstance);
        bool DeactivateStatusByType(EffectType effectType);
        void RemoveAll();

        void Tick(float timeStep);

        void ShowStatusTooltip(string description);
        void HideStatusTooltip();
    }
}