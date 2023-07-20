using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.BackgroundGeneration;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using Core.World_Map.Scripts;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Main_Database.Combat
{
    public sealed class BackgroundDatabase : ScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;
        
        [SerializeField, Required]
        private CombatBackground[] allBackgrounds;
        
        private readonly Dictionary<CleanString, CombatBackground> _mappedBackgrounds = new();

        public static Option<CombatBackground> SpawnBackground(Transform parent, BothWays bothWays)
        {
            BackgroundDatabase backgroundDatabase = Instance.BackgroundDatabase;
            for (int index = 0; index < backgroundDatabase.allBackgrounds.Length; index++)
            {
                CombatBackground background = backgroundDatabase.allBackgrounds[index];
                Option<BothWays> location = background.GetLocation;
                if (location.IsSome && location.Value == bothWays)
                {
                    CombatBackground spawned = Instantiate(background, parent, worldPositionStays: true);
                    return spawned;
                }
            }
            
            return Option<CombatBackground>.None;
        }

        public static Option<CombatBackground> SpawnBackground(Transform parent, CleanString key)
        {
            BackgroundDatabase backgroundDatabase = Instance.BackgroundDatabase;
            if (backgroundDatabase._mappedBackgrounds.TryGetValue(key, out CombatBackground background) == false)
                return Option.None;
            
            CombatBackground spawned = Instantiate(background, parent, worldPositionStays: true);
            return spawned;
        }

        [System.Diagnostics.Contracts.Pure]
        public static Option<CombatBackground> GetBackgroundPrefab(CleanString key) 
            => Instance.BackgroundDatabase._mappedBackgrounds.TryGetValue(key, out CombatBackground background) ? Option<CombatBackground>.Some(background) : Option.None;

        [System.Diagnostics.Contracts.Pure]
        public static Option<CombatBackground> GetBackgroundPrefab(BothWays path)
        {
            BackgroundDatabase backgroundDatabase = Instance.BackgroundDatabase;
            for (int index = 0; index < backgroundDatabase.allBackgrounds.Length; index++)
            {
                CombatBackground background = backgroundDatabase.allBackgrounds[index];
                Option<BothWays> location = background.GetLocation;
                if (location.IsSome && location.Value == path)
                    return background;
            }
            
            return Option<CombatBackground>.None;
        }

        public void Initialize()
        {
            foreach (CombatBackground background in allBackgrounds)
                _mappedBackgrounds.Add(background.Key, background);
            
            _mappedBackgrounds.TrimExcess();
        }

#if UNITY_EDITOR
        public void AssignData([NotNull] IEnumerable<CombatBackground> backgrounds)
        {
            allBackgrounds = backgrounds.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}