using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.Grappled;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.DefaultModules
{
    public class DefaultStateEvaluator : IStateEvaluator
    {
        private readonly CharacterStateMachine _owner;
        private bool _isDefeated;
        private bool _isCorpse;
        private CharacterState _lastTickState;

        public DefaultStateEvaluator(CharacterStateMachine owner)
        {
            _owner = owner;
            _lastTickState = PureEvaluate();
        }
        
        public DefaultStateEvaluator(CharacterStateMachine owner, bool isDefeated, bool isCorpse)
        {
            _owner = owner;
            _isDefeated = isDefeated;
            _isCorpse = isCorpse;
            _lastTickState = PureEvaluate();
        }
        
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
            if (_owner.StatusModule.GetAll.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.IsActive)
                return CharacterState.Grappled;
            
            if (_isCorpse)
                return CharacterState.Corpse;

            if (_isDefeated)
                return CharacterState.Defeated;

            if (_owner.DownedModule.TrySome(out IDownedModule downedModule) && downedModule.GetRemaining() > 0)
                return CharacterState.Downed;

            if (_owner.StunModule.GetRemaining() > 0)
                return CharacterState.Stunned;

            if (_owner.StatusModule.GetAllRelated.FindType<LustGrappled>().TrySome(out lustGrappled) && lustGrappled.IsActive && lustGrappled.Restrainer == _owner)
                return CharacterState.Grappling;

            if (_owner.ChargeModule.GetRemaining() > 0)
                return CharacterState.Charging;
            
            if (_owner.RecoveryModule.GetRemaining() > 0)
                return CharacterState.Recovering;
            
            if (_owner.SkillModule.PlannedSkill.IsSome && _owner.SkillModule.PlannedSkill.Value.IsDoneOrCancelled == false)
                return CharacterState.Charging;
            
            return CharacterState.Idle;
        }

        public FullCharacterState FullPureEvaluate()
        {
            bool corpse = _isCorpse;
            bool defeated = _isDefeated;
            bool grappled = _owner.StatusModule.GetAll.FindType<LustGrappled>().TrySome(out LustGrappled lustGrappled) && lustGrappled.IsActive;
            bool downed = _owner.DownedModule.TrySome(out IDownedModule downedModule) && downedModule.GetRemaining() > 0;
            bool stunned = _owner.StunModule.GetRemaining() > 0;
            bool grappling = _owner.StatusModule.GetAllRelated.FindType<LustGrappled>().TrySome(out lustGrappled) && lustGrappled.IsActive && lustGrappled.Restrainer == _owner;
            bool charging = _owner.ChargeModule.GetRemaining() > 0 || (_owner.SkillModule.PlannedSkill.TrySome(out PlannedSkill plan) && plan.IsDoneOrCancelled == false);
            bool recovering = _owner.RecoveryModule.GetRemaining() > 0;
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
            if (_owner.Display.TrySome(out CharacterDisplay display) == false)
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