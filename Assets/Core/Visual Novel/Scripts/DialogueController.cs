using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Utils.Patterns;
using Yarn.Unity;

namespace Core.Visual_Novel.Scripts
{
    public sealed class DialogueController : Singleton<DialogueController>
    {
        [OdinSerialize, Required, SceneObjectsOnly]
        private DialogueRunner _dialogueRunner;

        private DialogueTracker _currentDialogueFlag;
        
        public bool IsDialogueRunning => _dialogueRunner.IsDialogueRunning;

        protected override void Awake()
        {
            base.Awake();
            _dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
        }

        private void OnDialogueComplete()
        {
            if (_currentDialogueFlag is { IsDone: false })
                _currentDialogueFlag.SetDone(interrupted: false);
        }

        public DialogueTracker Play(string sceneName)
        {
            if (_currentDialogueFlag is { IsDone: false }) 
                _currentDialogueFlag.SetDone(interrupted: true);
            
            _currentDialogueFlag = new DialogueTracker();
            _dialogueRunner.Stop();
            _dialogueRunner.StartDialogue(sceneName);
            return _currentDialogueFlag;
        }
        
        public bool SceneExists(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return false;
            
            return _dialogueRunner.yarnProject.Program.Nodes.ContainsKey(sceneName);
        }

        public void Stop() => _dialogueRunner.Stop();
    }
}