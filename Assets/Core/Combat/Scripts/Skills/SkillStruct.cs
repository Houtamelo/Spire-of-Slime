using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Collections;
using Core.Utils.Math;
using JetBrains.Annotations;
using ListPool;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Core.Combat.Scripts.Skills
{
    public ref struct SkillStruct
    {
        private static readonly List<CharacterStateMachine> ReusableTargetList = new(capacity: 4);
        
        public ISkill Skill { get; }
        public TargetResolver TargetResolver { get; }
        public CharacterStateMachine Caster { get; }
        public CharacterStateMachine FirstTarget { get; }

        public TSpan Recovery;
        public CustomValuePooledList<TargetProperties> TargetProperties;
        public CustomValuePooledList<IActualStatusScript> CasterEffects;
        public CustomValuePooledList<IActualStatusScript> TargetEffects;

        private SkillStruct([NotNull] ISkill skill, CharacterStateMachine caster, CharacterStateMachine firstTarget)
        {
            Skill = skill;
            Recovery = skill.Recovery;

            Caster = caster;
            FirstTarget = firstTarget;

            TargetResolver = new TargetResolver(skill, caster, firstTarget);
            ReusableTargetList.Clear();
            TargetResolver.FillTargetList(ReusableTargetList);
            
            int targetCount = ReusableTargetList.Count;
            
            TargetProperties = new CustomValuePooledList<TargetProperties>(targetCount);
            for (int i = 0; i < targetCount; i++)
                TargetProperties.Add(new TargetProperties(ReusableTargetList[i], skill));

            ReadOnlySpan<IBaseStatusScript> sourceCasterEffects = skill.CasterEffects;
            CasterEffects = new CustomValuePooledList<IActualStatusScript>(sourceCasterEffects.Length);
            for (int i = 0; i < sourceCasterEffects.Length; i++)
                CasterEffects.Add(sourceCasterEffects[i].GetActual);

            ReadOnlySpan<IBaseStatusScript> sourceTargetEffects = skill.TargetEffects;
            TargetEffects = new CustomValuePooledList<IActualStatusScript>(sourceTargetEffects.Length);
            for (int i = 0; i < sourceTargetEffects.Length; i++)
                TargetEffects.Add(sourceTargetEffects[i].GetActual);
            
            _allocated = true;
        }

        public void ApplyCustomStats()
        {
            foreach (ICustomSkillStat customStat in Skill.CustomStats)
                customStat.Apply(skillStruct: ref this);
        }

        public static SkillStruct CreateInstance([NotNull] ISkill skill, CharacterStateMachine caster, CharacterStateMachine firstTarget) => new(skill, caster, firstTarget);

        private bool _allocated;
     
        public void Dispose()
        {
            if (_allocated == false)
                return;

            _allocated = false;
            TargetProperties.Dispose();
            CasterEffects.Dispose();
            TargetEffects.Dispose();
        }
    }
}