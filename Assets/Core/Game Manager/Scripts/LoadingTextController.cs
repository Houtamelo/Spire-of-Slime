using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Game_Manager.Scripts
{
    public class LoadingTextController : MonoBehaviour
    {
        [SerializeField, Required, SceneObjectsOnly]
        private GameObject loadingText;

        [SerializeField, Required, SceneObjectsOnly]
        private Image fadePanel;
        private void Update()
        {
            loadingText.SetActive(fadePanel.color.a >= 0.9f);
        }
    }
}