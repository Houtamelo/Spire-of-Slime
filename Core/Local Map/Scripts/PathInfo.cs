using Core.Combat.Scripts.BackgroundGeneration;
using Core.World_Map.Scripts;

namespace Core.Local_Map.Scripts
{
    public readonly struct PathInfo
    {
        public readonly TileInfo WalkableTileInfo;
        public readonly TileInfo ObstacleInfo;
        public readonly CombatBackground BackgroundPrefab;
        public readonly BothWays Location;

        public PathInfo(TileInfo walkableTileInfo, TileInfo obstacleInfo, CombatBackground backgroundPrefab, BothWays location)
        {
            WalkableTileInfo = walkableTileInfo;
            ObstacleInfo = obstacleInfo;
            BackgroundPrefab = backgroundPrefab;
            Location = location;
        }
    }
}