using System.Text;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Utils.Collections;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultResistancesRecord(int BaseDebuffResistance, int BaseMoveResistance, int BasePoisonResistance) : ResistancesRecord
    {
        [NotNull]
        public override IResistancesModule Deserialize(CharacterStateMachine owner) => DefaultResistancesModule.FromRecord(owner, record: this);
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    }
    
    public class DefaultResistancesModule : IResistancesModule
    {
        private readonly CharacterStateMachine _owner;

        private DefaultResistancesModule(CharacterStateMachine owner) => _owner = owner;

        [NotNull]
        public static DefaultResistancesModule FromInitialSetup([NotNull] CharacterStateMachine owner) =>
            new(owner)
            {
                BaseDebuffResistance = owner.Script.DebuffResistance,
                BaseMoveResistance = owner.Script.MoveResistance,
                BasePoisonResistance = owner.Script.PoisonResistance,
            };

        [NotNull]
        public static DefaultResistancesModule FromRecord(CharacterStateMachine owner, [NotNull] DefaultResistancesRecord record) =>
            new(owner)
            {
                BaseDebuffResistance = record.BaseDebuffResistance,
                BaseMoveResistance = record.BaseMoveResistance,
                BasePoisonResistance = record.BasePoisonResistance,
            };
        
        [NotNull]
        public ResistancesRecord GetRecord() => new DefaultResistancesRecord(BaseDebuffResistance, BaseMoveResistance, BasePoisonResistance);

    #region Debuff
        public int BaseDebuffResistance { get; set; }
        private readonly SelfSortingList<IBaseAttributeModifier> _baseDebuffResistanceModifiers = new(ModifierComparer.Instance);

        public void SubscribeDebuffResistance(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseDebuffResistanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseDebuffResistanceModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseDebuffResistanceModifiers.Add(modifier);
        }

        public void UnsubscribeDebuffResistance(IBaseAttributeModifier modifier)
        {
            _baseDebuffResistanceModifiers.Remove(modifier);
        }

        int IResistancesModule.GetDebuffResistanceInternal()
        {
            int debuffResistance = BaseDebuffResistance;
            foreach (IBaseAttributeModifier modifier in _baseDebuffResistanceModifiers)
                modifier.Modify(ref debuffResistance, _owner);

            return debuffResistance;
        }
    #endregion

    #region Move
        public int BaseMoveResistance { get; set; }
        private readonly SelfSortingList<IBaseAttributeModifier> _baseMoveResistanceModifiers = new(ModifierComparer.Instance);

        public void SubscribeMoveResistance(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseMoveResistanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseMoveResistanceModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseMoveResistanceModifiers.Add(modifier);
        }

        public void UnsubscribeMoveResistance(IBaseAttributeModifier modifier)
        {
            _baseMoveResistanceModifiers.Remove(modifier);
        }

        int IResistancesModule.GetMoveResistanceInternal()
        {
            int moveResistance = BaseMoveResistance;
            foreach (IBaseAttributeModifier modifier in _baseMoveResistanceModifiers)
                modifier.Modify(ref moveResistance, _owner);

            return moveResistance;
        }
    #endregion

    #region Poison
        public int BasePoisonResistance { get; set; }
        private readonly SelfSortingList<IBaseAttributeModifier> _basePoisonResistanceModifiers = new(ModifierComparer.Instance);

        int IResistancesModule.GetPoisonResistanceInternal()
        {
            int poisonResistance = BasePoisonResistance;
            foreach (IBaseAttributeModifier modifier in _basePoisonResistanceModifiers)
                modifier.Modify(ref poisonResistance, _owner);

            return poisonResistance;
        }
        
        public void SubscribePoisonResistance(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _basePoisonResistanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _basePoisonResistanceModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _basePoisonResistanceModifiers.Add(modifier);
        }
        
        public void UnsubscribePoisonResistance(IBaseAttributeModifier modifier)
        {
            _basePoisonResistanceModifiers.Remove(modifier);
        }
    #endregion
    }
}