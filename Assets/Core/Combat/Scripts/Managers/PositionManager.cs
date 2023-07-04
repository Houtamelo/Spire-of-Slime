using System;
using System.Collections;
using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using JetBrains.Annotations;
using KGySoft.CoreLibraries;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Async;
using Utils.Collections;
using Utils.Extensions;
using Utils.Patterns;

namespace Core.Combat.Scripts.Managers
{
    public class PositionManager : MonoBehaviour
    {
        private const float ScreenWidth = ScreenHalfWidth * 2f;
        private const float ScreenHalfWidth = 9.6f;
        private const float LeftEdge = -ScreenHalfWidth;
        private const float RightEdge = ScreenHalfWidth;
        
        private static readonly (CharacterStateMachine character, Vector3 position)[] ReusablePositionArray = new (CharacterStateMachine character, Vector3 position)[12];
        private static readonly float[] ReusableGraphicalXArray = new float[12];

        private const float CharacterMoveBaseDuration = 1.3f;
        public static float CharacterMoveDuration => CharacterMoveBaseDuration * IActionSequence.DurationMultiplier;
        
        [SerializeField, Required]
        private CombatManager combatManager;

        private CharacterManager CharacterManager => combatManager.Characters;
        
        public Option<CharacterStateMachine> GetByPositioning(int pos, bool isLeftSide)
        {
            int currentPos = 0;
            foreach (CharacterStateMachine character in CharacterManager.GetOnSide(isLeftSide))
            {
                if (character.StateEvaluator.PureEvaluate() is CharacterState.Defeated)
                    continue;

                for (int i = 0; i < character.PositionHandler.Size; i++)
                {
                    if (currentPos == pos)
                        return Option<CharacterStateMachine>.Some(character);

                    currentPos++;
                }
            }
            
            return Option.None;
        }
        
