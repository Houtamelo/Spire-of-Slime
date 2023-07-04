using Sirenix.OdinInspector;
using UnityEngine;
using Yarn.Unity;

namespace Core.Visual_Novel.Scripts
{
    public sealed class VisualNovelSceneEnabler : MonoBehaviour
    {
        [SerializeField, Required, SceneObjectsOnly] 
        private DialogueRunner dialogueRunner;
        
        [SerializeField, Required, SceneObjectsOnly] 
        private GameObject sceneRoot;

        private void Start()
        {
            dialogueRunner.onNodeStart.AddListener(OnNodeStart);
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
            sceneRoot.gameObject.SetActive(false);
        }

        private void OnDialogueComplete()
        {
            sceneRoot.gameObject.SetActive(false);
        }

        private void OnNodeStart(string _)
        {
            sceneRoot.gameObject.SetActive(true);
        }

        private void OnDestroy()
        {
            if (dialogueRunner == null)
                return;

            dialogueRunner.onNodeStart.RemoveListener(OnNodeStart);
            dialogueRunner.onDialogueComplete.RemoveListener(OnDialogueComplete);
        }
    }
}