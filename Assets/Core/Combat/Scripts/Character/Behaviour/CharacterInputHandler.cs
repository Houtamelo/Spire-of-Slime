using Core.Combat.Scripts.Managers;
using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Core.Combat.Scripts.Behaviour
{
    public sealed class CharacterInputHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField, Required]
        private DisplayModule owner;
        
        public bool IsMouseOver { get; private set; }
        private Option<CombatManager> _combatManager;

        public void AssignCombatManager(CombatManager manager)
        {
            _combatManager = manager != null ? manager : Option<CombatManager>.None;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (owner.StateMachine.IsSome && _combatManager.IsSome)
                _combatManager.Value.InputHandler.CharacterPointerEnter(character: owner.StateMachine.Value);
            
            IsMouseOver = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (owner.StateMachine.IsSome && _combatManager.IsSome)
                _combatManager.Value.InputHandler.CharacterPointerExit(character: owner.StateMachine.Value);
            
            IsMouseOver = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (owner.StateMachine.IsSome && _combatManager.IsSome)
                _combatManager.Value.InputHandler.CharacterClicked(clickedCharacter: owner.StateMachine.Value, eventData);
        }
    }
}