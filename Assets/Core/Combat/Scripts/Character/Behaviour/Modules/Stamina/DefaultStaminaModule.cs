using System.Text;
using Core.Combat.Scripts.Cues;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Collections;
using Core.Utils.Math;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;
using static Core.Combat.Scripts.Behaviour.Modules.IStaminaModule;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultStaminaRecord(int BaseMaxStamina, int MaxStamina, int CurrentStamina, int BaseResilience) : StaminaRecord
    {
        [NotNull]
        public override IStaminaModule Deserialize(CharacterStateMachine owner) => DefaultStaminaModule.FromRecord(owner, record: this);
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    }

    public class DefaultStaminaModule : IStaminaModule
    {
        private readonly CharacterStateMachine _owner;

        private DefaultStaminaModule(CharacterStateMachine owner) => _owner = owner;

        [NotNull]
        public static DefaultStaminaModule FromInitialSetup([NotNull] CharacterStateMachine owner)
        {
            int maxStamina = owner.Script.Stamina + Random.Range(minInclusive: -1 * owner.Script.StaminaAmplitude, maxExclusive: owner.Script.StaminaAmplitude + 1);
            return new DefaultStaminaModule(owner)
            {
                BaseMax = maxStamina,
                ActualMax = maxStamina,
                _current = maxStamina,
                BaseResilience = owner.Script.Resilience,
            };
        }

        [NotNull]
        public static DefaultStaminaModule FromRecord(CharacterStateMachine owner, [NotNull] DefaultStaminaRecord record)
        {
            int max = record.MaxStamina;
            return new DefaultStaminaModule(owner)
            {
                BaseMax = record.BaseMaxStamina,
                ActualMax = max,
                _current = ClampCurrentStamina(record.CurrentStamina, max),
                BaseResilience = record.BaseResilience
            };
        }
        
        [NotNull]
        public StaminaRecord GetRecord() => new DefaultStaminaRecord(BaseMax, ActualMax, _current, BaseResilience);

        public int BaseMax { get; private init; }
        public int ActualMax { get; private set; }
        void IStaminaModule.SetMaxInternal(int clampedMaxStamina) => ActualMax = clampedMaxStamina;

        private int _current;
        int IStaminaModule.Current => _current;
        void IStaminaModule.SetCurrentInternal(int clampedCurrentStamina) => _current = clampedCurrentStamina;

    #region Resilience
        public int BaseResilience { get; set; }
        private readonly SelfSortingList<IBaseAttributeModifier> _baseResilienceModifiers = new(ModifierComparer.Instance);

        public void SubscribeResilience(IBaseAttributeModifier modifier, bool allowDuplicates)
        {
            if (allowDuplicates)
            {
                _baseResilienceModifiers.Add(modifier);
                return;
            }

            foreach (IBaseAttributeModifier element in _baseResilienceModifiers)
            {
                if (element.SharedId == modifier.SharedId)
                    return;
            }

            _baseResilienceModifiers.Add(modifier);
        }

        public void UnsubscribeResilience(IBaseAttributeModifier modifier) => _baseResilienceModifiers.Remove(modifier);

        int IStaminaModule.GetResilienceInternal()
        {
            int resilience = BaseResilience;
            foreach (IBaseAttributeModifier modifier in _baseResilienceModifiers)
                modifier.Modify(ref resilience, _owner);

            return resilience;
        }
    #endregion
        
        public Option<CharacterStateMachine> LastDamager { get; private set; }

        public void ReceiveDamage(int damage, DamageType damageType, CharacterStateMachine source)
        {
            if (damage == 0)
            {
                Debug.LogWarning("Trying to receive 0 damage");
                return;
            }

            LastDamager = source;
            if (_owner.DownedModule.IsSome && _owner.DownedModule.Value.GetRemaining().Ticks > 0 && _owner.LustModule.IsSome)
            {
                _owner.LustModule.Value.ChangeLust(Mathf.CeilToInt(damage * 1.5f));
                return;
            }

            _current = ClampCurrentStamina(_current - damage, ActualMax);

            if (_owner.Display.AssertSome(out DisplayModule display) == false)
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

        public void DoHeal(int heal, bool isOvertime)
        {
            if (heal == 0)
            {
                Debug.LogWarning("Trying to heal for 0");
                return;
            }
            
            if (_owner.LustModule.IsSome && _owner.DownedModule.TrySome(out IDownedModule downedModule) && downedModule.GetRemaining().Ticks > 0)
            {
                TSpan previousDowned = downedModule.GetRemaining();

                double percentageOfMax = heal / (double)ActualMax;
                TSpan percentageOfMaxTime = downedModule.GetInitialDuration();
                percentageOfMaxTime.Multiply(percentageOfMax);
                
                TSpan newDowned = (previousDowned - percentageOfMaxTime).Clamp(TSpan.Zero, previousDowned);
                downedModule.SetInitial(newDowned);
            }
            else
            {
                _current = ClampCurrentStamina(_current + heal, ActualMax);
            }
            
            if (_owner.Display.TrySome(out DisplayModule display) == false ||
                CombatTextCueManager.AssertInstance(out CombatTextCueManager textCueManager) == false ||
                StatusVFXManager.AssertInstance(out StatusVFXManager statusVFXManager) == false)
                return;

            CombatCueOptions options = CombatCueOptions.Default(heal.ToString("0"), ColorReferences.Heal, display);
            options.FontSize *= 1.3f;
            if (isOvertime == false)
                options.Duration = IActionSequence.AnimationDuration / 2f;

            options.OnPlay += statusVFXManager.HealSource.Play;
            textCueManager.EnqueueAboveCharacter(ref options, display);
        }
        
        public void AfterTickUpdate(in TSpan timeStep, in CharacterState previousState, in CharacterState currentState)
        {
            if (_current <= 0 && currentState is not CharacterState.Defeated and not CharacterState.Corpse and not CharacterState.Downed and not CharacterState.Grappled)
                _owner.OnZeroStamina();
            
            if (_owner.Display.TrySome(out DisplayModule display) == false)
                return;

            if (currentState is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled))
                display.SetStaminaBar(active: true, _current, ActualMax);
            else
                display.SetStaminaBar(active: false, current: 0, max: 1);
        }

        public void ForceUpdateDisplay([NotNull] in DisplayModule display)
        {
            if (_owner.StateEvaluator.PureEvaluate() is not (CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled))
                display.SetStaminaBar(active: true, _current, ActualMax);
            else
                display.SetStaminaBar(active: false, current: 0, max: 1);
        }
    }
}