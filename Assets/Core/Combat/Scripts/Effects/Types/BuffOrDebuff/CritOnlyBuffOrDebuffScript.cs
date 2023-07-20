using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.Types.BuffOrDebuff
{
    public record CritOnlyBuffOrDebuffScript(bool Permanent, TSpan BaseDuration, int BaseApplyChance, CombatStat Stat, int BaseDelta)
        : BuffOrDebuffScript(Permanent, BaseDuration, BaseApplyChance, Stat, BaseDelta)
    {
        public override StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, [CanBeNull] ISkill skill = null)
        {
            if (crit == false)
                return new StatusResult(caster, target, success: false, statusInstance: null, generatesInstance: true, EffectType);
            
            return base.ApplyEffect(caster, target, crit: true, skill);
        }
    }
}