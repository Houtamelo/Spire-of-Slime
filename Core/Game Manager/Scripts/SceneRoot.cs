using UnityEngine;

namespace Core.Game_Manager.Scripts
{
    public class SceneRoot : MonoBehaviour
    {
        [SerializeField]
        private SceneEnum scene;
        
        private void OnEnable()
        {
            GameManager.NotifyRootEnabled(scene.ToName());
        }

        private void OnDisable()
        {
            GameManager.NotifyRootDisabled(scene.ToName());
        }
    }
}