using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Collections;
using Core.Utils.Math;
using JetBrains.Annotations;
using ListPool;

namespace Core.Combat.Scripts.Skills
{
    public ref struct ChargeStruct
    {
        private static readonly List<CharacterStateMachine> ReusableTargetList = new(capacity: 4);
        
        public ISkill Skill { get; }
        public CharacterStateMachine Caster { get; }

        public TSpan Charge;
        public CustomValuePooledList<TargetProperties> TargetsProperties;

        public ChargeStruct([NotNull] ISkill skill, CharacterStateMachine caster, CharacterStateMachine firstTarget)
        {
            Skill = skill;
            Caster = caster;

            TargetResolver targetResolver = new(skill, caster, firstTarget);
            ReusableTargetList.Clear();
            targetResolver.FillTargetList(ReusableTargetList);
            int count = ReusableTargetList.Count;
            
            TargetsProperties = new CustomValuePooledList<TargetProperties>(count);
            for (int index = 0; index < count; index++)
                TargetsProperties.Add(new TargetProperties(ReusableTargetList[index], skill));

            Charge = skill.Charge;
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