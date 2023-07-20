using System.Text;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Patterns;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Behaviour.Modules
{
    public record DefaultStateEvaluatorRecord(bool IsDefeated, bool IsCorpse) : StateEvaluatorRecord
    {
        [NotNull]
        public override IStateEvaluator Deserialize(CharacterStateMachine owner) => DefaultStateEvaluator.FromRecord(owner, record: this);
        public override bool IsDataValid(in StringBuilder errors, CharacterRecord[] allCharacters) => true;
    }

    public class DefaultStateEvaluator : IStateEvaluator
    {
        private readonly CharacterStateMachine _owner;
        
        private bool _isDefeated;
        private bool _isCorpse;
        private CharacterState _lastTickState;

        private DefaultStateEvaluator(CharacterStateMachine owner)
        {
            _owner = owner;
            _lastTickState = PureEvaluate();
        }

        private DefaultStateEvaluator(CharacterStateMachine owner, [NotNull] DefaultStateEvaluatorRecord record)
        {
            _owner = owner;
            _isDefeated = record.IsDefeated;
            _isCorpse = record.IsCorpse;
            _lastTickState = PureEvaluate();
        }
        
        [NotNull]
        public static DefaultStateEvaluator FromInitialSetup(CharacterStateMachine owner) => new(owner);

        [NotNull]
        public static DefaultStateEvaluator FromRecord(CharacterStateMachine owner, [NotNull] DefaultStateEvaluatorRecord record) => new(owner, record);

        [NotNull]
        public StateEvaluatorRecord GetRecord() => new DefaultStateEvaluatorRecord(_isDefeated, _isCorpse);
        
        public (CharacterState previous, CharacterState current) OncePerTickStateEvaluation()
        {
            CharacterState previous = _lastTickState;
            _lastTickState = PureEvaluate();
            return (previous, _lastTickState);
        }

        public bool CanBeTargeted()
        {
            FullCharacterState fullPureEvaluate = FullPureEvaluate();
            if (fullPureEvaluate.Defeated || fullPureEvaluate.Corpse || fullPureEvaluate.Grappled)
                return false;

            return true;
        }

        public CharacterState PureEvaluate()
        {
            if (_owner.StatusReceiverModule.GetAll.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.IsActive)
                return CharacterState.Grappled;
            
            if (_isCorpse)
                return CharacterState.Corpse;

            if (_isDefeated)
                return CharacterState.Defeated;

            if (_owner.DownedModule.TrySome(out IDownedModule downedModule) && downedModule.GetRemaining().Ticks > 0)
                return CharacterState.Downed;

            if (_owner.StunModule.GetRemaining().Ticks > 0)
                return CharacterState.Stunned;

            if (_owner.StatusReceiverModule.GetAllRelated.FindType<LustGrappled>().TrySome(out lustGrappled) && lustGrappled.IsActive && lustGrappled.Restrainer == _owner)
                return CharacterState.Grappling;

            if (_owner.ChargeModule.GetRemaining().Ticks > 0)
                return CharacterState.Charging;
            
            if (_owner.RecoveryModule.GetRemaining().Ticks > 0)
                return CharacterState.Recovering;
            
            if (_owner.SkillModule.PlannedSkill.IsSome && _owner.SkillModule.PlannedSkill.Value.IsDoneOrCancelled == false)
                return CharacterState.Charging;
            
            return CharacterState.Idle;
        }

        public FullCharacterState FullPureEvaluate()
        {
            bool corpse = _isCorpse;
            bool defeated = _isDefeated;
            bool grappled = _owner.StatusReceiverModule.GetAll.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.IsActive;
            bool downed = _owner.DownedModule.TrySome(out IDownedModule downedModule) && downedModule.GetRemaining().Ticks > 0;
            bool stunned = _owner.StunModule.GetRemaining().Ticks > 0;
            bool grappling = _owner.StatusReceiverModule.GetAllRelated.FindType<LustGrappled>().TrySome(out lustGrappled) && lustGrappled.IsActive && lustGrappled.Restrainer == _owner;
            bool charging = _owner.ChargeModule.GetRemaining().Ticks > 0 || (_owner.SkillModule.PlannedSkill.TrySome(out PlannedSkill plan) && plan.IsDoneOrCancelled == false);
            bool recovering = _owner.RecoveryModule.GetRemaining().Ticks > 0;
            bool idle = !corpse && !defeated && !grappled && !downed && !stunned && !grappling && !charging && !recovering;
            CharacterState main = corpse ? CharacterState.Corpse : defeated ? CharacterState.Defeated : grappled ? CharacterState.Grappled : downed ? CharacterState.Downed : stunned ? CharacterState.Stunned : grappling ?
                                      CharacterState.Grappling : charging ? CharacterState.Charging : recovering ? CharacterState.Recovering : CharacterState.Idle;
            return new FullCharacterState
            {
                Corpse = corpse,
                Defeated = defeated,
                Grappled = grappled,
                Downed = downed,
                Stunned = stunned,
                Grappling = grappling,
                Charging = charging,
                Recovering = recovering,
                Idle = idle,
                Main = main
            };
        }

        public void OutOfForces()
        {
            if (_isDefeated)
                return;
            
            _isDefeated = true;
            if (_owner.Display.TrySome(out DisplayModule display) == false)
                return;
            
            _isCorpse = _owner.Script.BecomesCorpseOnDefeat(out CombatAnimation corpseAnimation);
            Option<CharacterStateMachine> lastDamager = _owner.StaminaModule.TrySome(out IStaminaModule staminaModule) ? staminaModule.LastDamager : Option.None;
            display.CombatManager.Characters.NotifyDefeated(_owner, lastDamager, _isCorpse);
            
            FullCharacterState state = FullPureEvaluate();
            if (state.Grappled)
                return;

            if (state.Corpse)
            {
                display.AnimateCorpse(corpseAnimation);
                return;
            }

            display.AnimateDefeated(onFinish: characterDisplay =>
            {
                if (characterDisplay != null)
                    Object.Destroy(display.gameObject);
            });
        }
    }
}