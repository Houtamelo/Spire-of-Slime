using System.Text;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultPositionHandlerRecord(bool IsLeftSide, int Size) : PositionHandlerRecord(Size)
    {
        [NotNull]
        public override IPositionHandler Deserialize(CharacterStateMachine owner) => DefaultPositionHandler.FromRecord(owner, record: this);
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    }
    
    public class DefaultPositionHandler : IPositionHandler
    {
        private readonly CharacterStateMachine _owner;
        
        public bool IsLeftSide { get; private set; }
        public bool IsRightSide => IsLeftSide == false;

        private DefaultPositionHandler(CharacterStateMachine owner) => _owner = owner;

        [NotNull]
        public static DefaultPositionHandler FromInitialSetup([NotNull] CharacterStateMachine owner, bool isLeftSide) =>
            new(owner)
            {
                IsLeftSide = isLeftSide,
                Size = owner.Script.Size
            };

        [NotNull]
        public static DefaultPositionHandler FromRecord(CharacterStateMachine owner, [NotNull] DefaultPositionHandlerRecord record) =>
            new(owner)
            {
                IsLeftSide = record.IsLeftSide,
                Size = record.Size
            };
        
        [NotNull]
        public PositionHandlerRecord GetRecord() => new DefaultPositionHandlerRecord(IsLeftSide, Size);

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
            
            DisplayModule display = _owner.Display.Value;
            bool success = display.CombatManager.Characters.RequestSideSwitch(_owner);
            if (success == false)
                return false;
            
            IsLeftSide = isLeft;
            display.UpdateSide(IsLeftSide);
            return true;
        }
        
        private int _size;
        public int Size
        {
            get => _size;
            set
            {
                if (value is > 4 or < 0)
                {
                    Debug.LogWarning($"Trying to set {_owner.Script} to wrong size: {value.ToString()}", _owner.Display.SomeOrDefault());
                    value = value.Clamp(0, 4);
                }
                
                _size = value;
            }
        }

        public bool CanPositionCast(ISkill skill)
        {
            if (_owner.Display.AssertSome(out DisplayModule display) == false)
                return false;
            
            foreach (int pos in display.CombatManager.PositionManager.ComputePositioning(_owner))
            {
                if (skill.CastingPositions[pos])
                    return true;
            }

            return false;
        }

        public bool CanPositionCast(ISkill skill, CharacterPositioning positions)
        {
            foreach (int pos in positions)
            {
                if (skill.CastingPositions[pos])
                    return true;
            }

            return false;
        }
        
        public bool CanPositionBeTargetedBy(ISkill skill, CharacterStateMachine caster)
        {
            if (_owner.Display.AssertSome(out DisplayModule display) == false)
                return false;

            bool isAlly = caster.PositionHandler.IsLeftSide == IsLeftSide;
            if (isAlly != skill.IsPositive)
                return false;

            CharacterPositioning positioning = display.CombatManager.PositionManager.ComputePositioning(_owner);
            foreach (int position in positioning)
            {
                if (skill.TargetPositions[position])
                    return true;
            }

            return false;
        }

        public float GetRequiredGraphicalX()
        {
            if (_owner.StatusReceiverModule.GetAll.FindType<LustGrappled>().IsSome)
                return 0f; // we are grappled, the grappler handles rendering

            if (_owner.StatusReceiverModule.GetAllRelated.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.Restrainer == _owner)
                return lustGrappled.GraphicalX;

            return _owner.Script.IdleGraphicalX;
        }
    }
}