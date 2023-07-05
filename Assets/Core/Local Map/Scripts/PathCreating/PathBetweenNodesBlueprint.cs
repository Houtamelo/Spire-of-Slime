using Core.Local_Map.Scripts.Events;
using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Local_Map.Scripts.PathCreating
{
    public readonly struct PathBetweenNodesBlueprint
    {
        public readonly int Length;
        public readonly float PathAverageMultiplier;
        public readonly Option<(ILocalMapEvent mapEvent, float threat)> DangerEvent;
        public readonly Option<(ILocalMapEvent mapEvent, float threat)> RestEvent;
        public readonly TileInfo DangerCellInfo;
        public readonly TileInfo RestCellInfo;
        
        public PathBetweenNodesBlueprint(int length, float pathAverageMultiplier, Option<(ILocalMapEvent mapEvent, float threat)> dangerEvent,
            Option<(ILocalMapEvent mapEvent, float threat)> restEvent, TileInfo dangerCellInfo, TileInfo restCellInfo)
        {
            Length = length;
            PathAverageMultiplier = pathAverageMultiplier;
            DangerEvent = dangerEvent;
            RestEvent = restEvent;
            DangerCellInfo = dangerCellInfo;
            RestCellInfo = restCellInfo;
        }
    }
}