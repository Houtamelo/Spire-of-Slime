using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Utils.Patterns;

namespace Main_Database.Visual_Novel
{
    public class PortraitDatabase : SerializedScriptableObject
    {
        private static PortraitDatabase Instance => DatabaseManager.Instance.PortraitDatabase;
        
        [OdinSerialize, Required]
        private Dictionary<string, (Sprite portrait, bool isLeftSide)> _portraitDictionary;

        public static Option<(Sprite portrait, bool isLeftSide)> GetPortrait(string fileName)
        {
            PortraitDatabase database = Instance;
            if (database._portraitDictionary.TryGetValue(fileName, out (Sprite portrait, bool isLeftSide) tuple))
                return tuple;
            
            return Option<(Sprite portrait, bool isLeftSide)>.None;
        }

#if UNITY_EDITOR        
        public void AssignData(HashSet<Sprite> portraitSprites)
        {
            _portraitDictionary = new(); // portraitSprites.ToDictionary(sprite => sprite.name);
            
            foreach (Sprite sprite in portraitSprites)
            {
                if (sprite.name.ToLowerInvariant().StartsWith("nema"))
                    _portraitDictionary.Add(sprite.name, (sprite, true));
                else if (sprite.name.ToLowerInvariant().StartsWith("ethel") || sprite.name.ToLowerInvariant().StartsWith("mistress-tender"))
                    _portraitDictionary.Add(sprite.name, (sprite, false));
                else
                    Debug.LogWarning($"Portrait {sprite.name} is not assigned to any character", sprite);
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        public bool PortraitExists(string fileName) => _portraitDictionary.ContainsKey(fileName);
#endif
    }
}