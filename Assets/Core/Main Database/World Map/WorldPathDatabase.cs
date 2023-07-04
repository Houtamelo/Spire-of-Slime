using System;
using System.Collections.Generic;
using System.Linq;
using Core.World_Map.Scripts;
using KGySoft.CoreLibraries;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;
using Save = Save_Management.Save;

namespace Main_Database.World_Map
{
    public sealed class WorldPathDatabase : ScriptableObject
    {
        public static bool LOG;
        
        private static DatabaseManager Instance => DatabaseManager.Instance;

        [SerializeField, Required]
        private WorldPath[] allPaths;

        private readonly Dictionary<OneWay, WorldPath[]> _mappedPaths = new(capacity: Enum<LocationEnum>.GetValues().Length * (Enum<LocationEnum>.GetValues().Length - 1));

        public void Initialize()
        {
            LocationEnum[] locations = Enum<LocationEnum>.GetValues();
            for (int i = 0; i < locations.Length; i++)
            {
                LocationEnum origin = locations[i];
                for (int j = 0; j < locations.Length; j++)
                {
                    if (j == i)
                        continue;
                    
                    LocationEnum destination = locations[j];
                    OneWay way = new(origin, destination);
                    List<WorldPath> paths = new();
                    foreach (WorldPath path in allPaths)
                    {
                        OneWay pathWay = new(path.origin, path.destination);
                        if (way == pathWay || (path.IsBothWays && (BothWays)way == (BothWays)pathWay))
                            paths.Add(path);
                    }

                    WorldPath[] sortedArray = paths.ToArray();
                    Array.Sort(sortedArray, (a, b) => b.priority.CompareTo(a.priority));
                    _mappedPaths[way] = sortedArray;
                }
            }
            
#if UNITY_EDITOR
            foreach (WorldPath[] paths in _mappedPaths.Values)
                for (int i = 0; i < paths.Length - 1; i++)
                    if (paths[i] == paths[i + 1])
                        Debug.LogWarning($"Two paths with same priority and location: {paths[i].name} - {paths[i + 1].name}", context: paths[i]);
#endif
        }

        /// <summary> Returns only paths that start in origin. Key is destination. </summary>
        public static Dictionary<LocationEnum, WorldPath> GetAvailablePathsFrom(LocationEnum origin)
        {
            WorldPathDatabase database = Instance.WorldPathDatabase;
            Dictionary<LocationEnum, WorldPath> paths = new();
            if (Save.AssertInstance(out Save save) == false)
                return paths;

            foreach ((OneWay way, WorldPath[] sortedPaths) in database._mappedPaths)
            {
                if (way.Origin != origin)
                    continue;
                
                foreach (WorldPath candidate in sortedPaths)
                {
                    if (candidate.AreRequirementsMet(save))
                    {
                        paths[way.Destination] = candidate;
                        break;
                    }
                }
            }

            if (LOG)
            {
                Debug.Log($"Paths found: \n    {paths.ElementsToString()}");
            }
            
            return paths;
        }

#if UNITY_EDITOR        
        public void AssignData(IEnumerable<WorldPath> worldPaths)
        {
            allPaths = worldPaths.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}