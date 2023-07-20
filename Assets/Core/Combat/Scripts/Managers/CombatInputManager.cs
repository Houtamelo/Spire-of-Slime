using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Combat.Scripts.UI;
using Core.Combat.Scripts.UI.Selected;
using Core.Main_Characters.Ethel.Combat;
using Core.Main_Characters.Nema.Combat;
using Core.Utils.Handlers;
using Core.Utils.Objects;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Combat.Scripts.Managers
{
    public class CombatInputManager : MonoBehaviour
    {
        [SerializeField, Required, SceneObjectsOnly]
        private CombatManager combatManager;

        [SerializeField, Required, SceneObjectsOnly]
        private LazyAnimationQueue animations;

        [SerializeField, Required, SceneObjectsOnly]
        private CharacterManager characters;
        
        [SerializeField, Required, SceneObjectsOnly]
        private CombatChoiceBox optionsBox;

        [SerializeField, Required, SceneObjectsOnly]
        private AudioSource skillPlannedSound;

        [SerializeField, Required, SceneObjectsOnly]
        private CustomAudioSource characterIdleSound;

        [SerializeField, Required, SceneObjectsOnly]
        private AudioSource skillSelectedSound;
        
        [SerializeField, Required, SceneObjectsOnly]
        private SelectedCharacterInterface selectedCharacterInterface;

        [SerializeField, Required, SceneObjectsOnly]
        private Button escapeButton;
        
        public readonly ValueHandler<CharacterStateMachine> SelectedCharacter = new();
        public readonly ValueHandler<CharacterStateMachine> HighlightedCharacter = new();
        public readonly ValueHandler<ISkill> SelectedSkill = new();
        private readonly HashSet<CharacterStateMachine> _forceSkipActionCharacters = new();
        
        public event Action<CharacterStateMachine> PlayerCharacterIdle;

        public bool IsButtonSelected(SkillButton button) => SelectedSkill.Value != null && button.Skill.TrySome(out ISkill buttonSkill) && buttonSkill == SelectedSkill.Value;

        private void Start()
        {
            escapeButton.onClick.AddListener(() => combatManager.PlayerRequestsEscape());
        }

        private void Update()
        {
            if (combatManager.Running == false || combatManager.Announcer.IsBusy || animations.Tick())
                return;
            
            if (Mouse.current.rightButton.wasPressedThisFrame && characters.GetCharacterOverMouse().IsNone)
                OnRightClickOutOfCharacters();

            if (SelectedCharacter.Value == null || SelectedCharacter.Value.Script.IsControlledByPlayer == false)
                return;
            
            Keyboard keyboard = Keyboard.current;
            if (keyboard.digit1Key.wasPressedThisFrame && selectedCharacterInterface.GetSkillButton(0).Skill.TrySome(out ISkill skillOne))
                DoSelectSkill(skillOne);
            else if (keyboard.digit2Key.wasPressedThisFrame && selectedCharacterInterface.GetSkillButton(1).Skill.TrySome(out ISkill skillTwo))
                DoSelectSkill(skillTwo);
            else if (keyboard.digit3Key.wasPressedThisFrame && selectedCharacterInterface.GetSkillButton(2).Skill.TrySome(out ISkill skillThree))
                DoSelectSkill(skillThree);
            else if (keyboard.digit4Key.wasPressedThisFrame && selectedCharacterInterface.GetSkillButton(3).Skill.TrySome(out ISkill skillFour))
                DoSelectSkill(skillFour);
        }

        public void PlayerControlledCharacterIdle([NotNull] CharacterStateMachine character)
        {
            if (character.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled or CharacterState.Grappling)
                return;
            
            if (_forceSkipActionCharacters.Contains(character) == false)
            {
                characterIdleSound.Play();
                _forceSkipActionCharacters.Add(character);
                combatManager.PauseTime();
                SelectedCharacter.SetValue(character);
            }
            
            PlayerCharacterIdle?.Invoke(character);
        }
		
		public void CharacterClicked([CanBeNull] CharacterStateMachine clickedCharacter, PointerEventData pointerEventData)
        {
            if (animations.EvaluateState() is not QueueState.Idle || clickedCharacter == null)
                return;
            
            if (clickedCharacter.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Grappled)
                return;
            
            if (pointerEventData.button == PointerEventData.InputButton.Right && CheckIfClickedNema(clickedCharacter))
                return;
            
            if (pointerEventData.button != PointerEventData.InputButton.Left)
                return;

            CharacterStateMachine selectedCharacter = SelectedCharacter.Value;
            ISkill selectedSkill = SelectedSkill.Value;
            if (selectedCharacter != null && selectedCharacter.SkillModule.HasSkill(selectedSkill) && selectedCharacter.StateEvaluator.PureEvaluate() is CharacterState.Idle &&
                selectedSkill.FullCastingAndTargetingOk(caster: selectedCharacter, target: clickedCharacter))
            {
                _forceSkipActionCharacters.Clear();
                skillPlannedSound.Play();
                selectedCharacter.SkillModule.PlanSkill(selectedSkill, target: clickedCharacter);
                SelectedSkill.SetValue(null);

                Option<CharacterStateMachine> anotherIdleCharacter = Option<CharacterStateMachine>.None;
                foreach (CharacterStateMachine character in characters.FixedOnLeftSide)
                {
                    if (((CombatManager.DEBUGMODE && Application.isEditor) || character.Script.IsControlledByPlayer)
                     && character.StateEvaluator.PureEvaluate() is CharacterState.Idle)
                    {
                        anotherIdleCharacter = character;
                        break;
                    }
                }

                if (anotherIdleCharacter.IsSome)
                {
                    SelectedCharacter.SetValue(anotherIdleCharacter.Value);
                }
                else
                {
                    combatManager.UnPauseTime();
                    SelectedCharacter.SetValue(null);
                }
            }
            else if (clickedCharacter != SelectedCharacter.Value && (CombatManager.DEBUGMODE || (clickedCharacter.Script.IsControlledByPlayer && clickedCharacter.PositionHandler.IsLeftSide)))
            {
                SelectedSkill.SetValue(null);
                SelectedCharacter.SetValue(clickedCharacter);
            }
        }

        private bool CheckIfClickedNema([NotNull] CharacterStateMachine clickedCharacter)
        {
            if (clickedCharacter.Script.Key != Nema.GlobalKey || combatManager.CombatSetupInfo.MistExists == false || clickedCharacter.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse)
                return false;
            
            bool canEthelSpeak = false;
            foreach (CharacterStateMachine playerAllies in characters.FixedOnLeftSide)
            {
                if (playerAllies.Script.Key == Ethel.GlobalKey && playerAllies.StateEvaluator.PureEvaluate() is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Grappled))
                {
                    canEthelSpeak = true;
                    break;
                }
            }

            bool hasGameObject = clickedCharacter.Display.IsSome;

            if (canEthelSpeak == false && hasGameObject)
            {
                if (CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager))
                {
                    DisplayModule characterGameObject = clickedCharacter.Display.Value;
                    CombatCueOptions options = CombatCueOptions.Default(text: "I can't reach Nema!", color: Color.white, characterGameObject);
                    options.CanShowOnTopOfOthers = true;
                    cueManager.EnqueueAboveCharacter(ref options, characterGameObject);
                }

                return true;
            }

            if (hasGameObject && clickedCharacter.Display.Value.GetCuePosition().TrySome(out Vector3 position))
            {
                string text;
                UnityAction onClick;
                if (Save.Current.IsNemaClearingMist)
                {
                    text = "Nema, stop clearing the mist!";
                    onClick = () =>
                    {
                        if (Save.AssertInstance(out Save save))
                            save.SetNemaClearingMist(false);
                    };
                }
                else
                {
                    text = "Nema, clear the mist!";
                    onClick = () =>
                    {
                        if (Save.AssertInstance(out Save save))
                            save.SetNemaClearingMist(true);
                    };
                }
                
                optionsBox.ShowOptions(position, text, onClick);
            }

            return true;
        }

        public void CharacterPointerEnter(CharacterStateMachine character)
        {
            if (animations.EvaluateState() is QueueState.Idle)
                HighlightedCharacter.SetValue(character);
        }

        public void CharacterPointerExit(CharacterStateMachine character)
        {
            if (HighlightedCharacter.Value == character)
                HighlightedCharacter.SetValue(null);
        }

        public void DoSelectSkill(ISkill skill)
        {
            if (SelectedSkill.Value == skill)
            {
                SelectedSkill.SetValue(null);
                skillSelectedSound.Play();
                return;
            }
            
            if (SelectedCharacter.Value == null || SelectedCharacter.Value.StateEvaluator.PureEvaluate() is not CharacterState.Idle)
                return;
            
            skillSelectedSound.Play();
            SelectedSkill.SetValue(skill);
            combatManager.PauseTime();
        }

        public void ResetInputs()
        {
            SelectedCharacter.SetValue(default);
            HighlightedCharacter.SetValue(default);
            SelectedSkill.SetValue(default);
            _forceSkipActionCharacters.Clear();
        }

        private void OnRightClickOutOfCharacters()
        {
            if (SelectedSkill.Value != null)
                SelectedSkill.SetValue(null);
            else if (SelectedCharacter.Value != null)
                SelectedCharacter.SetValue(null);
        }
    }
}