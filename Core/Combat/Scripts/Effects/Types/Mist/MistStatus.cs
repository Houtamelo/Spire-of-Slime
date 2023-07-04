using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.Grappled;
using UnityEngine;
using Utils.Patterns;
using Save = Save_Management.Save;

namespace Core.Combat.Scripts.Effects.Types.Mist
{
    public record MistStatusRecord(float AccumulatedLust, float AccumulatedTime, float Duration, bool IsPermanent) : StatusRecord(Duration, IsPermanent)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters) => true;
    }

    public class MistStatus : StatusInstance
    {
        private const float LustPerStep = 1.5f;
        
        public override bool IsPositive => false;
        
        private readonly ComposureModifier _composureModifier = new();

        private MistStatus(float duration, bool isPermanent, CharacterStateMachine owner) : base(duration, isPermanent, owner)
        {
        }

        public static Option<StatusInstance> CreateInstance(float duration, bool isPermanent, CharacterStateMachine owner)
        {
            if (duration <= 0 && !isPermanent)
            {
                Debug.LogWarning($"Invalid parameters for {nameof(MistStatus)}. Duration: {duration.ToString()}, isPermanent: {isPermanent}");
                return Option<StatusInstance>.None;
            }
            
            MistStatus instance = new(duration, isPermanent, owner);
            owner.StatusModule.AddStatus(instance, owner);
            owner.LustModule.Value.SubscribeComposure(instance._composureModifier, allowDuplicates: false);
            return Option<StatusInstance>.Some(instance);
        }

        private MistStatus(MistStatusRecord record, CharacterStateMachine owner) : base(record, owner)
        {
            _accumulatedLust = record.AccumulatedLust;
            _accumulatedTime = record.AccumulatedTime;
        }

        public static Option<StatusInstance> CreateInstance(MistStatusRecord record, CharacterStateMachine owner)
        {
            MistStatus instance = new(record, owner);
            owner.StatusModule.AddStatus(instance, owner);
            owner.LustModule.Value.SubscribeComposure(instance._composureModifier, allowDuplicates: false);
            return Option<StatusInstance>.Some(instance);
        }

        public override EffectType EffectType => EffectType.Mist;
        public override StatusRecord GetRecord() => new MistStatusRecord(_accumulatedLust, _accumulatedTime, Duration, IsPermanent);

        public override Option<string> GetDescription() => StatusInstanceDescriptions.Get(this);

        public override void RequestDeactivation()
        {
            base.RequestDeactivation();
            if (Owner.LustModule.IsNone)
                return;
            
            Owner.LustModule.Value.UnsubscribeComposure(_composureModifier);
            //Owner.LustModule.Value.UnsubscribeTemptationResistance(_temptationResistanceModifier);
        }

        private float _accumulatedLust;
        private float _accumulatedTime;

        public override void Tick(float timeStep)
        {
            base.Tick(timeStep);
            if (Save.AssertInstance(out Save save) == false || Owner.LustModule.IsNone)
                return;
            
            if (Owner.StatusModule.GetAll.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.IsActive)
            {
                _accumulatedTime = 0f;
                return;
            }
            
            (bool isInCombat, bool isStanding) = save.GetNemaCombatStatus();
            float exhaustion = save.NemaExhaustion;
            switch (Save.Current.IsNemaClearingMist && isInCombat && isStanding)
            {
                case false:
                case true when exhaustion >= Save.HighExhaustion:
                    _accumulatedLust += timeStep * LustPerStep * 2;
                    break;
                case true when exhaustion >= Save.MediumExhaustion: _accumulatedLust += timeStep * LustPerStep; break;
                case true when exhaustion >= Save.LowExhaustion:    _accumulatedLust += timeStep * LustPerStep * 0.5f; break;
            }

            _accumulatedTime += timeStep;
            if (_accumulatedTime < 2f)
                return;
            
            _accumulatedTime -= 2f;
            int roundLust = Mathf.FloorToInt(_accumulatedLust);
            _accumulatedLust -= roundLust;
            Owner.LustModule.Value.ChangeLust(Mathf.RoundToInt(roundLust * Random.Range(0.75f, 1.25f)));
        }

        private class ComposureModifier : IBaseFloatAttributeModifier
        {
            public int Priority => -1;
            public string SharedId => nameof(MistStatus);

            public void Modify(ref float value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                (bool isInCombat, bool isStanding) = save.GetNemaCombatStatus();
                value = ((float)Save.Current.NemaExhaustion, Save.Current.IsNemaClearingMist && isInCombat && isStanding) switch
                {
                    (< 99999f, false) => value -= 0.2f,
                    (>= Save.HighExhaustion, true) => value -= 0.2f,
                    (>= Save.MediumExhaustion, true) => value -= 0.1f,
                    (>= Save.LowExhaustion, true) => value -= 0.05f,
                    _ => value
                };
            }
        }


        /*private class TemptationResistanceModifier : IBaseFloatAttributeModifier
        {
            public int Priority => -1;
            public string SharedId => nameof(MistStatus);
            public void Modify(ref float value, CharacterStateMachine self)
            {
                if (Save.AssertInstance(out Save save) == false)
                    return;
                
                (bool isInCombat, bool isStanding) = save.GetNemaCombatStatus();
                value = ((float)Save.Current.NemaExhaustion, Save.Current.IsNemaClearingMist && isInCombat && isStanding) switch
                {
                    (< 99999f, false) => value -= 0.3f,
                    (>= Save.HighExhaustion, true) => value -= 0.3f,
                    (>= Save.MediumExhaustion, true) => value -= 0.15f,
                    (>= Save.LowExhaustion, true) => value -= 0.05f,
                    _ => value
                };
            }
        }*/
        public const int GlobalId = NemaExhaustion.NemaExhaustion.GlobalId + 1;
    }
}