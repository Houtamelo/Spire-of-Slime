using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.Guarded;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Collections;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using ListPool;
using UnityEngine;

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
        
        public void FillTargetList(List<CharacterStateMachine> fillMe)
        {
            if (Invalid || _caster.Display.IsNone)
            {
                Debug.LogWarning($"Target Resolver is invalid. GameObject exists: {_caster.Display.IsSome}", _caster.Display.SomeOrDefault());
                return;
            }
            
            CombatManager combatManager = _caster.Display.Value.CombatManager;
            bool isCasterLeft = _caster.PositionHandler.IsLeftSide;
            if (_skill.MultiTarget == false && _firstTarget.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                return;
            
            if (_skill.MultiTarget == false)
            {
                if (_skill.IsPositive || _firstTarget.StatusReceiverModule.GetAll.FindType<Guarded>().TrySome(out Guarded guarded) == false || guarded.IsDeactivated)
                    fillMe.Add(_firstTarget);
                else
                    fillMe.Add(guarded.Caster);

                return;
            }
            
            if (_skill.IsPositive)
            {
                for (int index = 0; index < PositionSetup.Length; index++)
                {
                    bool invalid = _skill.TargetPositions[index] == false;
                    if (invalid)
                        continue;

                    Option<CharacterStateMachine> aliveOnPos = combatManager.PositionManager.GetByPositioning(pos: index, isCasterLeft);
                    if (aliveOnPos.IsNone || (_skill.TargetType == TargetType.NotSelf && aliveOnPos.Value == _caster))
                        continue;

                    if (fillMe.Contains(aliveOnPos.Value) == false)
                        fillMe.Add(aliveOnPos.Value);
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
                    if (aliveOnPos.IsSome && fillMe.Contains(aliveOnPos.Value) == false)
                        fillMe.Add(aliveOnPos.Value);
                }
                
                for (int index = 0; index < fillMe.Count; index++)
                {
                    CharacterStateMachine character = fillMe[index];
                    if (character.StatusReceiverModule.GetAll.FindType<Guarded>().TrySome(out Guarded guarded) == false || guarded.IsDeactivated)
                        continue;
                    
                    if (fillMe.Contains(guarded.Caster) == false)
                        fillMe[index] = guarded.Caster;
                    
                    break;
                }
            }
        }
    }
}