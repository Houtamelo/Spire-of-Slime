using Core.Audio.Scripts.MusicControllers;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Audio.Scripts
{
    public class SetMusicOnEnable : MonoBehaviour
    {
        [SerializeField, Required]
        private MusicController controllerPrefab;
        
        [SerializeField]
        private float volume = 1;

        private void OnEnable()
        {
            if (MusicManager.AssertInstance(out MusicManager musicManager))
                musicManager.SetController(controllerPrefab, volume);
        }

        private void OnDisable()
        {
            if (MusicManager.Instance.TrySome(out MusicManager musicManager))
                musicManager.UnsetIfPlaying(controllerPrefab);
        }
    }
}