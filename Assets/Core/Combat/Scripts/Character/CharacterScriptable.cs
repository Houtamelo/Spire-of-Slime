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
using Core.Localization.Scripts;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Core.Combat.Scripts
{
    public abstract class CharacterScriptable : SerializedScriptableObject, ICharacterScript
    {
        [SerializeField, Range(1, 4)]
        private byte size = 1;
        public byte Size => size;

        [SerializeField, Range(0, 9.6f)] private float idleGraphicalX = 3.1f;
        public float IdleGraphicalX => idleGraphicalX;

        [SerializeField, AssetsOnly, Required]
        private GameObject rendererPrefab;
        public GameObject RendererPrefab => rendererPrefab;

        [SerializeField]
        private Race race;
        public Race Race => race;

        [SerializeField, Required]
        private Sprite timelineIcon;
        public Sprite TimelineIcon => timelineIcon;
        
        [SerializeField, Required]
        protected SkillScriptable[] allPossibleSkills = new SkillScriptable[0];
        public ReadOnlySpan<ISkill> GetAllPossibleSkills() => allPossibleSkills.HasElements() ? allPossibleSkills : Skills;

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

        [SerializeField, ShowIf(nameof(IsGirl))]
        private TSpan downedTime = TSpan.FromSeconds(4);
        public TSpan DownedTime => downedTime;
        
        [SerializeField, ShowIf(nameof(IsGirl))]
        private TSpan orgasmDuration = TSpan.FromSeconds(4);
        public TSpan OrgasmDuration => orgasmDuration;

        [SerializeField, Required]
        private Sprite portrait;
        public Option<Sprite> GetPortrait => portrait != null ? Option<Sprite>.Some(portrait) : Option.None;

        [SerializeField]
        private Color portraitBackgroundColor;
        public Option<Color> GetPortraitBackgroundColor => portraitBackgroundColor != Color.clear ? Option<Color>.Some(portraitBackgroundColor) : Option<Color>.None;

        public abstract Option<RuntimeAnimatorController> GetPortraitAnimation { get; }
        
        public virtual CleanString Key => name;
        public abstract LocalizedText CharacterName { get; }
        public abstract double ExpMultiplier { get; }
        
        public abstract (int lower, int upper) Damage { get; }
        public abstract int Speed { get; }

        public abstract int StunMitigation { get; }
        
        public abstract int Stamina { get; }
        public abstract int StaminaAmplitude { get; }
        public abstract int Resilience { get; }
        
        public abstract int DebuffResistance { get; }
        public abstract int DebuffApplyChance { get; }
        
        public abstract int MoveResistance { get; }
        public abstract int MoveApplyChance { get; }
        
        public abstract int PoisonResistance { get; }
        public abstract int PoisonApplyChance { get; }
        
        public abstract int ArousalApplyChance { get; }
        
        public abstract int Accuracy { get; }
        public abstract int CriticalChance { get; }
        public abstract int Dodge { get; }
        
        public virtual bool IsControlledByPlayer => false;
        public abstract bool IsGirl { get; }
        
        public abstract ReadOnlySpan<ISkill> Skills { get; }
        public abstract ReadOnlySpan<IPerk> GetStartingPerks { get; }
        
        public abstract int Lust { get; }
        public abstract int Temptation { get; }
        public abstract int OrgasmLimit { get; }
        public abstract int OrgasmCount { get; }
        public abstract int Composure { get; }

        public virtual Option<string> GetBark(BarkType barkType, CharacterStateMachine character) => BarkDatabase.GetBark(Key, barkType);
        
        public abstract Option<(string parameter, float graphicalX)> DoesActiveSex(CharacterStateMachine other);
        public abstract Option<float> GetSexGraphicalX(string parameter);

#if UNITY_EDITOR
        public void AssignAllPossibleSkills([NotNull] IEnumerable<SkillScriptable> possibleSkills)
        {
            allPossibleSkills = possibleSkills.ToArray();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}