using System;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    /// <summary>  These are temporary created from StatusScript in order to process Perks </summary>
    public abstract record StatusToApply(CharacterStateMachine Caster, CharacterStateMachine Target, bool FromCrit, ISkill Skill, IBaseStatusScript ScriptOrigin)
    {
        public CharacterStateMachine Caster { get; } = Caster;
        public CharacterStateMachine Target { get; } = Target;
        public bool FromCrit { get; } = FromCrit;
        public bool FromSkill { get; } = Skill != null;
        protected ISkill Skill { get; } = Skill;
        public ISkill GetSkill() => FromSkill ? Skill : throw new InvalidOperationException("Check if effect is from skill before accessing skill");
        public IBaseStatusScript ScriptOrigin { get; set; } = ScriptOrigin;
        
        public abstract string GetDescription();
        public abstract StatusResult ApplyEffect();
        protected abstract void ProcessModifiersInternal();
        private bool ModifiersProcessed { get; set; }
        
        public void ProcessModifiers()
        {
            if (ModifiersProcessed)
                return;
            
            ProcessModifiersInternal();
            ModifiersProcessed = true;
        }

        public abstract string GetCompactDescription();
    }
}