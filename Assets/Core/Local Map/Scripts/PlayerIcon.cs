using Core.Utils.Extensions;
using DG.Tweening;
using UnityEngine;

namespace Core.Local_Map.Scripts
{
    public sealed class PlayerIcon : MonoBehaviour
    {
        private const float Duration = LocalMapManager.CameraLerpDuration;
        
        private Tween _tween;

        public Tween GoToCell(HexagonObject.Cell cell)
        {
            _tween.KillIfActive();
            Vector3 destination = cell.transform.position;
            return transform.DOMove(destination, Duration);
        }

        public void GoToCellImmediate(HexagonObject.Cell cell)
        {
            _tween.KillIfActive();
            Vector3 destination = cell.transform.position;
            transform.position = destination;
        }
    }
}