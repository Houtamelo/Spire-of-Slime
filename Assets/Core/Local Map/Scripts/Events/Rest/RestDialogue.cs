using UnityEngine;

namespace Core.Local_Map.Scripts.Events.Rest
{
    public class RestDialogue : ScriptableObject
    {
        [SerializeField]
        private string sceneName;
        public string SceneName => sceneName;

        [SerializeField]
        private int priority;
        public int Priority => priority;
        
        public virtual bool IsAvailable() => true;
    }
}