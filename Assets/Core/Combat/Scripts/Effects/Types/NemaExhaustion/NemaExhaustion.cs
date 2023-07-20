using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.Perk;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Combat.Scripts.Effects.Types.NemaExhaustion
{
    public class NemaExhaustion : StatusInstance
    {
        public readonly IBaseAttributeModifier SpeedDebuff = new SpeedModifier();
        public readonly IBaseAttributeModifier StunMitigationDebuff = new StunSpeedModifier();
        public readonly IBaseAttributeModifier DodgeDebuff = new DodgeModifier();
        public readonly IBaseAttributeModifier AccuracyDebuff = new AccuracyModifier();
        public readonly IBaseAttributeModifier ResistanceDebuff = new ResistancesModifier();
        public readonly IBaseAttributeModifier ComposureDebuff = new ComposureModifier();

        private NemaExhaustion(TSpan duration, bool isPermanent, CharacterStateMachine owner) : base(duration, isPermanent, owner)
        {
        }

        public static Option<StatusInstance> CreateInstance(TSpan duration, bool isPermanent, CharacterStateMachine owner)
        {
            if (duration.Ticks <= 0 && !isPermanent)
            {
                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                Debug.LogWarning($"Invalid parameters for {nameof(NemaExhaustion)}. Duration: {duration}, isPermanent: {isPermanent}");
                return Option<StatusInstance>.None;
            }

            NemaExhaustion instance = new(duration, isPermanent, owner);
            owner.StatusReceiverModule.AddStatus(instance, owner);
            
            IStatsModule statsModule = owner.StatsModule;
            statsModule.SubscribeSpeed(instance.SpeedDebuff, allowDuplicates: false);
            statsModule.SubscribeDodge(instance.DodgeDebuff, allowDuplicates: false);
            statsModule.SubscribeAccuracy(instance.AccuracyDebuff, allowDuplicates: false);

            IResistancesModule resistancesModule = owner.ResistancesModule;
            resistancesModule.SubscribeDebuffResistance(instance.ResistanceDebuff, allowDuplicates: false);
            resistancesModule.SubscribeMoveResistance(instance.ResistanceDebuff, allowDuplicates: false);
            resistancesModule.SubscribePoisonResistance(instance.ResistanceDebuff, allowDuplicates: false);
            
            owner.StunModule.SubscribeStunMitigation(instance.StunMitigationDebuff, allowDuplicates: false);
            
            if (owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.SubscribeComposure(instance.ComposureDebuff, allowDuplicates: false);

            return Option<StatusInstance>.Some(instance);
        }

        public NemaExhaustion([NotNull] NemaExhaustionStatusRecord record, CharacterStateMachine owner) : base(record, owner)
        {
        }

        public override void RequestDeactivation()
        {
            base.RequestDeactivation();

            IStatsModule statsModule = Owner.StatsModule;
            statsModule.UnsubscribeSpeed(SpeedDebuff);
            statsModule.UnsubscribeDodge(DodgeDebuff);
            statsModule.UnsubscribeAccuracy(AccuracyDebuff);

            IResistancesModule resistancesModule = Owner.ResistancesModule;
            resistancesModule.UnsubscribeDebuffResistance(ResistanceDebuff);
            resistancesModule.UnsubscribeMoveResistance(ResistanceDebuff);
            resistancesModule.UnsubscribePoisonResistance(ResistanceDebuff);
            
            Owner.StunModule.UnsubscribeStunMitigation(StunMitigationDebuff);
            
            if (Owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.UnsubscribeComposure(ComposureDebuff);
        }

        public override void CharacterDefeated(CharacterStateMachine character, bool becomesCorpseOnDefeat)
        {
            if (character == Owner)
            {
                RequestDeactivation();
                if (Save.AssertInstance(out Save save))
                    save.CheckNemaCombatStatus();
            }
        }

        [NotNull]
        public override StatusRecord GetRecord() => new NemaExhaustionStatusRecord();

        private class SpeedModifier : IBaseAttributeModifier
        {
            public int Priority => -1;
            [NotNull]
            public string SharedId => nameof(NemaExhaustion);

            public void Modify(ref int value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<int> modifier = GetSpeedModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }

        public static Option<int> GetSpeedModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<int>.None,
            ExhaustionEnum.Low    => Option<int>.Some(-5),
            ExhaustionEnum.Medium => Option<int>.Some(-10),
            ExhaustionEnum.High   => Option<int>.Some(-20),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, message: null)
        };

        private class StunSpeedModifier : IBaseAttributeModifier
        {
            public int Priority => -1;
            [NotNull]
            public string SharedId => nameof(NemaExhaustion);
            public void Modify(ref int value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<int> modifier = GetStunMitigationModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }

        public static Option<int> GetStunMitigationModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<int>.None,
            ExhaustionEnum.Low    => Option<int>.Some(-10),
            ExhaustionEnum.Medium => Option<int>.Some(-20),
            ExhaustionEnum.High   => Option<int>.Some(-35),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, message: null)
        };

        private class DodgeModifier : IBaseAttributeModifier
        {
            public int Priority => -1;
            [NotNull]
            public string SharedId => nameof(NemaExhaustion);
            
            public void Modify(ref int value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<int> modifier = GetDodgeModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }

        public static Option<int> GetDodgeModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<int>.None,
            ExhaustionEnum.Low    => Option<int>.None,
            ExhaustionEnum.Medium => Option<int>.Some(-10),
            ExhaustionEnum.High   => Option<int>.Some(-20),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, message: null)
        };

        private class AccuracyModifier : IBaseAttributeModifier
        {
            public int Priority => -1;
            [NotNull]
            public string SharedId => nameof(NemaExhaustion);
            
            public void Modify(ref int value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<int> modifier = GetAccuracyModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }

        public static Option<int> GetAccuracyModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<int>.None,
            ExhaustionEnum.Low    => Option<int>.Some(-3),
            ExhaustionEnum.Medium => Option<int>.Some(-7),
            ExhaustionEnum.High   => Option<int>.Some(-12),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, message: null)
        };

        private class ResistancesModifier : IBaseAttributeModifier
        {
            public int Priority => -1;
            [NotNull]
            public string SharedId => nameof(NemaExhaustion);
            
            public void Modify(ref int value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<int> modifier = GetResistancesModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }

        public static Option<int> GetResistancesModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<int>.None,
            ExhaustionEnum.Low    => Option<int>.Some(-5),
            ExhaustionEnum.Medium => Option<int>.Some(-10),
            ExhaustionEnum.High   => Option<int>.Some(-20),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, message: null)
        };

        private class ComposureModifier : IBaseAttributeModifier
        {
            public int Priority => -1;
            [NotNull]
            public string SharedId => nameof(NemaExhaustion);
            
            public void Modify(ref int value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<int> modifier = GetComposureModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }

        public static Option<int> GetComposureModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<int>.None,
            ExhaustionEnum.Low    => Option<int>.None,
            ExhaustionEnum.Medium => Option<int>.Some(-5),
            ExhaustionEnum.High   => Option<int>.Some(-10),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, message: null)
        };

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.NemaExhaustion;
        public const int GlobalId = PerkStatus.GlobalId + 1;
        public override bool IsPositive => false;
    }
}