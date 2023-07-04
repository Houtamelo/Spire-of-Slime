using Core.Local_Map.Scripts.Enums;
using Save_Management;
using Sirenix.OdinInspector;
using Utils.Async;
using Utils.Patterns;

namespace Core.Local_Map.Scripts.Events
{
    /// <summary> Events should be modular and responsible for managing the state of the game. </summary>
    public abstract class ScriptableLocalMapEvent : SerializedScriptableObject, ILocalMapEvent
    {
        public CleanString Key => name;
        public abstract IconType GetIconType(in Option<float> multiplier);
        public abstract bool AllowSaving { get; }
        public abstract CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier);
    }
}