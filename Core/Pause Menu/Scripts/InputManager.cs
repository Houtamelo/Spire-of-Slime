using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;
using Utils.Patterns;

namespace Core.Pause_Menu.Scripts
{
    public sealed class InputManager : Singleton<InputManager>
    {
        [OdinSerialize, Required]
        private readonly InputActionAsset _inputActionAsset;
        
        private readonly Dictionary<InputEnum, InputAction> _inputActions = new();

        public readonly Dictionary<InputEnum, List<Action>> PerformedActionsCallbacks = new();
        public readonly Dictionary<InputEnum, List<Action>> CanceledActionsCallbacks = new();
        public bool HoldTooltipHeld { get; private set; }

        protected override void Awake()
        {
            base.Awake();
            foreach (InputAction action in _inputActionAsset.actionMaps[0].actions)
            {
                if (Enum.TryParse(action.name, out InputEnum inputEnum) == false)
                {
                    Debug.LogError($"{action.name} is not a valid input enum");
                    continue;
                }
                
                action.Enable();
                action.performed += _ => ActionPerformed(inputEnum);
                action.canceled += _ => ActionCanceled(inputEnum);
                _inputActions.Add(inputEnum, action);
                PerformedActionsCallbacks.Add(inputEnum, new List<Action>());
                CanceledActionsCallbacks.Add(inputEnum, new List<Action>());
            }
            
            PerformedActionsCallbacks[InputEnum.HoldTooltip].Add(HoldTooltipPerformed);
            CanceledActionsCallbacks[InputEnum.HoldTooltip].Add(HoldTooltipCanceled);
        }

        private void HoldTooltipCanceled() => HoldTooltipHeld = false;
        private void HoldTooltipPerformed() => HoldTooltipHeld = true;
        
        private void ActionPerformed(InputEnum inputEnum)
        {
            GameObject selectedGameObject = EventSystem.current.currentSelectedGameObject;
            if (selectedGameObject != null && selectedGameObject.TryGetComponent<TMP_InputField>(out _))
                return;
            
            foreach (Action action in PerformedActionsCallbacks[inputEnum])
                action.Invoke();
        }
        
        private void ActionCanceled(InputEnum inputEnum)
        {
            GameObject selectedGameObject = EventSystem.current.currentSelectedGameObject;
            if (selectedGameObject != null && selectedGameObject.TryGetComponent<TMP_InputField>(out _))
                return;
            
            foreach (Action action in CanceledActionsCallbacks[inputEnum])
                action.Invoke();
        }
        
        public ReadOnlyArray<InputBinding> GetInputBindings(InputEnum inputEnum) => _inputActions[inputEnum].bindings;

        public bool AnyAdvanceInputThisFrame() => Mouse.current.leftButton.wasPressedThisFrame || Keyboard.current.anyKey.wasPressedThisFrame;


#if UNITY_EDITOR
        private void OnValidate()
        {
            foreach (InputAction action in _inputActionAsset.actionMaps[0].actions)
                if (!Enum.TryParse(action.name, out InputEnum _))
                    Debug.LogWarning($"InputAsset does not contain a valid input enum named {action.name}");
        }
#endif
    }
}