using System;
using System.Collections.Generic;
using System.Linq;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Main_Database.Combat;
using Save_Management;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Utils.Extensions;
using Utils.Patterns;

namespace Core.Combat.Scripts
{
    public abstract class CharacterScriptable : SerializedScriptableObject, ICharacterScript
    {
        [OdinSerialize, PropertyRange(1, 4)]
        public byte Size { get; private set; } = 1;

        [SerializeField, PropertyRange(0f, 9.6f)] private float idleGraphicalX = 3.1f;
        public float IdleGraphicalX => idleGraphicalX;

        [SerializeField, AssetsOnly, Required]
        private GameObject rendererPrefab;
        public GameObject RendererPrefab => rendererPrefab;
        
        [OdinSerialize]
        public Race Race { get; private set; }
        
        [OdinSerialize]
        public Sprite TimelineIcon { get; private set; }
        
        [OdinSerialize]
        public Sprite LustPromptPortrait { get; private set; }
        
        [SerializeField]
        protected SkillScriptable[] allPossibleSkills = new SkillScriptable[0];
        public IReadOnlyList<ISkill> GetAllPossibleSkills() => allPossibleSkills.IsNullOrEmpty() == false ? allPossibleSkills : Skills;

        [SerializeField]
        private bool becomesCorpseOnDefeat;

        [SerializeField, ShowIf(nameof(becomesCorpseOnDefeat))]
        private string corpseAnimationTrigger;

        public bool BecomesCorpseOnDefeat(out CombatAnimation combatAnimation)
        {
            if (becomesCorpseOnDefeat)
            {
                combatAnimation = new CombatAnimation(corpseAnimationTrigger, Option<CasterContext>.None, Option<TargetContext>.None);
                return true;
            }
            
            combatAnimation = default;
            return false;
        }

        [OdinSerialize, ShowIf(nameof(IsGirl))]
        public float DownedTime { get; private set; } = 4;
        
        [OdinSerialize, ShowIf(nameof(IsGirl))]
        public float OrgasmDuration { get; private set; } = 4;

        [SerializeField, Required]
        private Sprite portrait;
        public Option<Sprite> GetPortrait => portrait != null ? Option<Sprite>.Some(portrait) : Option<Sprite>.None;

        [SerializeField]
        private Color portraitBackgroundColor;
        public Option<Color> GetPortraitBackgroundColor => portraitBackgroundColor != Color.clear ? Option<Color>.Some(portraitBackgroundColor) : Option<Color>.None;

        public abstract Option<RuntimeAnimatorController> GetPortraitAnimation { get; }
        
        public virtual CleanString Key => name;
        public abstract string CharacterName { get; }
        public abstract float ExpMultiplier { get; }
        
        public abstract (uint lower, uint upper) Damage { get; }
        public abstract float Speed { get; }

        public abstract float StunRecoverySpeed { get; }
        
        public abstract uint Stamina { get; }
        public abstract uint StaminaAmplitude { get; }
        public abstract float Resilience { get; }
        
        public abstract float DebuffResistance { get; }
        public abstract float DebuffApplyChance { get; }
        
        public abstract float MoveResistance { get; }
        public abstract float MoveApplyChance { get; }
        
        public abstract float PoisonResistance { get; }
        public abstract float PoisonApplyChance { get; }
        
        public abstract float ArousalApplyChance { get; }
        
        public abstract float Accuracy { get; }
        public abstract float Critical { get; }
        public abstract float Dodge { get; }
        
        public virtual bool IsControlledByPlayer => false;
        public abstract bool IsGirl { get; }
        
        public abstract IReadOnlyList<ISkill> Skills { get; }
        public abstract ReadOnlySpan<IPerk> GetStartingPerks { get; }
        
        public abstract uint Lust { get; }
        public abstract ClampedPercentage Temptation { get; }
        public abstract uint OrgasmLimit { get; }
        public abstract uint OrgasmCount { get; }
        public abstract float Composure { get; }

        public virtual Option<string> GetBark(BarkType barkType, CharacterStateMachine character) => BarkDatabase.GetBark(Key, barkType);
        
        public abstract Option<(string parameter, float graphicalX)> DoesActiveSex(CharacterStateMachine other);
        public abstract Option<float> GetSexGraphicalX(string parameter);

#if UNITY_EDITOR
        public void AssignAllPossibleSkills(IEnumerable<SkillScriptable> possibleSkills)
        {
            allPossibleSkills = possibleSkills.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}