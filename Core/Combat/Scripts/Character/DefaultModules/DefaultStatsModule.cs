using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Interfaces.Modules;
using Utils.Collections;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultStatsModule : IStatsModule
    {
        private readonly CharacterStateMachine _owner;

        private DefaultStatsModule(CharacterStateMachine owner) => _owner = owner;

        public static DefaultStatsModule FromInitialSetup(CharacterStateMachine owner) =>
            new(owner)
            {
                BaseSpeed = owner.Script.Speed,
                BaseDamageLower = owner.Script.Damage.lower,
                BaseDamageUpper = owner.Script.Damage.upper,
                BaseAccuracy = owner.Script.Accuracy,
                BasePower = 1f,
                BaseCriticalChance = owner.Script.Critical,
                BaseDodge = owner.Script.Dodge
            };

        public static DefaultStatsModule FromRecord(CharacterStateMachine owner, CharacterRecord record) =>
            new(owner)
            {
                BaseAccuracy = record.BaseAccuracy,
                BaseCriticalChance = record.BaseCriticalChance,
                BaseDodge = record.BaseDodge,
                BaseSpeed = record.BaseSpeed,
                BaseDamageLower = record.BaseDamageLower,
                BaseDamageUpper = record.BaseDamageUpper,
                BasePower = record.BasePower
            };

    #region Speed
        public float BaseSpeed { get; set; }
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseSpeedModifiers = new(ModifierComparer.Instance);

        public void SubscribeSpeed(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
                _baseSpeedModifiers.Add(modifier);
            else
            {
                foreach (IBaseFloatAttributeModifier element in _baseSpeedModifiers)
                    if (element.SharedId == modifier.SharedId)
                        return;

                _baseSpeedModifiers.Add(modifier);
            }
        }

        public void UnsubscribeSpeed(IBaseFloatAttributeModifier modifier) => _baseSpeedModifiers.Remove(modifier);

        float IStatsModule.GetSpeedInternal()
        {
            float speed = BaseSpeed;
            foreach (IBaseFloatAttributeModifier modifier in _baseSpeedModifiers)
                modifier.Modify(ref speed, _owner);

            return speed;
        }
    #endregion

    #region Damage
        public uint BaseDamageLower { get; set; }
        public uint BaseDamageUpper { get; set; }
        
        public float BasePower { get; set; }
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _basePowerModifiers = new(ModifierComparer.Instance);

        public void SubscribePower(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
                _basePowerModifiers.Add(modifier);
            else
            {
                foreach (IBaseFloatAttributeModifier element in _basePowerModifiers)
                    if (element.SharedId == modifier.SharedId)
                        return;

                _basePowerModifiers.Add(modifier);
            }
        }

        public void UnsubscribePower(IBaseFloatAttributeModifier modifier) => _basePowerModifiers.Remove(modifier);

        float IStatsModule.GetPowerInternal()
        {
            float result = BasePower;
            foreach (IBaseFloatAttributeModifier modifier in _basePowerModifiers)
                modifier.Modify(ref result, _owner);

            return result;
        }
    #endregion

    #region Accuracy
        public float BaseAccuracy { get; set; }
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseAccuracyModifiers = new(ModifierComparer.Instance);

        public void SubscribeAccuracy(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
                _baseAccuracyModifiers.Add(modifier);
            else
            {
                foreach (IBaseFloatAttributeModifier element in _baseAccuracyModifiers)
                    if (element.SharedId == modifier.SharedId)
                        return;

                _baseAccuracyModifiers.Add(modifier);
            }

        }

        public void UnsubscribeAccuracy(IBaseFloatAttributeModifier modifier) => _baseAccuracyModifiers.Remove(modifier);

        float IStatsModule.GetAccuracyInternal()
        {
            float accuracy = BaseAccuracy;
            foreach (IBaseFloatAttributeModifier modifier in _baseAccuracyModifiers)
                modifier.Modify(ref accuracy, _owner);

            return accuracy;
        }
    #endregion

    #region CriticalChance
        public float BaseCriticalChance { get; set; }
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseCriticalChanceModifiers = new(ModifierComparer.Instance);

        public void SubscribeCriticalChance(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
                _baseCriticalChanceModifiers.Add(modifier);
            else
            {
                foreach (IBaseFloatAttributeModifier element in _baseCriticalChanceModifiers)
                    if (element.SharedId == modifier.SharedId)
                        return;

                _baseCriticalChanceModifiers.Add(modifier);
            }
        }

        public void UnsubscribeCriticalChance(IBaseFloatAttributeModifier modifier) => _baseCriticalChanceModifiers.Remove(modifier);

        float IStatsModule.GetCriticalChanceInternal()
        {
            float critical = BaseCriticalChance;
            foreach (IBaseFloatAttributeModifier modifier in _baseCriticalChanceModifiers)
                modifier.Modify(ref critical, _owner);

            return critical;
        }
    #endregion

    #region Dodge
        public float BaseDodge { get; set; }
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseDodgeModifiers = new(ModifierComparer.Instance);

        public void SubscribeDodge(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
                _baseDodgeModifiers.Add(modifier);
            else
            {
                foreach (IBaseFloatAttributeModifier element in _baseDodgeModifiers)
                    if (element.SharedId == modifier.SharedId)
                        return;

                _baseDodgeModifiers.Add(modifier);
            }
        }

        public void UnsubscribeDodge(IBaseFloatAttributeModifier modifier) => _baseDodgeModifiers.Remove(modifier);

        float IStatsModule.GetDodgeInternal()
        {
            float dodge = BaseDodge;
            foreach (IBaseFloatAttributeModifier modifier in _baseDodgeModifiers)
                modifier.Modify(ref dodge, _owner);

            return dodge;
        }
    #endregion
    }
}