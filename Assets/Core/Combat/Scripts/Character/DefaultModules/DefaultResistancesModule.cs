using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Utils.Collections;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultResistancesModule : IResistancesModule
    {
        private readonly CharacterStateMachine _owner;

        private DefaultResistancesModule(CharacterStateMachine owner) => _owner = owner;

        public static DefaultResistancesModule FromInitialSetup(CharacterStateMachine owner) =>
            new(owner)
            {
                BaseDebuffResistance = owner.Script.DebuffResistance,
                BaseMoveResistance = owner.Script.MoveResistance,
                BasePoisonResistance = owner.Script.PoisonResistance,
                BaseStunRecoverySpeed = owner.Script.StunRecoverySpeed
            };

        public static DefaultResistancesModule FromRecord(CharacterStateMachine owner, CharacterRecord record) =>
            new(owner)
            {
                BaseDebuffResistance = record.BaseDebuffResistance,
                BaseMoveResistance = record.BaseMoveResistance,
                BasePoisonResistance = record.BasePoisonResistance,
                BaseStunRecoverySpeed = record.BaseStunRecoverySpeed
            };

    #region Debuff
        public float BaseDebuffResistance { get; set; }
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseDebuffResistanceModifiers = new(ModifierComparer.Instance);

        public void SubscribeDebuffResistance(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
                _baseDebuffResistanceModifiers.Add(modifier);
            else
            {
                foreach (IBaseFloatAttributeModifier element in _baseDebuffResistanceModifiers)
                    if (element.SharedId == modifier.SharedId)
                        return;

                _baseDebuffResistanceModifiers.Add(modifier);
            }
        }

        public void UnsubscribeDebuffResistance(IBaseFloatAttributeModifier modifier)
        {
            _baseDebuffResistanceModifiers.Remove(modifier);
        }

        float IResistancesModule.GetDebuffResistanceInternal()
        {
            float debuffResistance = BaseDebuffResistance;
            foreach (IBaseFloatAttributeModifier modifier in _baseDebuffResistanceModifiers)
                modifier.Modify(ref debuffResistance, _owner);

            return debuffResistance;
        }
    #endregion

    #region Move
        public float BaseMoveResistance { get; set; }
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseMoveResistanceModifiers = new(ModifierComparer.Instance);

        public void SubscribeMoveResistance(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
                _baseMoveResistanceModifiers.Add(modifier);
            else
            {
                foreach (IBaseFloatAttributeModifier element in _baseMoveResistanceModifiers)
                    if (element.SharedId == modifier.SharedId)
                        return;

                _baseMoveResistanceModifiers.Add(modifier);
            }
        }

        public void UnsubscribeMoveResistance(IBaseFloatAttributeModifier modifier)
        {
            _baseMoveResistanceModifiers.Remove(modifier);
        }

        float IResistancesModule.GetMoveResistanceInternal()
        {
            float moveResistance = BaseMoveResistance;
            foreach (IBaseFloatAttributeModifier modifier in _baseMoveResistanceModifiers)
                modifier.Modify(ref moveResistance, _owner);

            return moveResistance;
        }
    #endregion

    #region Poison
        public float BasePoisonResistance { get; set; }
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _basePoisonResistanceModifiers = new(ModifierComparer.Instance);

        float IResistancesModule.GetPoisonResistanceInternal()
        {
            float poisonResistance = BasePoisonResistance;
            foreach (IBaseFloatAttributeModifier modifier in _basePoisonResistanceModifiers)
                modifier.Modify(ref poisonResistance, _owner);

            return poisonResistance;
        }
        
        public void SubscribePoisonResistance(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
                _basePoisonResistanceModifiers.Add(modifier);
            else
            {
                foreach (IBaseFloatAttributeModifier element in _basePoisonResistanceModifiers)
                    if (element.SharedId == modifier.SharedId)
                        return;

                _basePoisonResistanceModifiers.Add(modifier);
            }
        }
        
        public void UnsubscribePoisonResistance(IBaseFloatAttributeModifier modifier)
        {
            _basePoisonResistanceModifiers.Remove(modifier);
        }
    #endregion

    #region StunRecoverySpeed
        public float BaseStunRecoverySpeed { get; set; }
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseStunRecoverySpeedModifiers = new(ModifierComparer.Instance);

        public void SubscribeStunRecoverySpeed(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
                _baseStunRecoverySpeedModifiers.Add(modifier);
            else
            {
                foreach (IBaseFloatAttributeModifier element in _baseStunRecoverySpeedModifiers)
                    if (element.SharedId == modifier.SharedId)
                        return;

                _baseStunRecoverySpeedModifiers.Add(modifier);
            }
        }

        public void UnsubscribeStunRecoverySpeed(IBaseFloatAttributeModifier modifier)
        {
            _baseStunRecoverySpeedModifiers.Remove(modifier);
        }

        float IResistancesModule.GetStunRecoverySpeedInternal()
        {
            float recoverySpeed = BaseStunRecoverySpeed;
            foreach (IBaseFloatAttributeModifier modifier in _baseStunRecoverySpeedModifiers)
                modifier.Modify(ref recoverySpeed, _owner);

            return recoverySpeed;
        }
    #endregion
    }
}