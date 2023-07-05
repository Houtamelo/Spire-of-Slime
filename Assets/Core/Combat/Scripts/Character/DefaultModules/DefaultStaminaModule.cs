using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Collections;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;
using static Core.Combat.Scripts.Interfaces.Modules.IStaminaModule;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultStaminaModule : IStaminaModule
    {
        private readonly CharacterStateMachine _owner;

        private DefaultStaminaModule(CharacterStateMachine owner) => _owner = owner;

        public static DefaultStaminaModule FromInitialSetup(CharacterStateMachine owner)
        {
            uint maxStamina = (uint)((int)owner.Script.Stamina + Random.Range(minInclusive: -1 * (int)owner.Script.StaminaAmplitude, maxExclusive: (int)owner.Script.StaminaAmplitude + 1));
            return new DefaultStaminaModule(owner)
            {
                BaseMax = maxStamina,
                ActualMax = maxStamina,
                _current = maxStamina,
                BaseResilience = owner.Script.Resilience,
            };
        }

        public static DefaultStaminaModule FromRecord(CharacterStateMachine owner, CharacterRecord record)
        {
            uint max = record.MaxStamina;
            return new DefaultStaminaModule(owner)
            {
                BaseMax = record.BaseMaxStamina,
                ActualMax = max,
                _current = ClampCurrentStamina(record.CurrentStamina, max),
                BaseResilience = record.BaseResilience
            };
        }

        public uint BaseMax { get; private init; }
        public uint ActualMax { get; private set; }
        void IStaminaModule.SetMaxInternal(uint maxStamina) => ActualMax = maxStamina;

        private uint _current;
        uint IStaminaModule.Current => _current;
        void IStaminaModule.SetCurrentInternal(uint currentStamina) => _current = currentStamina;

    #region Resilience
        public float BaseResilience { get; set; }
        private readonly SelfSortingList<IBaseFloatAttributeModifier> _baseResilienceModifiers = new(ModifierComparer.Instance);

        public void SubscribeResilience(IBaseFloatAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseResilienceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseFloatAttributeModifier element in _baseResilienceModifiers)
                if (element.SharedId == modifier.SharedId)
                    return;

            _baseResilienceModifiers.Add(modifier);
        }

        public void UnsubscribeResilience(IBaseFloatAttributeModifier modifier) => _baseResilienceModifiers.Remove(modifier);

        float IStaminaModule.GetResilienceInternal()
        {
            float resilience = BaseResilience;
            foreach (IBaseFloatAttributeModifier modifier in _baseResilienceModifiers)
                modifier.Modify(ref resilience, _owner);

            return resilience;
        }
    #endregion
        
        public Option<CharacterStateMachine> LastDamager { get; private set; }

        public void ReceiveDamage(uint damage, DamageType damageType, CharacterStateMachine source)
        {
            if (damage == 0)
            {
                Debug.LogWarning("Trying to receive 0 damage");
                return;
            }

            LastDamager = source;
            if (_owner.DownedModule.IsSome && _owner.DownedModule.Value.GetRemaining() > 0 && _owner.LustModule.IsSome)
            {
                _owner.LustModule.Value.ChangeLust(Mathf.CeilToInt(damage * 1.5f));
                return;
            }

            _current = ClampCurrentStamina(_current - damage, ActualMax);

            if (_owner.Display.AssertSome(out CharacterDisplay display) == false)
                return;
            
            if (damageType == DamageType.Brute && CombatTextCueManager.AssertInstance(out CombatTextCueManager cueManager))
            {
                CombatCueOptions options = CombatCueOptions.Default(text: damage.ToString("0"), ColorReferences.Damage, display);
                options.Duration = IActionSequence.AnimationDuration / 2f;
                options.FontSize *= 1.3f;
                cueManager.EnqueueAboveCharacter(options: ref options, character: display);
            }
            else if (damageType == DamageType.Poison && StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager))
            {
                statusVFXManager.Enqueue(StatusCueHandler.FromPoisonTick(_owner, StatusCueHandler.NoValidator, damage));
            }
        }

        public void DoHeal(uint heal, bool isOvertime)
        {
            if (heal == 0)
            {
                Debug.LogWarning("Trying to heal for 0");
                return;
            }
            
            if (_owner.LustModule.IsSome && _owner.DownedModule.TrySome(out IDownedModule downedModule) && downedModule.GetRemaining() > 0)
            {
                float percentageOfMax = heal / (float)ActualMax;
                float previousDowned = downedModule.GetRemaining();
                float newDowned = Mathf.Clamp(0, previousDowned - percentageOfMax * downedModule.GetInitialDuration(), previousDowned);
                downedModule.SetInitial(newDowned);
            }
            else
            {
                _current = ClampCurrentStamina(_current + heal, ActualMax);
            }
            
            if (_owner.Display.TrySome(out CharacterDisplay display) == false ||
                CombatTextCueManager.AssertInstance(out CombatTextCueManager textCueManager) == false ||
                StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager) == false)
            {
                return;
            }
            
            CombatCueOptions options = CombatCueOptions.Default(heal.ToString("0"), ColorReferences.Heal, display);
            options.FontSize *= 1.3f;
            if (isOvertime == false)
                options.Duration = IActionSequence.AnimationDuration / 2f;

            options.OnPlay += statusVFXManager.HealSource.Play;
            textCueManager.EnqueueAboveCharacter(ref options, display);
        }
        
        public void AfterTickUpdate(in float timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            if (_current <= 0 && currentState is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Downed and not CharacterState.Grappled)
                _owner.OnZeroStamina();
            
            if (_owner.Display.TrySome(out CharacterDisplay display) == false)
                return;

            if (currentState is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled))
                display.SetStaminaBar(active: true, _current, ActualMax);
            else
                display.SetStaminaBar(active: false, current: 0, max: 1);
        }

        public void ForceUpdateDisplay(in CharacterDisplay display)
        {
            if (_owner.StateEvaluator.PureEvaluate() is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled))
                display.SetStaminaBar(active: true, _current, ActualMax);
            else
                display.SetStaminaBar(active: false, current: 0, max: 1);
        }
    }
}