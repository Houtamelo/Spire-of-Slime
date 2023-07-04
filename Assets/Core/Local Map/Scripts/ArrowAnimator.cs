using UnityEngine;

namespace Core.Local_Map.Scripts
{
    public sealed class ArrowAnimator : MonoBehaviour
    {
        [SerializeField] 
        private Vector3 referenceScale;

        public void SetScale(float cellSize) => transform.localScale = referenceScale / cellSize; // we are set as the cell's child, so we need to increase our scale to compensate for the cell's scale
    }
}