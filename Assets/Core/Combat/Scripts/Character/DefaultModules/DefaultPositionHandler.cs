using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills.Interfaces;
using UnityEngine;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultPositionHandler : IPositionHandler
    {
        private readonly CharacterStateMachine _owner;
        
        public bool IsLeftSide { get; private set; }
        public bool IsRightSide => IsLeftSide == false;

        private DefaultPositionHandler(CharacterStateMachine owner) => _owner = owner;

        public static DefaultPositionHandler FromInitialSetup(CharacterStateMachine owner, bool isLeftSide) =>
            new(owner)
            {
                IsLeftSide = isLeftSide,
                Size = owner.Script.Size
            };

        public static DefaultPositionHandler FromRecord(CharacterStateMachine owner, CharacterRecord record) =>
            new(owner)
            {
                IsLeftSide = record.IsLeftSide,
                Size = owner.Script.Size
            };

        public float GetAveragePosition()
        {
            if (_owner.Display.IsNone)
                return 0;
            
            CharacterPositioning positioning = _owner.Display.Value.CombatManager.PositionManager.ComputePositioning(_owner);
            if (positioning.size == 0)
                return 0f;
            
            float average = 0f;
            for (int i = 0; i < positioning.size; i++)
                average += positioning.startPosition + i;

            average /= positioning.size;
            return average;
        }

        public bool SetSide(bool isLeft)
        {
            if (_owner.Display.IsNone)
                return false;

            if (IsLeftSide == isLeft)
                return true;
            
            CharacterDisplay display = _owner.Display.Value;
            bool success = display.CombatManager.Characters.RequestSideSwitch(_owner);
            if (success == false)
                return false;
            
            IsLeftSide = isLeft;
            display.UpdateSide(IsLeftSide);
            return true;
        }
        
        private byte _size;
        public byte Size
        {
            get => _size;
            set
            {
                if (value > 4)
                {
                    Debug.LogWarning($"Trying to set {_owner.Script} to wrong size: {value.ToString()}", _owner.Display.SomeOrDefault());
                    value = 4;
                }
                
                _size = value;
            }
        }

        public bool CanPositionCast(ISkill skill)
        {
            if (_owner.Display.AssertSome(out CharacterDisplay display) == false)
                return false;
            
            foreach (int pos in display.CombatManager.PositionManager.ComputePositioning(_owner))
                if (skill.CastingPositions[pos])
                    return true;
            
            return false;
        }

        public bool CanPositionCast(ISkill skill, CharacterPositioning positions)
        {
            foreach (int pos in positions)
                if (skill.CastingPositions[pos])
                    return true;
            
            return false;
        }
        
        public bool CanPositionBeTargetedBy(ISkill skill, CharacterStateMachine caster)
        {
            if (_owner.Display.AssertSome(out CharacterDisplay display) == false)
                return false;

            bool isAlly = caster.PositionHandler.IsLeftSide == IsLeftSide;
            if (isAlly != skill.AllowAllies)
                return false;

            CharacterPositioning positioning = display.CombatManager.PositionManager.ComputePositioning(_owner);
            foreach (int position in positioning)
                if (skill.TargetPositions[position])
                    return true;

            return false;
        }

        public float GetRequiredGraphicalX()
        {
            if (_owner.StatusModule.GetAll.FindType<LustGrappled>().IsSome)
                return 0f; // we are grappled, the grappler handles rendering

            if (_owner.StatusModule.GetAllRelated.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.Restrainer == _owner)
                return lustGrappled.GraphicalX;

            return _owner.Script.IdleGraphicalX;
        }
    }
}