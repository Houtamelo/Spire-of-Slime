using Utils.Async;
using Utils.Patterns;

namespace Core.Local_Map.Scripts.Events
{
    public sealed class LocalMapEventHandler : Singleton<LocalMapEventHandler>
    {
        private ScriptableLocalMapEvent _currentEvent;
        private CoroutineWrapper _currentEventCoroutine;
        public bool IsEventRunning => _currentEvent != null && _currentEventCoroutine is {Running: true};
        public bool CurrentEventAllowsSaving => IsEventRunning && _currentEvent.AllowSaving;

        public CoroutineWrapper HandleEvent(ILocalMapEvent mapEvent, float multiplier, TileInfo tileInfo)
        {
            return mapEvent.Execute(tileInfo, Option<float>.Some(multiplier));
        }
    }
}