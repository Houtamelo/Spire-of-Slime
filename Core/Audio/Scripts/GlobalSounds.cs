using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Objects;
using Utils.Patterns;

namespace Core.Audio.Scripts
{
    public class GlobalSounds : Singleton<GlobalSounds>
    {
        [SerializeField, Required, SceneObjectsOnly]
        private CustomAudioSource lustReduction;
        public CustomAudioSource LustReduction => lustReduction;

        [SerializeField, Required, SceneObjectsOnly]
        private CustomAudioSource newGame;
        public CustomAudioSource NewGame => newGame;
        public CustomAudioSource EnteringLocalMap => newGame;
        public CustomAudioSource LoadGame => newGame;
    }
}