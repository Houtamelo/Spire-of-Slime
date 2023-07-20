using System.Text;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.Arousal;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Effects.Types.Guarded;
using Core.Combat.Scripts.Effects.Types.Heal;
using Core.Combat.Scripts.Effects.Types.Lust;
using Core.Combat.Scripts.Effects.Types.Marked;
using Core.Combat.Scripts.Effects.Types.Move;
using Core.Combat.Scripts.Effects.Types.OvertimeHeal;
using Core.Combat.Scripts.Effects.Types.Poison;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Effects.Types.Stun;
using Core.Combat.Scripts.Effects.Types.Summon;
using Core.Combat.Scripts.Effects.Types.Tempt;
using Core.Utils.Collections;
using JetBrains.Annotations;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultStatusApplierRecord(int BaseDebuffApplyChance, int BaseMoveApplyChance, int BasePoisonApplyChance, int BaseArousalApplyChance) : StatusApplierRecord
    {
        [NotNull]
        public override IStatusApplierModule Deserialize(CharacterStateMachine owner) => DefaultStatusApplierModule.FromRecord(owner, record: this);
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    }

    public class DefaultStatusApplierModule : IStatusApplierModule
    {
        private readonly CharacterStateMachine _owner;

        private DefaultStatusApplierModule(CharacterStateMachine owner) => _owner = owner;

        [NotNull]
        public static DefaultStatusApplierModule FromInitialSetup([NotNull] CharacterStateMachine owner) =>
            new(owner)
            {
                BaseDebuffApplyChance = owner.Script.DebuffApplyChance,
                BaseMoveApplyChance = owner.Script.MoveApplyChance,
                BasePoisonApplyChance = owner.Script.PoisonApplyChance,
                BaseArousalApplyChance = owner.Script.ArousalApplyChance
            };

        [NotNull]
        public static DefaultStatusApplierModule FromRecord(CharacterStateMachine owner, [NotNull] DefaultStatusApplierRecord record) =>
            new(owner)
            {
                BaseDebuffApplyChance = record.BaseDebuffApplyChance,
                BaseMoveApplyChance = record.BaseMoveApplyChance,
                BasePoisonApplyChance = record.BasePoisonApplyChance,
                BaseArousalApplyChance = record.BaseArousalApplyChance
            };
        
        [NotNull]
        public StatusApplierRecord GetRecord() => new DefaultStatusApplierRecord(BaseDebuffApplyChance, BaseMoveApplyChance, BasePoisonApplyChance, BaseArousalApplyChance);

    #region Arousal
        public int BaseArousalApplyChance { get; set; }
        public SelfSortingList<IArousalModifier> ArousalApplyModifiers { get; } = new(ModifierComparer.Instance);
        private readonly SelfSortingList<IBaseAttributeModifier> _baseArousalApplyChanceModifiers = new(ModifierComparer.Instance);

        public void ModifyEffectApplying(ref ArousalToApply effectStruct)
        {
            foreach (IArousalModifier modifier in ArousalApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public void SubscribeArousalApplyChance(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseArousalApplyChanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseArousalApplyChanceModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseArousalApplyChanceModifiers.Add(modifier);
        }

        public void UnsubscribeArousalApplyChance(IBaseAttributeModifier modifier) => _baseArousalApplyChanceModifiers.Remove(modifier);

        int IStatusApplierModule.GetArousalApplyChanceInternal()
        {
            int arousalApplyChance = BaseArousalApplyChance;
            foreach (IBaseAttributeModifier modifier in _baseArousalApplyChanceModifiers)
                modifier.Modify(ref arousalApplyChance, _owner);

            return arousalApplyChance;
        }
    #endregion

    #region Buffs / Debuffs
        public int BaseDebuffApplyChance { get; set; }
        public SelfSortingList<IBuffOrDebuffModifier> BuffOrDebuffApplyModifiers { get; } = new(ModifierComparer.Instance);
        private readonly SelfSortingList<IBaseAttributeModifier> _baseDebuffApplyChanceModifiers = new(ModifierComparer.Instance);

        public void ModifyEffectApplying(ref BuffOrDebuffToApply effectStruct)
        {
            foreach (IBuffOrDebuffModifier modifier in BuffOrDebuffApplyModifiers)
                modifier.Modify(ref effectStruct);
        }

        public void SubscribeDebuffApplyChance(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseDebuffApplyChanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseDebuffApplyChanceModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseDebuffApplyChanceModifiers.Add(modifier);
        }

        public void UnsubscribeDebuffApplyChance(IBaseAttributeModifier modifier) => _baseDebuffApplyChanceModifiers.Remove(modifier);

        int IStatusApplierModule.GetDebuffApplyChanceInternal()
        {
            int debuffApplyChance = BaseDebuffApplyChance;
            foreach (IBaseAttributeModifier modifier in _baseDebuffApplyChanceModifiers)
                modifier.Modify(ref debuffApplyChance, _owner);

            return debuffApplyChance;
        }
    #endregion

    #region Move
        public int BaseMoveApplyChance { get; set; }
        public SelfSortingList<IMoveModifier> MoveApplyModifiers { get; } = new(ModifierComparer.Instance);
        private readonly SelfSortingList<IBaseAttributeModifier> _baseMoveApplyChanceModifiers = new(ModifierComparer.Instance);

        public void ModifyEffectApplying(ref MoveToApply effectStruct)
        {
            foreach (IMoveModifier modifier in MoveApplyModifiers)
                modifier.Modify(ref effectStruct);
        }

        public void SubscribeMoveApplyChance(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseMoveApplyChanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseMoveApplyChanceModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseMoveApplyChanceModifiers.Add(modifier);
        }

        public void UnsubscribeMoveApplyChance(IBaseAttributeModifier modifier) => _baseMoveApplyChanceModifiers.Remove(modifier);

        int IStatusApplierModule.GetMoveApplyChanceInternal()
        {
            int moveApplyChance = BaseMoveApplyChance;
            foreach (IBaseAttributeModifier modifier in _baseMoveApplyChanceModifiers)
                modifier.Modify(ref moveApplyChance, _owner);

            return moveApplyChance;
        }
    #endregion
        
    #region Poison
        public int BasePoisonApplyChance { get; set; }
        public SelfSortingList<IPoisonModifier> PoisonApplyModifiers { get; } = new(ModifierComparer.Instance);
        private readonly SelfSortingList<IBaseAttributeModifier> _basePoisonApplyChanceModifiers = new(ModifierComparer.Instance);

        public void ModifyEffectApplying(ref PoisonToApply effectStruct)
        {
            foreach (IPoisonModifier modifier in PoisonApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public void SubscribePoisonApplyChance(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _basePoisonApplyChanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _basePoisonApplyChanceModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _basePoisonApplyChanceModifiers.Add(modifier);
        }

        public void UnsubscribePoisonApplyChance(IBaseAttributeModifier modifier) => _basePoisonApplyChanceModifiers.Remove(modifier);

        int IStatusApplierModule.GetPoisonApplyChanceInternal()
        {
            int poisonApplyChance = BasePoisonApplyChance;
            foreach (IBaseAttributeModifier modifier in _basePoisonApplyChanceModifiers)
                modifier.Modify(ref poisonApplyChance, _owner);

            return poisonApplyChance;
        }
    #endregion
        
        public SelfSortingList<IGuardedModifier> GuardedApplyModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectApplying(ref GuardedToApply effectStruct)
        {
            foreach (IGuardedModifier modifier in GuardedApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public SelfSortingList<IHealModifier> HealApplyModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectApplying(ref HealToApply effectStruct)
        {
            foreach (IHealModifier modifier in HealApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public SelfSortingList<ILustModifier> LustApplyModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectApplying(ref LustToApply effectStruct)
        {
            foreach (ILustModifier modifier in LustApplyModifiers)
                modifier.Modify(ref effectStruct);
        }

        public SelfSortingList<ILustGrappledModifier> LustGrappledApplyModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectApplying(ref LustGrappledToApply effectStruct)
        {
            foreach (ILustGrappledModifier modifier in LustGrappledApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public SelfSortingList<IMarkedModifier> MarkedApplyModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectApplying(ref MarkedToApply effectStruct)
        {
            foreach (IMarkedModifier modifier in MarkedApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public SelfSortingList<IOvertimeHealModifier> OvertimeHealApplyModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectApplying(ref OvertimeHealToApply effectStruct)
        {
            foreach (IOvertimeHealModifier modifier in OvertimeHealApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public SelfSortingList<IRiposteModifier> RiposteApplyModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectApplying(ref RiposteToApply effectStruct)
        {
            foreach (IRiposteModifier modifier in RiposteApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public SelfSortingList<IStunModifier> StunApplyModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectApplying(ref StunToApply effectStruct)
        {
            foreach (IStunModifier modifier in StunApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public SelfSortingList<ISummonModifier> SummonApplyModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectApplying(ref SummonToApply effectStruct)
        {
            foreach (ISummonModifier modifier in SummonApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }
        
        public SelfSortingList<ITemptModifier> TemptationApplyModifiers { get; } = new(ModifierComparer.Instance);
        public void ModifyEffectApplying(ref TemptToApply effectStruct)
        {
            foreach (ITemptModifier modifier in TemptationApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }
    }
}