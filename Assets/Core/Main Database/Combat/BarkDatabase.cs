using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Barks;
using Core.Save_Management.SaveObjects;
using Core.Utils.Collections.Extensions;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace Core.Main_Database.Combat
{
    public class BarkDatabase : SerializedScriptableObject
    {
        public static DatabaseManager Instance => DatabaseManager.Instance;

        [OdinSerialize] 
        private Dictionary<(CleanString characterKey, BarkType), string[]> _barks = new(); // string[] is possible barks

        public static Option<string> GetBark(CleanString key, BarkType barkType)
        {
            if (Instance.BarkDatabase._barks.TryGetValue((key, barkType), out string[] barks) && !barks.IsNullOrEmpty())
                return barks.GetRandom();
            
            return Option<string>.None;
        }

#if UNITY_EDITOR        
        public void AssignData([NotNull] Dictionary<(string, BarkType), List<string>> barkDictionary)
        {
            _barks = barkDictionary.ToDictionary(e => ((CleanString)e.Key.Item1, e.Key.Item2), e => e.Value.ToArray());
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}