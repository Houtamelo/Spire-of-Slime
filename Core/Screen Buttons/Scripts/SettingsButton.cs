using Core.Pause_Menu.Scripts;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Utils.Patterns;

namespace Core.Screen_Buttons.Scripts
{
    public class SettingsButton : MonoBehaviour
    {
        [SerializeField, Required] 
        private Button button;

        private void Start()
        {
            button.onClick.AddListener(() =>
            {
                if (PauseMenuManager.AssertInstance(out PauseMenuManager pauseMenu))
                    pauseMenu.Open();
            });
        }
    }
}