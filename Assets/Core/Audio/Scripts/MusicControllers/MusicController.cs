using Core.World_Map.Scripts;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;

namespace Core.Audio.Scripts.MusicControllers
{
    public abstract class MusicController : MonoBehaviour
    {
        public CleanString Key => name;

        [SerializeField]
        private bool belongsToLocalMap;

        [SerializeField, ShowIf(nameof(belongsToLocalMap))]
        private BothWays location;
        
        public Option<BothWays> BelongsToLocalMap => belongsToLocalMap ? location : Option.None;
        
        public MusicEvent State { get; protected set; }
        public abstract void SetState(MusicEvent newState);
        public abstract void FadeDownAndDestroy(float duration);
        public abstract void SetVolume(float volume);
    }
}