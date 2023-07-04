using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.Game_Manager.Scripts
{
    public class EnableOnSpecificScenes : MonoBehaviour
    {
        [SerializeField]
        private SceneEnum[] scenes;

        private void Start()
        {
            GameManager.OnRootEnabled += OnRootEnabled;
            GameManager.OnRootDisabled += OnRootDisabled;
            DeactivateIfNoScenesLoaded();
        }

        private void OnDestroy()
        {
            GameManager.OnRootEnabled -= OnRootEnabled;
            GameManager.OnRootDisabled -= OnRootDisabled;
        }

        private void OnRootEnabled(SceneRef scene)
        {
            foreach (SceneEnum sceneEnum in scenes)
            {
                if (sceneEnum.ToName() == scene)
                {
                    gameObject.SetActive(true);
                    break;
                }
            }
        }

        private void OnRootDisabled(SceneRef scene) => DeactivateIfNoScenesLoaded();

        private void DeactivateIfNoScenesLoaded()
        {
            foreach (SceneEnum sceneEnum in scenes)
            {
                Scene sceneStruct = SceneManager.GetSceneByName(sceneEnum.ToName().Name);
                if (sceneStruct.IsValid() && sceneStruct.isLoaded && sceneStruct.GetRootGameObjects().Any(obj => obj.activeInHierarchy))
                    return;
            }

            gameObject.SetActive(false);
        }
    }
}