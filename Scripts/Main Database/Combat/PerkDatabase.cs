using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Perks;
using ListPool;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;

namespace Main_Database.Combat
{
    public sealed class PerkDatabase : ScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;
        
        [SerializeField, Required] 
        private PerkScriptable[] allPerks;
        
        private readonly Dictionary<CleanString, PerkScriptable> _mappedPerks = new();

        public static ReadOnlySpan<IPerk> GetPerks(ref ValueListPool<CleanString> perkKeys)
        {
            IPerk[] perks = new IPerk[perkKeys.Count];
            for (int index = 0; index < perkKeys.Count; index++)
            {
                CleanString key = perkKeys[index];
                if (Instance.PerkDatabase._mappedPerks.TryGetValue(key, out PerkScriptable perk))
                    perks[index] = perk;
                else
                    Debug.LogWarning($"Perk with key {key.ToString()} not found in database", context: Instance.PerkDatabase);
            }

            return perks;
        }

        public static Option<PerkScriptable> GetPerk(CleanString perkKey) 
            => Instance.PerkDatabase._mappedPerks.TryGetValue(perkKey, out PerkScriptable perk) ? Option<PerkScriptable>.Some(perk) : Option.None;

        public void Initialize()
        {
            foreach (PerkScriptable perk in allPerks)
                _mappedPerks.Add(perk.Key, perk);
            
            _mappedPerks.TrimExcess();
        }

#if UNITY_EDITOR        
        public void AssignData(IEnumerable<PerkScriptable> perks)
        {
            allPerks = perks.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}