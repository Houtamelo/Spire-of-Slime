using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.UI;
using Core.Save_Management.SaveObjects;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Core.Combat.Scripts.Perks
{
    public abstract class PerkScriptable : SerializedScriptableObject, IPerk
    {
        public CleanString Key => name;

        [OdinSerialize]
        public bool IsPositive { get; private set; }

        [OdinSerialize]
        public PerkScriptable[] PerkPrerequisites { get; private set; } = new PerkScriptable[0];

        [OdinSerialize]
        public SkillScriptable[] SkillPrerequisites { get; private set; } = new SkillScriptable[0];

        [field: SerializeField]
        public Sprite Icon { get; private set; }

        [field: SerializeField, HideInInspector]
        public string DisplayName { get; private set; }

        [field: SerializeField, HideInInspector]
        public string Description { get; private set; }

        [field: SerializeField, HideInInspector]
        public string DescriptionWithRichText { get; private set; }

        [field: SerializeField, HideInInspector]
        public string FlavorText { get; private set; }

        public abstract PerkInstance CreateInstance(CharacterStateMachine character);

#if UNITY_EDITOR
        public void AssignData(string displayName, string flavorText, string description)
        {
            DisplayName = displayName;
            Description = description;
            DescriptionWithRichText = RichTextKeywords.InsertRichText(description);
            FlavorText = flavorText;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        public void AssignIcon(Sprite icon)
        {
            Icon = icon;
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void OnValidate()
        {
            if (name.StartsWith("perk_") == false)
            {
                Debug.Log("Warning, file name does not start with \"perk_\"", context: this);
            }
        }
#endif
    }
}