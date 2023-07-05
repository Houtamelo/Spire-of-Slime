using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.UI;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public abstract record StatusScript : IActualStatusScript
    {
        protected const float BonusApplyChanceOnCrit = 0.5f;
        public abstract EffectType EffectType { get; }
        public abstract bool PlaysBarkAppliedOnCaster { get; }
        public abstract bool PlaysBarkAppliedOnEnemy { get; }
        public abstract bool PlaysBarkAppliedOnAlly { get; }
        public abstract bool IsPositive { get; }
        public abstract StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null);
        public abstract string Description { get; }
        public abstract float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target);
        public abstract StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null);
        public IActualStatusScript GetActual => this;
        public virtual Option<PredictionIconsDisplay.IconType> GetPredictionIconType() => EffectType.GetPredictionIcon();

        public virtual bool Equals(IActualStatusScript other)
        {
            bool isOtherNull = ReferenceEquals(null, other);
            bool isThisNull = ReferenceEquals(null,  this);
            
            if (isOtherNull && isThisNull)
                return true;
            
            if (isOtherNull || isThisNull)
                return false;
            
            if (other is StatusScript otherStatusScript)
                return Equals(otherStatusScript);
            
            return false;
        }
    }
}