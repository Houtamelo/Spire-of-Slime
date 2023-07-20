using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Skills;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Main_Database.Combat
{
    public sealed class SkillDatabase : ScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;
        
        [SerializeField, Required]
        private SkillScriptable[] allSkills;
        
        private readonly Dictionary<CleanString, SkillScriptable> _mappedSkills = new();

        [System.Diagnostics.Contracts.Pure]
        public static Option<SkillScriptable> GetSkill(CleanString skillKey) => Instance.SkillDatabase._mappedSkills.TryGetValue(skillKey, out SkillScriptable skill) ? Option<SkillScriptable>.Some(skill) : Option.None;

        [System.Diagnostics.Contracts.Pure]
        public static bool ContainsSkill(CleanString scriptKey) => Instance.SkillDatabase._mappedSkills.ContainsKey(scriptKey);

        public void Initialize()
        {
            foreach (SkillScriptable skill in allSkills)
                _mappedSkills.Add(skill.Key, skill);
            
            _mappedSkills.TrimExcess();
        }
        
#if UNITY_EDITOR        
        public void AssignData([NotNull] IEnumerable<SkillScriptable> skills)
        {
            allSkills = skills.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}