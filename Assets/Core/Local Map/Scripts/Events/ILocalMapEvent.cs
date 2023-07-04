using Core.Local_Map.Scripts.Enums;
using Save_Management;
using Utils.Async;
using Utils.Patterns;

namespace Core.Local_Map.Scripts.Events
{
    public interface ILocalMapEvent
    {
        CleanString Key { get; }
        bool AllowSaving { get; }
        IconType GetIconType(in Option<float> multiplier);
        CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier);
    }
}