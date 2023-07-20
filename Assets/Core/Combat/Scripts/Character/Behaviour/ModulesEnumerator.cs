using Core.Combat.Scripts.Behaviour.Modules;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Behaviour
{
    public ref struct ModulesEnumerator
    {
        private readonly IAIModule _aiModule;
        private readonly IChargeModule _chargeModule;
        private readonly IRecoveryModule _recoveryModule;
        private readonly IStunModule _stunModule;
        private readonly IStatusReceiverModule _statusModule;
        private readonly ISkillModule _skillModule;
        private readonly IPositionHandler _positionHandler;
        private readonly IStateEvaluator _stateEvaluator;
        private readonly IStatsModule _statsModule;
        private readonly IResistancesModule _resistancesModule;
        private readonly IStatusApplierModule _statusApplierModule;
        private readonly IEventsHandler _events;
        private readonly IPerksModule _perksModule;
        private readonly Utils.Patterns.Option<IDownedModule> _downedModule;
        private readonly Utils.Patterns.Option<IStaminaModule> _staminaModule;
        private readonly Utils.Patterns.Option<ILustModule> _lustModule;
        private int _index;
        
        public ModulesEnumerator([NotNull] CharacterStateMachine source)
        {
            _aiModule = source.AIModule;
            _chargeModule = source.ChargeModule;
            _recoveryModule = source.RecoveryModule;
            _stunModule = source.StunModule;
            _statusModule = source.StatusReceiverModule;
            _skillModule = source.SkillModule;
            _positionHandler = source.PositionHandler;
            _stateEvaluator = source.StateEvaluator;
            _statsModule = source.StatsModule;
            _resistancesModule = source.ResistancesModule;
            _statusApplierModule = source.StatusApplierModule;
            _events = source.Events;
            _perksModule = source.PerksModule;
            _downedModule = source.DownedModule;
            _staminaModule = source.StaminaModule;
            _lustModule = source.LustModule;
            _index = -1;
            Current = default;
        }
        
        public IModule Current { get; private set; }
        
        public ModulesEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            _index++;
            switch (_index)
            {
                case 0: Current = _aiModule; return true;
                case 1: Current = _chargeModule; return true;
                case 2: Current = _recoveryModule; return true;
                case 3: Current = _stunModule; return true;
                case 4: Current = _statusModule; return true;
                case 5: Current = _skillModule; return true;
                case 6: Current = _positionHandler; return true;
                case 7: Current = _stateEvaluator; return true;
                case 8: Current = _statsModule; return true;
                case 9: Current = _resistancesModule; return true;
                case 10: Current = _statusApplierModule; return true; 
                case 11: Current = _events; return true;
                case 12: Current = _perksModule; return true;
                case 13:
                    if (_downedModule.TrySome(out IDownedModule downedModule))
                    {
                        Current = downedModule;
                        return true;
                    }
                    if (_staminaModule.TrySome(out IStaminaModule staminaModule))
                    {
                        _index++;
                        Current = staminaModule;
                        return true;
                    }
                    if (_lustModule.TrySome(out ILustModule lustModule))
                    {
                        _index += 2;
                        Current = lustModule;
                        return true;
                    }
                    return false;
                
                case 14:
                    if (_staminaModule.TrySome(out staminaModule))
                    {
                        Current = staminaModule;
                        return true;
                    }
                    if (_lustModule.TrySome(out lustModule))
                    {
                        _index++;
                        Current = lustModule;
                        return true;
                    }
                    return false;
                
                case 15:
                    if (_lustModule.TrySome(out lustModule))
                    {
                        Current = lustModule;
                        return true;
                    }
                    return false;
                default: return false;
            }
        }
        
        public void Reset()
        {
            _index = -1;
            Current = default;
        }
    }
}