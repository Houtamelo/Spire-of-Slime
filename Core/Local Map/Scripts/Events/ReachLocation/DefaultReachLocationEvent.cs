using Core.Local_Map.Scripts.Enums;
using Core.World_Map.Scripts;
using KGySoft.CoreLibraries;
using Save_Management;
using Utils.Async;
using Utils.Patterns;

namespace Core.Local_Map.Scripts.Events.ReachLocation
{
    public class DefaultReachLocationEvent : ILocalMapEvent
    {
        public CleanString Key { get; }
        public readonly LocationEnum Location;

        public DefaultReachLocationEvent(LocationEnum location)
        {
            Key = $"reach-location_{Enum<LocationEnum>.ToString(location)}_default";
            Location = location;
        }
        
        public bool AllowSaving => false;

        public IconType GetIconType(in Option<float> multiplier) => IconType.ReachLocation;

        public CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier)
        {
            return ReachLocationEventHandler.HandleReachLocation(Location);
        }
    }
}