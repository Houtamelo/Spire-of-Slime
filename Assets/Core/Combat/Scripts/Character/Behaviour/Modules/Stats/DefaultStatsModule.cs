using System.Text;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Utils.Collections;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultStatsRecord(int BaseAccuracy, int BaseCriticalChance, int BaseDodge, int BaseSpeed, int BaseDamageLower, int BaseDamageUpper, int BasePower) : StatsRecord
    {
        [NotNull]
        public override IStatsModule Deserialize(CharacterStateMachine owner) => DefaultStatsModule.FromRecord(owner, record: this);
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    };
    
    public class DefaultStatsModule : IStatsModule
    {
        private readonly CharacterStateMachine _owner;

        private DefaultStatsModule(CharacterStateMachine owner) => _owner = owner;

        [NotNull]
        public static DefaultStatsModule FromInitialSetup([NotNull] CharacterStateMachine owner) =>
            new(owner)
            {
                BaseSpeed          = owner.Script.Speed,
                BaseDamageLower    = owner.Script.Damage.lower,
                BaseDamageUpper    = owner.Script.Damage.upper,
                BaseAccuracy       = owner.Script.Accuracy,
                BasePower          = 100,
                BaseCriticalChance = owner.Script.CriticalChance,
                BaseDodge          = owner.Script.Dodge
            };

        [NotNull]
        public static DefaultStatsModule FromRecord(CharacterStateMachine owner, [NotNull] DefaultStatsRecord record) =>
            new(owner)
            {
                BaseAccuracy       = record.BaseAccuracy,
                BaseCriticalChance = record.BaseCriticalChance,
                BaseDodge          = record.BaseDodge,
                BaseSpeed          = record.BaseSpeed,
                BaseDamageLower    = record.BaseDamageLower,
                BaseDamageUpper    = record.BaseDamageUpper,
                BasePower          = record.BasePower
            };
        
        [NotNull]
        public StatsRecord GetRecord() => new DefaultStatsRecord(BaseAccuracy, BaseCriticalChance, BaseDodge, BaseSpeed, BaseDamageLower, BaseDamageUpper, BasePower);

    #region Speed
        public int BaseSpeed { get; set; }
        private readonly SelfSortingList<IBaseAttributeModifier> _baseSpeedModifiers = new(ModifierComparer.Instance);

        public void SubscribeSpeed(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseSpeedModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseSpeedModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseSpeedModifiers.Add(modifier);
        }

        public void UnsubscribeSpeed(IBaseAttributeModifier modifier) => _baseSpeedModifiers.Remove(modifier);

        int IStatsModule.GetSpeedInternal()
        {
            int speed = BaseSpeed;
            foreach (IBaseAttributeModifier modifier in _baseSpeedModifiers)
                modifier.Modify(ref speed, _owner);

            return speed;
        }
    #endregion

    #region Damage
        public int BaseDamageLower { get; set; }
        public int BaseDamageUpper { get; set; }
        
        public int BasePower { get; set; }
        private readonly SelfSortingList<IBaseAttributeModifier> _basePowerModifiers = new(ModifierComparer.Instance);

        public void SubscribePower(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _basePowerModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _basePowerModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _basePowerModifiers.Add(modifier);
        }

        public void UnsubscribePower(IBaseAttributeModifier modifier) => _basePowerModifiers.Remove(modifier);

        int IStatsModule.GetPowerInternal()
        {
            int result = BasePower;
            foreach (IBaseAttributeModifier modifier in _basePowerModifiers)
                modifier.Modify(ref result, _owner);
            
            return result;
        }
    #endregion

    #region Accuracy
        public int BaseAccuracy { get; set; }
        private readonly SelfSortingList<IBaseAttributeModifier> _baseAccuracyModifiers = new(ModifierComparer.Instance);

        public void SubscribeAccuracy(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseAccuracyModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseAccuracyModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseAccuracyModifiers.Add(modifier);
        }

        public void UnsubscribeAccuracy(IBaseAttributeModifier modifier) => _baseAccuracyModifiers.Remove(modifier);

        int IStatsModule.GetAccuracyInternal()
        {
            int accuracy = BaseAccuracy;
            foreach (IBaseAttributeModifier modifier in _baseAccuracyModifiers)
                modifier.Modify(ref accuracy, _owner);

            return accuracy;
        }
    #endregion

    #region CriticalChance
        public int BaseCriticalChance { get; set; }
        private readonly SelfSortingList<IBaseAttributeModifier> _baseCriticalChanceModifiers = new(ModifierComparer.Instance);

        public void SubscribeCriticalChance(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseCriticalChanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseCriticalChanceModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseCriticalChanceModifiers.Add(modifier);
        }

        public void UnsubscribeCriticalChance(IBaseAttributeModifier modifier) => _baseCriticalChanceModifiers.Remove(modifier);

        int IStatsModule.GetCriticalChanceInternal()
        {
            int critical = BaseCriticalChance;
            foreach (IBaseAttributeModifier modifier in _baseCriticalChanceModifiers)
                modifier.Modify(ref critical, _owner);

            return critical;
        }
    #endregion

    #region Dodge
        public int BaseDodge { get; set; }
        private readonly SelfSortingList<IBaseAttributeModifier> _baseDodgeModifiers = new(ModifierComparer.Instance);

        public void SubscribeDodge(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseDodgeModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseDodgeModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseDodgeModifiers.Add(modifier);
        }

        public void UnsubscribeDodge(IBaseAttributeModifier modifier) => _baseDodgeModifiers.Remove(modifier);

        int IStatsModule.GetDodgeInternal()
        {
            int dodge = BaseDodge;
            foreach (IBaseAttributeModifier modifier in _baseDodgeModifiers)
                modifier.Modify(ref dodge, _owner);

            return dodge;
        }
    #endregion
    }
}