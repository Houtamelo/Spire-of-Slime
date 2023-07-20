using System.Text;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using UnityEngine;

namespace Core.Combat.Scripts.Skills
{
    /// <summary>
    /// Meant for adding extra functionality to a skill that the base system does not support.
    /// </summary>
    public abstract class CustomStatScriptable : ScriptableObject, ICustomSkillStat
    {
        protected static readonly StringBuilder SharedStringBuilder = new();
        public abstract void Apply(ref SkillStruct skillStruct);
        public abstract Option<string> GetDescription();
    }
}