        [MustUseReturnValue]
        public CharacterPositioning ComputePositioning(CharacterStateMachine requester)
        {
            if (requester.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                return default;
            
            IPositionHandler selfPositionHandler = requester.PositionHandler;
            IReadOnlyList<CharacterStateMachine> allies = CharacterManager.GetEditable(selfPositionHandler.IsLeftSide);
            int selfIndex = allies.IndexOf(requester);
            
            byte startPosition = 0;
            for (int i = 0; i < selfIndex; i++)
            {
                CharacterStateMachine ally = allies[i];
                if (ally.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                    continue;
                
                startPosition += ally.PositionHandler.Size;
            }

            return new CharacterPositioning(selfPositionHandler.Size, startPosition);
        }
        
        /// <summary> This is expensive, if you have to calculate more than one position use <see cref="ComputeAllDefaultPositions"/> instead </summary>
        public Vector3 GetCharacterDefaultWorldPosition(CharacterStateMachine requester)
        {
            ReadOnlySpan<(CharacterStateMachine character, Vector3 position)> allPositions = ComputeAllDefaultPositions();
            foreach ((CharacterStateMachine character, Vector3 position) in allPositions)
                if (character == requester)
                    return position;
            
            Debug.LogError($"Character {requester} not found in all positions");
            return Vector3.zero;
        }

        public ReadOnlySpan<(CharacterStateMachine character, Vector3 position)> ComputeAllDefaultPositions()
        {
            GeneralPaddingSettings paddingSettings = combatManager.CombatSetupInfo.PaddingSettings;
            
            Array.Clear(ReusablePositionArray, index: 0, ReusablePositionArray.Length);
            Array.Clear(ReusableGraphicalXArray, index: 0, ReusableGraphicalXArray.Length);

            float currentX = LeftEdge;
            int currentIndex = 0;
            IReadOnlyList<CharacterStateMachine> leftSide = CharacterManager.GetLeftEditable();
            for (int i = 0; i < leftSide.Count; i++, currentIndex++)
            {
                CharacterStateMachine character = leftSide[i];
                float requiredGraphicalX = character.PositionHandler.GetRequiredGraphicalX();
                currentX += requiredGraphicalX;
                ReusableGraphicalXArray[currentIndex] = requiredGraphicalX;
                ReusablePositionArray[currentIndex] = (character, new Vector3(currentX - requiredGraphicalX / 2f, paddingSettings.EvenY, 0f));
            }

            currentX += paddingSettings.LeftMiddle + paddingSettings.RightMiddle; //todo! Merge middles
            IReadOnlyList<CharacterStateMachine> rightSide = CharacterManager.GetRightEditable();
            for (int i = 0; i < rightSide.Count; i++, currentIndex++)
            {
                CharacterStateMachine character = rightSide[i];
                float requiredGraphicalX = character.PositionHandler.GetRequiredGraphicalX();
                currentX += requiredGraphicalX;
                ReusableGraphicalXArray[currentIndex] = requiredGraphicalX;
                ReusablePositionArray[currentIndex] = (character, new Vector3(currentX - requiredGraphicalX / 2f, paddingSettings.EvenY, 0f));
            }
            
            float occupiedWidth = currentX + ScreenHalfWidth;
            float ratio = occupiedWidth == 0f ? 2f : ScreenWidth / occupiedWidth;
            if (ratio < 1f) // check if we exceeded the screen's bounds, if yes, we'll overlap the characters by re-scaling their graphicalXes
            {
                for (int i = 0; i < currentIndex; i++)
                {
                    ReusableGraphicalXArray[i] *= ratio;
                }

                currentX = LeftEdge;
                currentIndex = 0;
                for (int i = 0; i < leftSide.Count; i++, currentIndex++)
                {
                    CharacterStateMachine character = ReusablePositionArray[currentIndex].character;
                    float requiredGraphicalX = ReusableGraphicalXArray[currentIndex];
                    currentX += requiredGraphicalX;
                    ReusablePositionArray[currentIndex] = (character, new Vector3(currentX - requiredGraphicalX / 2f, paddingSettings.EvenY, 0f));
                }
                
                currentX += (paddingSettings.LeftMiddle + paddingSettings.RightMiddle) * ratio;
                for (int i = 0; i < rightSide.Count; i++, currentIndex++)
                {
                    CharacterStateMachine character = ReusablePositionArray[currentIndex].character;
                    float requiredGraphicalX = ReusableGraphicalXArray[currentIndex];
                    currentX += requiredGraphicalX;
                    ReusablePositionArray[currentIndex] = (character, new Vector3(currentX - requiredGraphicalX / 2f, paddingSettings.EvenY, 0f));
                }
            }
            
            // we'll shift every character to the right by half the space difference so that the distance between the first character and the left edge is the same as the distance between the last character and the right edge
            occupiedWidth = currentX + ScreenHalfWidth;
            float spaceDifference = ScreenWidth - occupiedWidth;
            float shift = spaceDifference / 2f;
            for (int i = 0; i < currentIndex; i++)
            {
                (CharacterStateMachine character, Vector3 position) = ReusablePositionArray[i];
                position.x += shift;
                ReusablePositionArray[i] = (character, position);
            }

            return ReusablePositionArray.AsSpan(start: 0, length: currentIndex);
        }

        /// <returns> If any change was made. </returns>
        public bool ShiftPosition(CharacterStateMachine target, int delta)
        {
            if (delta == 0)
                return false;

            IndexableHashSet<CharacterStateMachine> allies = CharacterManager.GetEditable(target.PositionHandler.IsLeftSide);
            int currentIndex = allies.IndexOf(target);
            int desiredIndex = Mathf.Clamp(currentIndex + delta, 0, allies.Count - 1);
            if (currentIndex == -1)
            {
                Debug.LogWarning($"Character {target} not found in {allies}");
                return false;
            }
            
            if (desiredIndex == currentIndex)
                return false;
            
            allies.ReInsert(target, desiredIndex);
            MoveAllToDefaultPosition(baseDuration: Option<float>.Some(CharacterMoveDuration));
            return true;
        }

        /// <returns> World Space Position </returns>
        private static Vector3 GetAnimationPosition(int position, bool isLeftSide, Vector3 split, float middlePadding, float inBetweenPadding)
        {
            if (position is < 0 or > 3)
                return split;

            if (isLeftSide)
                split.x += -middlePadding - position * inBetweenPadding;
            else
                split.x += +middlePadding + position * inBetweenPadding;

            return split;
        }

        public void FillDefaultAnimationPositions(IDictionary<CharacterStateMachine, Vector3> positions, CharacterStateMachine caster, HashSet<CharacterStateMachine> charactersToCalculate, ISkill skill)
        {
            ReadOnlyPaddingSettings paddingSettings = skill.GetPaddingSettings();
            float inBetweenPadding = paddingSettings.InBetweenPadding;
            float leftMiddlePadding, rightMiddlePadding;
            if (caster.PositionHandler.IsLeftSide)
            {
                leftMiddlePadding = paddingSettings.AllyMiddlePadding;
                rightMiddlePadding = paddingSettings.EnemyMiddlePadding;
            }
            else
            {
                leftMiddlePadding = paddingSettings.EnemyMiddlePadding;
                rightMiddlePadding = paddingSettings.AllyMiddlePadding;
            }
            
            int leftCount = 0;
            foreach (CharacterStateMachine character in CharacterManager.FixedOnLeftSide)
                if (charactersToCalculate.Contains(character))
                    AddPositionToDictionary(positions, character, ref leftCount, leftMiddlePadding, inBetweenPadding);

            int rightCount = 0;
            foreach (CharacterStateMachine character in CharacterManager.FixedOnRightSide)
                if (charactersToCalculate.Contains(character))
                    AddPositionToDictionary(positions, character, ref rightCount, rightMiddlePadding, inBetweenPadding);

            static void AddPositionToDictionary(IDictionary<CharacterStateMachine, Vector3> source, CharacterStateMachine character, ref int count, float middlePadding, float inBetweenPadding)
            {
                if (character.Display.AssertSome(out CharacterDisplay characterGameObject) == false)
                    return;

                if (character.StateEvaluator.PureEvaluate() is CharacterState.Grappled or CharacterState.Defeated)
                {
                    source.Add(character, characterGameObject.transform.position);
                    return;
                }
                
                int size = character.Script.Size;
                if (size == 0)
                {
                    source.Add(character, characterGameObject.transform.position);
                    return;
                }

                Vector3 split = new(0f, IActionSequence.YPosition, 0);
                Vector3 worldPos = Vector3.zero;
                for (int i = 0; i < size; i++)
                    worldPos += GetAnimationPosition(position: i + count, isLeftSide: character.PositionHandler.IsLeftSide, split, middlePadding, inBetweenPadding);

                worldPos /= size;
                source.Add(character, worldPos);
                count += size;
            }
        }

        public void FillTemptAnimationPositions(IDictionary<CharacterStateMachine, Vector3> positions, CharacterStateMachine caster, HashSet<CharacterStateMachine> charactersToCalculate)
        {
            Vector3 center = new(0f, IActionSequence.YPosition, 0f);
            positions[caster] = center;
            foreach (CharacterStateMachine character in charactersToCalculate)
                positions[character] = center;
        }
        
        public void MoveAllToDefaultPosition(Option<float> baseDuration)
        {
            CharacterManager.UnsubscribeDefeated();

            if (baseDuration.TrySome(out float duration))
            {
                CoroutineWrapper wrapper = new(MoveAllToDefaultPositionRoutine(duration), nameof(MoveAllToDefaultPositionRoutine), this, autoStart: false);
                AnimationRoutineInfo info = AnimationRoutineInfo.WithoutCharacter(wrapper);
                combatManager.Animations.PriorityEnqueue(info);
            }
            else
            {
                foreach ((CharacterStateMachine character, Vector3 position) in ComputeAllDefaultPositions())
                    if (character.Display.TrySome(out CharacterDisplay display))
                        display.MoveToPosition(position, baseDuration: Option.None);
            }
        }
        
        private IEnumerator MoveAllToDefaultPositionRoutine(float duration)
        {
            MoveAll();
            
            while (AnyMoving())
                yield return null;

            void MoveAll() // cannot inline due to Enumerator being ref struct
            {
                foreach ((CharacterStateMachine character, Vector3 position) in ComputeAllDefaultPositions())
                    if (character.Display.TrySome(out CharacterDisplay display))
                        display.MoveToPosition(position, baseDuration: Option<float>.Some(duration));
            }

            bool AnyMoving()
            {
                foreach (CharacterStateMachine character in CharacterManager.GetAllFixed())
                    if (character.Display.TrySome(out CharacterDisplay display) && display.IsBusy)
                        return true;

                return false;
            }
        }
        
        public CharacterPositioning PredictPositionsOnMove(CharacterStateMachine target, byte delta, out bool anyMovement)
        {
            List<CharacterStateMachine> allies = CharacterManager.GetEditable(isLeftSide: target.PositionHandler.IsLeftSide);
            int currentIndex = allies.IndexOf(target);
            Debug.Assert(currentIndex >= 0, $"{target.Script.CharacterName},isLeft:{target.PositionHandler.IsLeftSide}");
            int newIndex = Mathf.Clamp(currentIndex + delta, 0, allies.Count - 1);

            CharacterPositioning positioning = ComputePositioning(target);
            if (currentIndex == newIndex)
            {
                anyMovement = false;
                return positioning;
            }
            
            delta = (byte)(newIndex - currentIndex);
            positioning.startPosition += delta;
            anyMovement = true;
            return positioning;
        }
    }
}