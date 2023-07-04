using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;

namespace Core.Combat.Scripts.Behaviour.UI
{
    public class Indicators : MonoBehaviour
    {
        [SerializeField, Required]
        private CharacterDisplay owner;

        [SerializeField, Required]
        private CanvasGroup canvasGroup;

        [SerializeField, Required]
        private Image targetable, highlighted, selected;

        private Sequence _sequence;
        private bool _allowed = true;
        
        private CombatManager _combatManager;

        public void SubscribeToCombatManager(CombatManager manager)
        {
            if (_combatManager != null)
                UnsubscribeToCombatManager(_combatManager);
            
            _combatManager = manager;
            manager.InputHandler.SelectedCharacter.Changed += OnCharacterSelected;
            manager.InputHandler.HighlightedCharacter.Changed += OnCharacterMouseOver;
            manager.InputHandler.SelectedSkill.Changed += OnSkillSelected;
        }

        private void UnsubscribeToCombatManager(CombatManager manager)
        {
            manager.InputHandler.SelectedCharacter.Changed -= OnCharacterSelected;
            manager.InputHandler.HighlightedCharacter.Changed -= OnCharacterMouseOver;
            manager.InputHandler.SelectedSkill.Changed -= OnSkillSelected;
        }

        private void Start()
        {
            canvasGroup.alpha = 1f;
        }
        private void OnDestroy()
        {
            if (_combatManager != null)
                UnsubscribeToCombatManager(_combatManager);
            
            _sequence.KillIfActive();
        }

        private void OnCharacterMouseOver(CharacterStateMachine character)
        {
            if (_allowed)
                CheckForChanges();
        }

        private void OnCharacterSelected(CharacterStateMachine character)
        {
            if (_allowed)
                CheckForChanges();
        }

        private void OnSkillSelected(ISkill skill)
        {
            if (_allowed)
                CheckForChanges();
        }

        public void AnimateForAction()
        {
            _sequence.CompleteIfActive();

            targetable.enabled = false;
            selected.enabled = false;
            highlighted.enabled = true;
            highlighted.transform.localScale = new Vector3(1.3f, 1.3f, 1.3f);
            canvasGroup.alpha = 0f;
            _sequence = DOTween.Sequence().SetEase(Ease.OutQuad);
            _sequence.Append(highlighted.transform.DOScale(endValue: 1f, IActionSequence.StartDuration / 2f));
            _sequence.Join(canvasGroup.DOFade(endValue: 1f, IActionSequence.StartDuration / 2f));
            _sequence.AppendInterval(IActionSequence.StartDuration / 2f);
        }

        public void CheckForChanges()
        {
            if (_sequence is { active: true } || owner.StateMachine.IsNone || _combatManager == null || owner.AnimationStatus is AnimationStatus.Defeated)
                return;
            
            CharacterStateMachine ownerStateMachine = owner.StateMachine.Value;
            highlighted.transform.localScale = Vector3.one;

            bool isHighlighted = false;
            CharacterStateMachine selectedCharacter = _combatManager.InputHandler.SelectedCharacter.Value;
            CharacterStateMachine highlightedCharacter = _combatManager.InputHandler.HighlightedCharacter.Value;
            ISkill selectedSkill = _combatManager.InputHandler.SelectedSkill.Value;
            if (highlightedCharacter == ownerStateMachine && ownerStateMachine != null)
            {
                isHighlighted = true;
            }
            else if (selectedSkill != null && selectedCharacter != null && highlightedCharacter != null && ownerStateMachine != null && selectedSkill.HitsCollateral(selectedCharacter, ownerStateMachine))
            {
                isHighlighted = true;
            }
            
            if (isHighlighted)
            {
                highlighted.enabled = true;
                selected.enabled = false;
                targetable.enabled = false;
                return;
            }
            
            bool isTargetable = _combatManager.Animations.EvaluateState() is QueueState.Idle &&
                                selectedCharacter != null && selectedSkill != null &&
                                selectedCharacter.StateEvaluator.PureEvaluate() is CharacterState.Idle 
                                && selectedSkill.FullCastingAndTargetingOk(selectedCharacter, ownerStateMachine);

            if (isTargetable)
            {
                highlighted.enabled = false;
                selected.enabled = false;
                targetable.enabled = true;
                return;
            }
            
            bool isSelected = selectedCharacter == ownerStateMachine;
            selected.enabled = isSelected;
            highlighted.enabled = false;
            targetable.enabled = false;
        }

        public void Allow(bool value)
        {
            if (_allowed != value)
                _sequence.CompleteIfActive();
            
            _allowed = value;
            canvasGroup.alpha = value ? 1f : 0f;
            highlighted.transform.localScale = Vector3.one;
            
            if (value)
            {
                CheckForChanges();
            }
            else
            {
                highlighted.enabled = false;
                selected.enabled = false;
                targetable.enabled = false;
            }
        }
    }
}