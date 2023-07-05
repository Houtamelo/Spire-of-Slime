using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Utils.Patterns;

namespace Core.World_Map.Scripts
{
    public sealed class WorldMapCameraManager : Singleton<WorldMapCameraManager>, IDragHandler
    {
        [OdinSerialize, SceneObjectsOnly, Required]
        private readonly Camera _camera;
        
        [OdinSerialize, SceneObjectsOnly, Required] 
        private readonly Transform _virtualCameraTransform;

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
                return;
            
            Vector3 mouseScreenDelta = -Mouse.current.delta.ReadValue();
            mouseScreenDelta.z = 0;
            Vector3 cameraWorldPosition = _virtualCameraTransform.transform.position;
            Vector3 cameraScreenPos = _camera.WorldToScreenPoint(position: cameraWorldPosition);
            cameraScreenPos += mouseScreenDelta;
            float cameraZ = cameraWorldPosition.z;
            cameraWorldPosition = _camera.ScreenToWorldPoint(position: cameraScreenPos);
            cameraWorldPosition.z = cameraZ;
            _virtualCameraTransform.transform.position = cameraWorldPosition;
        }
    }
}