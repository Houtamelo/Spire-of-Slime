using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Interfaces;
using ListPool;

namespace Core.Combat.Scripts.Skills
{
    public ref struct ChargeStruct
    {
        public ISkill Skill { get; }
        public CharacterStateMachine Caster { get; }
        public ValueListPool<TargetProperties> TargetsProperties;
        public float Charge;

        public ChargeStruct(ISkill skill, CharacterStateMachine caster, CharacterStateMachine firstTarget)
        {
            Skill = skill;
            Caster = caster;
            
            TargetResolver targetResolver = new(skill, caster, firstTarget);
            using ValueListPool<CharacterStateMachine> targets = targetResolver.GetTargetList();
            {
                int count = targets.Count;
                ValueListPool<TargetProperties> targetProperties = new(count);
                for (int index = 0; index < count; index++)
                {
                    CharacterStateMachine target = targets[index];
                    targetProperties.Add(new TargetProperties(target, skill));
                }

                TargetsProperties = targetProperties;
            }
            
            Charge = skill.BaseCharge;
            _unDisposed = true;
        }

        private bool _unDisposed;
        private bool Disposed
        {
            get => !_unDisposed;
            set => _unDisposed = !value;
        }

        public void Dispose()
        {
            if (Disposed)
                return;

            Disposed = true;
            TargetsProperties.Dispose();
        }
    }
}