using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.UI;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Action;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Effects.BaseTypes
{
    public abstract record StatusScript : IActualStatusScript
    {
        protected const int BonusApplyChanceOnCrit = 50;

        [NotNull]
        public IActualStatusScript GetActual => this;

        public abstract StatusToApply GetStatusToApply(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null);
        public abstract StatusResult ApplyEffect(CharacterStateMachine caster, CharacterStateMachine target, bool crit, ISkill skill = null);

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


        public abstract float ComputePoints(ref SkillStruct skillStruct, CharacterStateMachine target);

        public abstract EffectType EffectType { get; }
        public abstract string Description { get; }
        public virtual Option<PredictionIconsDisplay.IconType> GetPredictionIconType() => EffectType.GetPredictionIcon();

        public abstract bool PlaysBarkAppliedOnCaster { get; }
        public abstract bool PlaysBarkAppliedOnEnemy { get; }
        public abstract bool PlaysBarkAppliedOnAlly { get; }

        public abstract bool IsPositive { get; }
    }
}