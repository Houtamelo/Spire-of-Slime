using Core.Combat.Scripts.BackgroundGeneration;
using Core.Local_Map.Scripts;
using Core.Main_Database.Combat;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using Sirenix.OdinInspector;

namespace Core.Main_Database.Local_Map
{
    public sealed class PathDatabase: SerializedScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;
        
        public static Option<PathInfo> GetPathInfo(BothWays path)
        {
            Option<CombatBackground> background = BackgroundDatabase.GetBackgroundPrefab(path);
            if (background.IsNone)
                return Option<PathInfo>.None;
            
            TileInfo walkableTile = TileInfoDatabase.GetWalkableTileInfo(path).TrySome(out walkableTile) ? walkableTile : TileInfoDatabase.FallbackTileInfo;
            TileInfo obstacle = TileInfoDatabase.GetObstacleInfo(path).TrySome(out obstacle) ? obstacle : TileInfoDatabase.FallbackTileInfo;
            return Option<PathInfo>.Some(new PathInfo(walkableTile, obstacle, background.Value, path));
        }
    }
}