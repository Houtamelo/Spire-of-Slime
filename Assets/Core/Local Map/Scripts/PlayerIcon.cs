using Core.Utils.Extensions;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Local_Map.Scripts
{
    public sealed class PlayerIcon : MonoBehaviour
    {
        private const float Duration = LocalMapManager.CameraLerpDuration;
        
        private Tween _tween;

        public Tween GoToCell([NotNull] HexagonObject.Cell cell)
        {
            _tween.KillIfActive();
            Vector3 destination = cell.transform.position;
            return transform.DOMove(destination, Duration);
        }

        public void GoToCellImmediate([NotNull] HexagonObject.Cell cell)
        {
            _tween.KillIfActive();
            Vector3 destination = cell.transform.position;
            transform.position = destination;
        }
    }
}