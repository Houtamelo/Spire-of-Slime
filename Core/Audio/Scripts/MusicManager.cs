using Core.Audio.Scripts.MusicControllers;
using Core.Local_Map.Scripts;
using Core.World_Map.Scripts;
using Main_Database.Audio;
using Main_Database.World_Map;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;

namespace Core.Audio.Scripts
{
    public sealed class MusicManager : Singleton<MusicManager>
    {
        public const float FadeDuration = 1f;
        
        [SerializeField, Required]
        private Transform musicControllersParent;
        
        private MusicController _activeController;
        private MusicEvent _latestEvent;  
        public CleanString CurrentMusicKey => _activeController != null ? _activeController.Key : string.Empty;

        public void SetController(MusicController controllerPrefab, float volume = 1f)
        {
            if (_activeController == null)
            {
                _activeController = Instantiate(controllerPrefab, musicControllersParent);
                _activeController.SetState(_latestEvent);
            }
            else if (_activeController.Key != controllerPrefab.Key)
            {
                _activeController.FadeDownAndDestroy(FadeDuration);
                _activeController = Instantiate(controllerPrefab, musicControllersParent);
                _activeController.SetState(_latestEvent);
            }
            
            _activeController.SetVolume(volume);
        }

        public void SetController(CleanString key, float volume = 1f)
        {
            if (MusicDatabase.GetController(key).AssertSome(out MusicController controller))
            {
                SetController(controller, volume);
                return;
            }

            Debug.LogWarning($"No music controller found for key {key}");
            if (_activeController != null)
            {
                _activeController.FadeDownAndDestroy(FadeDuration);
                _activeController = null;
            }
        }

        public void UnsetIfPlaying(MusicController prefab)
        {
            if (_activeController != null && _activeController.Key == prefab.Key)
            {
                _activeController.FadeDownAndDestroy(FadeDuration);
                _activeController = null;
            }
        }

        public void NotifyEvent(MusicEvent musicEvent)
        {
            _latestEvent = musicEvent;
            if (_activeController != null)
                _activeController.SetState(musicEvent);
        }

        public void LocalMapEnds()
        {
            if (_activeController != null && _activeController.BelongsToLocalMap.IsSome)
            {
                _activeController.FadeDownAndDestroy(FadeDuration);
                _activeController = null;
            }
        }

        public void StopAny()
        {
            if (_activeController != null)
            {
                _activeController.FadeDownAndDestroy(FadeDuration);
                _activeController = null;
            }
        }

        public void AllowLocalMapOrCombatMusic(bool value)
        {
            (bool enable, bool isActiveNull) tuple = (value, _activeController == null);
            switch (tuple)
            {
                case (enable: true, isActiveNull: false) when _activeController.BelongsToLocalMap.IsNone:
                {
                    _activeController.FadeDownAndDestroy(FadeDuration);
                    _activeController = null;
                    SearchForControllerInDatabase();
                    break;
                }
                case (enable: true, isActiveNull: true):
                {
                    SearchForControllerInDatabase();
                    break;
                }
                case (enable: false, isActiveNull: false) when _activeController.BelongsToLocalMap.IsSome:
                {
                    _activeController.FadeDownAndDestroy(FadeDuration);
                    _activeController = null;
                    break;
                }
            }

            void SearchForControllerInDatabase()
            {
                if (LocalMapManager.Instance.TrySome(out LocalMapManager localMapManager) == false)
                    return;

                if (localMapManager.CurrentSource.TrySome(out WorldPath source))
                {
                    SetController(source.MusicController);
                }
                else
                {
                    Option<MusicController> controllerOption = MusicDatabase.GetStandardControllerForPath(new BothWays(localMapManager.Origin, localMapManager.Destination));
                    if (controllerOption.AssertSome(out MusicController controller))
                        SetController(controller);
                }
            }
        }
    }
}