using DG.Tweening;
using UnityEngine;

namespace Animation
{
    public sealed class CameraAnimator : MonoBehaviour
    {
        [SerializeField] public Camera selfCamera;

        public Tween LerpCamera(Vector3 worldPos, float speed, bool isSpeedBased)
        {
            Transform cameraTransform = selfCamera.transform;
            worldPos.z = cameraTransform.position.z;
            selfCamera.DOKill();

            return selfCamera.transform.DOMove(endValue: worldPos, duration: speed).SetSpeedBased(isSpeedBased: isSpeedBased).SetTarget(target: selfCamera);
        }

        private void Reset()
        {
            selfCamera = GetComponent<Camera>();
        }

        private void OnDestroy()
        {
            selfCamera.DOKill();
            this.DOKill();
        }
    }
}