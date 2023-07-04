using UnityEngine;
using UnityEngine.UI;

namespace Unused.Free_UI_build_package.Script
{
    public class Togglescript : MonoBehaviour
    {
        private Toggle _toggle;

        private void Start()
        {
            _toggle = GetComponent<Toggle>();
        }

        public GameObject slider;


        private void Update() { slider.SetActive(!_toggle.isOn); }
    }
}
