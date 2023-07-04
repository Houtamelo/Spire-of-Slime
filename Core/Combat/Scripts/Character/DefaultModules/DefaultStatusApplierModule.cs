using Core.Combat.Scripts.Behaviour;
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
using Core.Combat.Scripts.Interfaces.Modules;
using Utils.Collections;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultStatusApplierModule : IStatusApplierModule
    {
        private readonly CharacterStateMachine _owner;

        private DefaultStatusApplierModule(CharacterStateMachine owner) => _owner = owner;

        public static DefaultStatusApplierModule FromInitialSetup(CharacterStateMachine owner) =>
            new(owner)
            {
                BaseDebuffApplyChance = owner.Script.DebuffApplyChance,
                BaseMoveApplyChance = owner.Script.MoveApplyChance,
                BasePoisonApplyChance = owner.Script.PoisonApplyChance,
                BaseArousalApplyChance = owner.Script.ArousalApplyChance
            };

        public static DefaultStatusApplierModule FromRecord(CharacterStateMachine owner, CharacterRecord record) =>
            new(owner)
            {
                BaseDebuffApplyChance = record.BaseDebuffApplyChance,
                BaseMoveApplyChance = record.BaseMoveApplyChance,
                BasePoisonApplyChance = record.BasePoisonApplyChance,
                BaseArousalApplyChance = record.BaseArousalApplyChance
            };

    #region Arousal
        public float BaseArousalApplyChance { get; set; }
        public SelfSortingList<IArousalModifier> ArousalApplyModifiers { get; } = new(ModifierComparer.Instance);
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseArousalApplyChanceModifiers = new(ModifierComparer.Instance);

        public void ModifyEffectApplying(ref ArousalToApply effectStruct)
        {
            foreach (IArousalModifier modifier in ArousalApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public void SubscribeArousalApplyChance(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseArousalApplyChanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseFloatAttributeModifier element in _baseArousalApplyChanceModifiers)
                if (element.SharedId == modifier.SharedId)
                    return;

            _baseArousalApplyChanceModifiers.Add(modifier);
        }

        public void UnsubscribeArousalApplyChance(IBaseFloatAttributeModifier modifier) => _baseArousalApplyChanceModifiers.Remove(modifier);

        float IStatusApplierModule.GetArousalApplyChanceInternal()
        {
            float arousalApplyChance = BaseArousalApplyChance;
            foreach (IBaseFloatAttributeModifier modifier in _baseArousalApplyChanceModifiers)
                modifier.Modify(ref arousalApplyChance, _owner);

            return arousalApplyChance;
        }
    #endregion

    #region Buffs / Debuffs
        public float BaseDebuffApplyChance { get; set; }
        public SelfSortingList<IBuffOrDebuffModifier> BuffOrDebuffApplyModifiers { get; } = new(ModifierComparer.Instance);
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseDebuffApplyChanceModifiers = new(ModifierComparer.Instance);

        public void ModifyEffectApplying(ref BuffOrDebuffToApply effectStruct)
        {
            foreach (IBuffOrDebuffModifier modifier in BuffOrDebuffApplyModifiers)
                modifier.Modify(ref effectStruct);
        }

        public void SubscribeDebuffApplyChance(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseDebuffApplyChanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseFloatAttributeModifier element in _baseDebuffApplyChanceModifiers)
                if (element.SharedId == modifier.SharedId)
                    return;

            _baseDebuffApplyChanceModifiers.Add(modifier);
        }

        public void UnsubscribeDebuffApplyChance(IBaseFloatAttributeModifier modifier) => _baseDebuffApplyChanceModifiers.Remove(modifier);

        float IStatusApplierModule.GetDebuffApplyChanceInternal()
        {
            float debuffApplyChance = BaseDebuffApplyChance;
            foreach (IBaseFloatAttributeModifier modifier in _baseDebuffApplyChanceModifiers)
                modifier.Modify(ref debuffApplyChance, _owner);

            return debuffApplyChance;
        }
    #endregion

    #region Move
        public float BaseMoveApplyChance { get; set; }
        public SelfSortingList<IMoveModifier> MoveApplyModifiers { get; } = new(ModifierComparer.Instance);
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseMoveApplyChanceModifiers = new(ModifierComparer.Instance);

        public void ModifyEffectApplying(ref MoveToApply effectStruct)
        {
            foreach (IMoveModifier modifier in MoveApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public void SubscribeMoveApplyChance(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseMoveApplyChanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseFloatAttributeModifier element in _baseMoveApplyChanceModifiers)
                if (element.SharedId == modifier.SharedId)
                    return;

            _baseMoveApplyChanceModifiers.Add(modifier);
        }

        public void UnsubscribeMoveApplyChance(IBaseFloatAttributeModifier modifier) => _baseMoveApplyChanceModifiers.Remove(modifier);

        float IStatusApplierModule.GetMoveApplyChanceInternal()
        {
            float moveApplyChance = BaseMoveApplyChance;
            foreach (IBaseFloatAttributeModifier modifier in _baseMoveApplyChanceModifiers)
                modifier.Modify(ref moveApplyChance, _owner);

            return moveApplyChance;
        }
    #endregion
        
    #region Poison
        public float BasePoisonApplyChance { get; set; }
        public SelfSortingList<IPoisonModifier> PoisonApplyModifiers { get; } = new(ModifierComparer.Instance);
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _basePoisonApplyChanceModifiers = new(ModifierComparer.Instance);

        public void ModifyEffectApplying(ref PoisonToApply effectStruct)
        {
            foreach (IPoisonModifier modifier in PoisonApplyModifiers) 
                modifier.Modify(ref effectStruct);
        }

        public void SubscribePoisonApplyChance(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _basePoisonApplyChanceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseFloatAttributeModifier element in _basePoisonApplyChanceModifiers)
                if (element.SharedId == modifier.SharedId)
                    return;

            _basePoisonApplyChanceModifiers.Add(modifier);
        }

        public void UnsubscribePoisonApplyChance(IBaseFloatAttributeModifier modifier) => _basePoisonApplyChanceModifiers.Remove(modifier);

        float IStatusApplierModule.GetPoisonApplyChanceInternal()
        {
            float poisonApplyChance = BasePoisonApplyChance;
            foreach (IBaseFloatAttributeModifier modifier in _basePoisonApplyChanceModifiers)
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