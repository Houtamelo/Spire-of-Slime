using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Core.Combat.Scripts.Skills;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;

namespace Main_Database.Combat
{
    public sealed class SkillDatabase : ScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;
        
        [SerializeField, Required]
        private SkillScriptable[] allSkills;
        
        private readonly Dictionary<CleanString, SkillScriptable> _mappedSkills = new();

        [Pure]
        public static Option<SkillScriptable> GetSkill(CleanString skillKey) => Instance.SkillDatabase._mappedSkills.TryGetValue(skillKey, out SkillScriptable skill) ? Option<SkillScriptable>.Some(skill) : Option.None;

        [Pure]
        public static bool ContainsSkill(CleanString scriptKey) => Instance.SkillDatabase._mappedSkills.ContainsKey(scriptKey);

        public void Initialize()
        {
            foreach (SkillScriptable skill in allSkills)
                _mappedSkills.Add(skill.Key, skill);
            
            _mappedSkills.TrimExcess();
        }
        
#if UNITY_EDITOR        
        public void AssignData(IEnumerable<SkillScriptable> skills)
        {
            allSkills = skills.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}