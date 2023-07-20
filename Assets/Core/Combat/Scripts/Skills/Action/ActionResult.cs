using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Patterns;
using ListPool;

namespace Core.Combat.Scripts.Skills.Action
{
    public struct ActionResult
    {
        public readonly bool Hit;
        public bool Missed => Hit == false;
        
        public readonly ISkill Skill;
        
        public readonly CharacterStateMachine Caster;
        public readonly CharacterStateMachine Target;
        
        public readonly Option<int> DamageDealt;
        public readonly bool Critical;
        
        private readonly Option<ListPool<StatusResult>> _statusResults;
        public Option<ListPool<StatusResult>> StatusResults => Hit ? _statusResults : Option.None;

        private ActionResult(bool success, ISkill skill, CharacterStateMachine caster, CharacterStateMachine target, Option<int> damageDealt, bool critical, Option<ListPool<StatusResult>> statusResults)
        {
            Hit = success;
            Skill = skill;
            Target = target;
            Caster = caster;
            DamageDealt = damageDealt;
            Critical = critical;
            _statusResults = statusResults;
            _allocated = true;
        }
        
        public static ActionResult FromMiss(ISkill skill, CharacterStateMachine caster, CharacterStateMachine target) 
            => new(success: false, skill, caster, target, damageDealt: Option.None, critical: false, Option<ListPool<StatusResult>>.None);
        
        public static ActionResult FromHit(ISkill skill, CharacterStateMachine caster, CharacterStateMachine target, Option<int> damageDealt, bool crit, Option<ListPool<StatusResult>> statusResults)
            => new(success: true, skill, caster, target, damageDealt, crit, statusResults);

        private bool _allocated;

        public void Dispose()
        {
            if (_allocated == false)
                return;

            _allocated = false;
            if (_statusResults.IsSome)
                _statusResults.Value?.Dispose();
        }
    }
}