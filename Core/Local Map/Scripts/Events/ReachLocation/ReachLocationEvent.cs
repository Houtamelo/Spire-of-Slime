using Core.Local_Map.Scripts.Enums;
using Core.World_Map.Scripts;
using UnityEngine;
using Utils.Async;
using Utils.Patterns;

namespace Core.Local_Map.Scripts.Events.ReachLocation
{
    public class ReachLocationEvent : ScriptableLocalMapEvent
    {
        [field: SerializeField] 
        public LocationEnum Location { get; protected set; }
        
        public override IconType GetIconType(in Option<float> multiplier) => IconType.ReachLocation;
        public override bool AllowSaving => false;

        public override CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier)
        {
            return ReachLocationEventHandler.HandleReachLocation(Location);
        }
    }
}