﻿using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Game_Manager.Scripts.Tooltips
{
    public class GeneralTooltip : Singleton<GeneralTooltip>
    {
        private static readonly Vector3[] ReusableCornersArray = new Vector3[4];
        
        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text tmp;

        [SerializeField, Required, SceneObjectsOnly]
        private CanvasGroup canvasGroup;

        [SerializeField, Required, SceneObjectsOnly]
        private Camera uiCamera;
        
        [SerializeField, Required, SceneObjectsOnly]
        private RectTransform selfRect;

        private void Start()
        {
            canvasGroup.alpha = 1f;
            Hide();
        }

        public void Show(string text)
        {
            tmp.text = text;
            gameObject.SetActive(true);
            UpdatePosition();
        }

        private void Update()
        {
            UpdatePosition();
        }

        private void UpdatePosition()
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector2 desiredScreenPosition = mouseScreenPosition + new Vector2(-5, 5);
            Vector3 desiredWorldPosition = uiCamera.ScreenToWorldPoint(desiredScreenPosition);
            desiredWorldPosition.z = 0;
            selfRect.position = desiredWorldPosition;
            
            selfRect.GetWorldCorners(ReusableCornersArray);
            Vector2 min = ReusableCornersArray[0];
            Vector2 max = ReusableCornersArray[2];
            Vector2 screenSize = new(Screen.width, Screen.height);
            Vector2 minScreen = uiCamera.WorldToScreenPoint(min);
            Vector2 maxScreen = uiCamera.WorldToScreenPoint(max);
            Vector2 offset = Vector2.zero;
            
            if (minScreen.x < 0)
                offset.x = -minScreen.x;
            else if (maxScreen.x > screenSize.x)
                offset.x = screenSize.x - maxScreen.x;
            
            if (minScreen.y < 0)
                offset.y = -minScreen.y;
            else if (maxScreen.y > screenSize.y)
                offset.y = screenSize.y - maxScreen.y;
            
            Vector3 offsetWorld = uiCamera.ScreenToWorldPoint(offset) - uiCamera.ScreenToWorldPoint(Vector3.zero);
            offsetWorld.z = 0;
            
            selfRect.position += offsetWorld;
        }

        public void Hide() => gameObject.SetActive(false);
    }
}