using System.Collections.Generic;
using System.Linq;
using Core.ResourceManagement;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Core.Visual_Novel.Scripts.Animations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Utils.Patterns;

namespace Core.Main_Database.Visual_Novel
{
    public class CgDatabase : SerializedScriptableObject
    {
        [OdinSerialize, Required]
        private Dictionary<string, Sprite> _cgDictionary;

        [OdinSerialize, Required]
        private Dictionary<string, string> _animationFilePaths;

        private static CgDatabase Instance => DatabaseManager.Instance.CgDatabase;

        public static Option<Sprite> GetCg(string fileName) => GetCg(Instance, fileName);
        public static Option<Sprite> GetCg(CgDatabase database, string fileName)
        {
            fileName = fileName.ToAlphaNumericLower();
            if (database._cgDictionary.TryGetValue(fileName, out Sprite cg))
                return Option<Sprite>.Some(cg);

            return Option<Sprite>.None;
        }

        public Option<string> GetCgAnimationFilePath(string fileName) 
            => _animationFilePaths.TryGetValue(fileName.ToLowerInvariant(), out string filePath) ? Option<string>.Some(filePath) : Option<string>.None;

        public static Option<ResourceHandle<VisualNovelAnimation>> GetCgAnimationPrefab(string fileName)
        {
            CgDatabase database = Instance;
            Option<string> filePath = database.GetCgAnimationFilePath(fileName);
            if (filePath.IsSome)
                return ResourceHandle<VisualNovelAnimation>.Load(filePath.Value);
            
            return Option<ResourceHandle<VisualNovelAnimation>>.None;
        }

#if UNITY_EDITOR
        public void AssignData(List<Sprite> hashSet, Dictionary<string, string> animationFilePaths)
        {
            _cgDictionary = hashSet.ToDictionary(c => c.name.ToAlphaNumericLower(), c => c);
            _animationFilePaths = animationFilePaths;
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}