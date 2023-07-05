using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Utils.Objects
{
    [ExecuteInEditMode]
    public class PositionDisplay : MonoBehaviour
    {
        [SerializeField, ShowInInspector]
        private Vector3 worldPosition;

        private void Update()
        {
            worldPosition = transform.position;
        }
    }
}