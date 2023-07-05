using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.Perk;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Combat.Scripts.Effects.Types.NemaExhaustion
{
    public record NemaExhaustionStatusRecord() : StatusRecord(Duration: 99999f, IsPermanent: true)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters) => true;
    }

    public class NemaExhaustion : StatusInstance
    {
        public override bool IsPositive => false;

        private readonly IBaseFloatAttributeModifier _speedModifier = new SpeedModifier();
        private readonly IBaseFloatAttributeModifier _stunSpeedModifier = new StunSpeedModifier();
        private readonly IBaseFloatAttributeModifier _dodgeModifier = new DodgeModifier();
        private readonly IBaseFloatAttributeModifier _accuracyModifier = new AccuracyModifier();
        private readonly IBaseFloatAttributeModifier _resistanceModifier = new ResistancesModifier();
        private readonly IBaseFloatAttributeModifier _composureModifier = new ComposureModifier();

        private NemaExhaustion(float duration, bool isPermanent, CharacterStateMachine owner) : base(duration, isPermanent, owner)
        {
        }

        public static Option<StatusInstance> CreateInstance(float duration, bool isPermanent, CharacterStateMachine owner)
        {
            if (duration <= 0 && !isPermanent)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(NemaExhaustion)}. Duration: {duration}, isPermanent: {isPermanent}");
                return Option<StatusInstance>.None;
            }

            NemaExhaustion instance = new(duration, isPermanent, owner);
            owner.StatusModule.AddStatus(instance, owner);
            
            IStatsModule statsModule = owner.StatsModule;
            statsModule.SubscribeSpeed(instance._speedModifier, allowDuplicates: false);
            statsModule.SubscribeDodge(instance._dodgeModifier, allowDuplicates: false);
            statsModule.SubscribeAccuracy(instance._accuracyModifier, allowDuplicates: false);

            IResistancesModule resistancesModule = owner.ResistancesModule;
            resistancesModule.SubscribeDebuffResistance(instance._resistanceModifier, allowDuplicates: false);
            resistancesModule.SubscribeMoveResistance(instance._resistanceModifier, allowDuplicates: false);
            resistancesModule.SubscribePoisonResistance(instance._resistanceModifier, allowDuplicates: false);
            resistancesModule.SubscribeStunRecoverySpeed(instance._stunSpeedModifier, allowDuplicates: false);
            
            if (owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.SubscribeComposure(instance._composureModifier, allowDuplicates: false);

            return Option<StatusInstance>.Some(instance);
        }

        private NemaExhaustion(NemaExhaustionStatusRecord record, CharacterStateMachine owner) : base(record, owner)
        {
        }

        public static Option<StatusInstance> CreateInstance(NemaExhaustionStatusRecord record, CharacterStateMachine owner)
        {
            NemaExhaustion instance = new(record, owner);
            owner.StatusModule.AddStatus(instance, owner);

            IStatsModule statsModule = owner.StatsModule;
            statsModule.SubscribeSpeed(instance._speedModifier, allowDuplicates: false);
            statsModule.SubscribeDodge(instance._dodgeModifier, allowDuplicates: false);
            statsModule.SubscribeAccuracy(instance._accuracyModifier, allowDuplicates: false);

            IResistancesModule resistancesModule = owner.ResistancesModule;
            resistancesModule.SubscribeDebuffResistance(instance._resistanceModifier, allowDuplicates: false);
            resistancesModule.SubscribeMoveResistance(instance._resistanceModifier, allowDuplicates: false);
            resistancesModule.SubscribePoisonResistance(instance._resistanceModifier, allowDuplicates: false);
            resistancesModule.SubscribeStunRecoverySpeed(instance._stunSpeedModifier, allowDuplicates: false);

            return Option<StatusInstance>.Some(instance);
        }

        public override void RequestDeactivation()
        {
            base.RequestDeactivation();

            IStatsModule statsModule = Owner.StatsModule;
            statsModule.UnsubscribeSpeed(_speedModifier);
            statsModule.UnsubscribeDodge(_dodgeModifier);
            statsModule.UnsubscribeAccuracy(_accuracyModifier);

            IResistancesModule resistancesModule = Owner.ResistancesModule;
            resistancesModule.UnsubscribeDebuffResistance(_resistanceModifier);
            resistancesModule.UnsubscribeMoveResistance(_resistanceModifier);
            resistancesModule.UnsubscribePoisonResistance(_resistanceModifier);
            resistancesModule.UnsubscribeStunRecoverySpeed(_resistanceModifier);
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
        
        public override StatusRecord GetRecord() => new NemaExhaustionStatusRecord();

        private class SpeedModifier : IBaseFloatAttributeModifier
        {
            public int Priority => -1;
            public string SharedId => nameof(NemaExhaustion);

            public void Modify(ref float value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<float> modifier = GetSpeedModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }

        public static Option<float> GetSpeedModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<float>.None,
            ExhaustionEnum.Low    => Option<float>.Some(-0.05f),
            ExhaustionEnum.Medium => Option<float>.Some(-0.1f),
            ExhaustionEnum.High   => Option<float>.Some(-0.2f),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, null)
        };

        private class StunSpeedModifier : IBaseFloatAttributeModifier
        {
            public int Priority => -1;
            public string SharedId => nameof(NemaExhaustion);
            public void Modify(ref float value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<float> modifier = GetStunRecoverySpeedModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }

        public static Option<float> GetStunRecoverySpeedModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<float>.None,
            ExhaustionEnum.Low    => Option<float>.Some(-0.1f),
            ExhaustionEnum.Medium => Option<float>.Some(-0.2f),
            ExhaustionEnum.High   => Option<float>.Some(-0.35f),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, null)
        };

        private class DodgeModifier : IBaseFloatAttributeModifier
        {
            public int Priority => -1;
            public string SharedId => nameof(NemaExhaustion);
            public void Modify(ref float value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<float> modifier = GetDodgeModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }

        public static Option<float> GetDodgeModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<float>.None,
            ExhaustionEnum.Low    => Option<float>.None,
            ExhaustionEnum.Medium => Option<float>.Some(-0.1f),
            ExhaustionEnum.High   => Option<float>.Some(-0.2f),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, null)
        };

        private class AccuracyModifier : IBaseFloatAttributeModifier
        {
            public int Priority => -1;
            public string SharedId => nameof(NemaExhaustion);
            public void Modify(ref float value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<float> modifier = GetAccuracyModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }

        public static Option<float> GetAccuracyModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<float>.None,
            ExhaustionEnum.Low    => Option<float>.Some(-0.03f),
            ExhaustionEnum.Medium => Option<float>.Some(-0.07f),
            ExhaustionEnum.High   => Option<float>.Some(-0.12f),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, null)
        };

        private class ResistancesModifier : IBaseFloatAttributeModifier
        {
            public int Priority => -1;
            public string SharedId => nameof(NemaExhaustion);
            public void Modify(ref float value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<float> modifier = GetResistancesModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }

        public static Option<float> GetResistancesModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<float>.None,
            ExhaustionEnum.Low    => Option<float>.Some(-0.05f),
            ExhaustionEnum.Medium => Option<float>.Some(-0.1f),
            ExhaustionEnum.High   => Option<float>.Some(-0.2f),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, null)
        };
        
        private class ComposureModifier : IBaseFloatAttributeModifier
        {
            public int Priority => -1;
            public string SharedId => nameof(NemaExhaustion);
            public void Modify(ref float value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                Option<float> modifier = GetComposureModifier(save.NemaExhaustionAsEnum);
                if (modifier.IsSome)
                    value += modifier.Value;
            }
        }
        
        public static Option<float> GetComposureModifier(ExhaustionEnum exhaustion) => exhaustion switch
        {
            ExhaustionEnum.None   => Option<float>.None,
            ExhaustionEnum.Low    => Option<float>.None,
            ExhaustionEnum.Medium => Option<float>.Some(-0.05f),
            ExhaustionEnum.High   => Option<float>.Some(-0.1f),
            _                     => throw new System.ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, null)
        };


        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);
        public override EffectType EffectType => EffectType.NemaExhaustion;
        public const int GlobalId = PerkStatus.GlobalId + 1;
    }
}