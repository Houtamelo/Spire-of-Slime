using System.Collections.Generic;
using System.Linq;
using Core.Local_Map.Scripts;
using Core.Local_Map.Scripts.Enums;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

// ReSharper disable Unity.RedundantHideInInspectorAttribute

namespace Core.Main_Database.Local_Map
{
    public sealed class TileInfoDatabase : ScriptableObject
    {
        [SerializeField, Required]
        private TileInfo[] allTiles;

        [SerializeField, Required]
        private TileInfo fallbackTileInfo;
        public static TileInfo FallbackTileInfo => Instance.TileInfoDatabase.fallbackTileInfo;

        private readonly Dictionary<CleanString, TileInfo> _mappedTiles = new();
        private Dictionary<BothWays, TileInfo> _walkableTiles;
        private Dictionary<BothWays, TileInfo> _obstacleTiles;
        private Dictionary<LocationEnum, TileInfo> _worldLocationTiles;

        private static DatabaseManager Instance => DatabaseManager.Instance;

        public static Option<TileInfo> GetObstacleInfo(in BothWays location)
            => Instance.TileInfoDatabase._obstacleTiles.TryGetValue(location, out TileInfo tileInfo) ? tileInfo : Option<TileInfo>.None;

        public static Option<TileInfo> GetWalkableTileInfo(in BothWays location)
            => Instance.TileInfoDatabase._walkableTiles.TryGetValue(location, out TileInfo tileInfo) ? tileInfo : Option<TileInfo>.None;
        
        public static Option<TileInfo> GetWorldLocationTileInfo(LocationEnum location) 
            => Instance.TileInfoDatabase._worldLocationTiles.TryGetValue(location, out TileInfo tileInfo) ? tileInfo : Option<TileInfo>.None;

        public static Option<TileInfo> GetTileInfo(CleanString key) => Instance.TileInfoDatabase._mappedTiles.TryGetValue(key, out TileInfo tileInfo)
            ? tileInfo : Option<TileInfo>.None;

        public static bool TileInfoExists(CleanString key) => Instance.TileInfoDatabase._mappedTiles.ContainsKey(key);

        public void Initialize()
        {
            foreach (TileInfo tile in allTiles)
                _mappedTiles.Add(tile.Key, tile);
            
            _mappedTiles.TrimExcess();
            _walkableTiles = allTiles
                .Where(t => t.Type == TileType.Walkable && t.GetBothWaysPath().IsOk)
                .ToDictionary(t => t.GetBothWaysPath().Value, t => t);
            
            _obstacleTiles = allTiles
                .Where(t => t.Type == TileType.Obstacle && t.GetBothWaysPath().IsOk)
                .ToDictionary(t => t.GetBothWaysPath().Value, t => t);
            
            _worldLocationTiles = allTiles
                .Where(t => t.Type == TileType.WorldLocation && t.GetWorldLocation().IsOk)
                .ToDictionary(t => t.GetWorldLocation().Value, t => t);
        }
        
#if UNITY_EDITOR
        public void AssignData([NotNull] IEnumerable<TileInfo> tileInfos)
        {
            allTiles = tileInfos.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}