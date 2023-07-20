using Core.Local_Map.Scripts.Enums;
using Core.Utils.Async;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Local_Map.Scripts.Events.ReachLocation
{
    public class ReachLocationEvent : ScriptableLocalMapEvent
    {
        [field: SerializeField] 
        public LocationEnum Location { get; protected set; }
        
        public override IconType GetIconType(in Option<float> multiplier) => IconType.ReachLocation;
        public override bool AllowSaving => false;

        [NotNull]
        public override CoroutineWrapper Execute(TileInfo tileInfo, in Option<float> multiplier) 
            => ReachLocationEventHandler.HandleReachLocation(Location);
    }
}