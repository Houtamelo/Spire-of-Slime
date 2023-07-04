using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.Guarded;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Interfaces;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Skills
{
    public readonly struct TargetResolver
    {
        private readonly ISkill _skill;
        private readonly CharacterStateMachine _caster;
        private readonly CharacterStateMachine _firstTarget;
        private readonly bool _valid;
        private bool Invalid => _valid == false;
        
        public TargetResolver(ISkill skill, CharacterStateMachine caster, CharacterStateMachine firstTarget)
        {
            _skill = skill;
            _caster = caster;
            _firstTarget = firstTarget;
            _valid = true;
        }

        /// <summary> Dispose the list after use. </summary>
        [MustUseReturnValue]
        public ValueListPool<CharacterStateMachine> GetTargetList()
        {
            if (Invalid || _caster.Display.IsNone)
            {
                Debug.LogWarning($"Target Resolver is invalid. GameObject exists: {_caster.Display.IsSome}", _caster.Display.SomeOrDefault());
                return new ValueListPool<CharacterStateMachine>(0);
            }
            
            CombatManager combatManager = _caster.Display.Value.CombatManager;
            bool isCasterLeft = _caster.PositionHandler.IsLeftSide;
            if (_skill.MultiTarget == false && _firstTarget.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                return new ValueListPool<CharacterStateMachine>(0);

            ValueListPool<CharacterStateMachine> targets;
            if (_skill.MultiTarget == false)
            {
                targets = new ValueListPool<CharacterStateMachine>(1);
                if (_skill.AllowAllies || _firstTarget.StatusModule.GetAll.FindType<Guarded>().TrySome(out Guarded guarded) == false || guarded.IsDeactivated)
                    targets.Add(_firstTarget);
                else
                    targets.Add(guarded.Caster);

                return targets;
            }

            targets = new ValueListPool<CharacterStateMachine>(4);
            if (_skill.AllowAllies)
            {
                for (int index = 0; index < PositionSetup.Length; index++)
                {
                    bool invalid = _skill.TargetPositions[index] == false;
                    if (invalid)
                        continue;

                    Option<CharacterStateMachine> aliveOnPos = combatManager.PositionManager.GetByPositioning(pos: index, isCasterLeft);
                    if (aliveOnPos.IsNone || (_skill.TargetType == TargetType.NotSelf && aliveOnPos.Value == _caster))
                        continue;

                    if (targets.Contains(aliveOnPos.Value) == false)
                        targets.Add(aliveOnPos.Value);
                }
            }
            else
            {
                for (int index = 0; index < PositionSetup.Length; index++)
                {
                    bool invalid = _skill.TargetPositions[index] == false;
                    if (invalid)
                        continue;

                    Option<CharacterStateMachine> aliveOnPos = combatManager.PositionManager.GetByPositioning(pos: index, isLeftSide: !isCasterLeft);
                    if (aliveOnPos.IsSome && targets.Contains(aliveOnPos.Value) == false)
                        targets.Add(aliveOnPos.Value);
                }
                
                for (int index = 0; index < targets.Count; index++)
                {
                    CharacterStateMachine character = targets[index];
                    if (character.StatusModule.GetAll.FindType<Guarded>().TrySome(out Guarded guarded) == false || guarded.IsDeactivated)
                        continue;
                    
                    if (targets.Contains(guarded.Caster) == false)
                        targets[index] = guarded.Caster;
                    break;
                }
            }
            
            return targets;
        }
    }
}