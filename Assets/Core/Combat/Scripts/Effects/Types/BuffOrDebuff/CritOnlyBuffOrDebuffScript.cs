using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
    public record CritOnlyBuffOrDebuffScript(bool Permanent, float BaseDuration, float BaseApplyChance, CombatStat Stat, float BaseDelta)
        : BuffOrDebuffScript(Permanent, BaseDuration, BaseApplyChance, Stat, BaseDelta)
    {
        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null)
        {
            if (!crit)
                return new StatusResult(caster, target, success: false, statusInstance: null, generatesInstance: true, EffectType);
            
            return base.ApplyEffect(caster, target, true, skill);
        }
    }
}