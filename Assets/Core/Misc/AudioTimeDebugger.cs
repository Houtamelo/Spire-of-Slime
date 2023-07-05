using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Misc
{
    [ExecuteAlways]
    public class AudioTimeDebugger : MonoBehaviour
    {
        [SerializeField]
        private float time;

        [SerializeField]
        private AudioSource audioSource;
        
        private void Update()
        {
            if (audioSource != null)
                time = audioSource.time;
        }
        
        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
        }

        [Button]
        private void Schedule()
        {
            audioSource.PlayScheduled(AudioSettings.dspTime + 10d);
        }
    }
}