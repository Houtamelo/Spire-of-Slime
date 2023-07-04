using UnityEngine;
using UnityEngine.UI;

namespace Utils.Objects
{
    [RequireComponent(typeof(ToggleGroup))]
    public class ToggleChildrenOffOnDisable : MonoBehaviour
    {
        private Toggle[] _children;

        private void Start()
        {
            _children = GetComponentsInChildren<Toggle>(includeInactive: true);
        }

        private void OnDisable()
        {
            foreach (Toggle child in _children)
                child.isOn = false;
        }
    }
}