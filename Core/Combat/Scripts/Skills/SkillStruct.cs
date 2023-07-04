using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Interfaces;
using ListPool;

// ReSharper disable FieldCanBeMadeReadOnly.Global

namespace Core.Combat.Scripts.Skills
{
    public ref struct SkillStruct
    {
        public ISkill Skill { get; }
        public float Recovery;
        public ValueListPool<TargetProperties> TargetProperties;
        public ValueListPool<IActualStatusScript> CasterEffects;
        public ValueListPool<IActualStatusScript> TargetEffects;
        public readonly TargetResolver TargetResolver;
        public CharacterStateMachine Caster { get; }
        public CharacterStateMachine FirstTarget { get; }

        private SkillStruct(ISkill skill, CharacterStateMachine caster, CharacterStateMachine firstTarget)
        {
            Skill = skill;
            Recovery = skill.BaseRecovery;

            Caster = caster;
            TargetResolver = new TargetResolver(skill, caster, firstTarget);
            using (ValueListPool<CharacterStateMachine> targets = TargetResolver.GetTargetList())
            {
                int targetCount = targets.Count;
                ValueListPool<TargetProperties> targetProperties = new(targetCount);
                for (int i = 0; i < targetCount; i++)
                    targetProperties.Add(new TargetProperties(targets[i], skill));

                TargetProperties = targetProperties;
            }
            
            ValueListPool<IActualStatusScript> casterEffects = new(skill.CasterEffects.Count);
            for (int i = 0; i < skill.CasterEffects.Count; i++)
                casterEffects.Add(skill.CasterEffects[i].GetActual);
            
            CasterEffects = casterEffects;
            
            ValueListPool<IActualStatusScript> targetEffects = new(skill.TargetEffects.Count);
            for (int i = 0; i < skill.TargetEffects.Count; i++)
                targetEffects.Add(skill.TargetEffects[i].GetActual);
            
            TargetEffects = targetEffects;
            FirstTarget = firstTarget;
            _allocated = true;
        }

        public void ApplyCustomStats()
        {
            foreach (ICustomSkillStat customStat in Skill.CustomStats)
                customStat.Apply(skillStruct: ref this);
        }

        public static SkillStruct CreateInstance(ISkill skill, CharacterStateMachine caster, CharacterStateMachine firstTarget) => new(skill, caster, firstTarget);

        private bool _allocated;
        private bool Disposed
        {
            get => _allocated == false;
            set => _allocated = value == false;
        }

        public void Dispose()
        {
            if (Disposed)
                return;

            Disposed = true;
            TargetProperties.Dispose();
            CasterEffects.Dispose();
            TargetEffects.Dispose();
        }
    }
}