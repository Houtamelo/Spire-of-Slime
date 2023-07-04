using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Marked;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using ListPool;
using Main_Database.Combat;
using Save_Management;
using Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Duelist
{
    public class AlluringChallenger : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            AlluringChallengerInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record AlluringChallengerRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(AlluringChallengerRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            AlluringChallengerInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class AlluringChallengerInstance : PerkInstance, IRiposteModifier, ISkillModifier
    {
        private const int MarkedDuration = 4;
        public string SharedId => nameof(AlluringChallengerInstance);
        private static readonly MarkedScript MarkedScript = new(false, MarkedDuration);
        public int Priority => 0;
        
        public AlluringChallengerInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public AlluringChallengerInstance(CharacterStateMachine owner, AlluringChallengerRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.SkillModule.SkillModifiers.Add(this);
            Owner.StatusModule.RiposteReceiveModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SkillModule.SkillModifiers.Remove(this);
            Owner.StatusModule.RiposteReceiveModifiers.Remove(this);
        }

        public override PerkRecord GetRecord() => new AlluringChallengerRecord(Key);

        public void Modify(ref RiposteToApply effectStruct)
        {
            effectStruct.Duration += 1;
        }
        
        public void Modify(ref SkillStruct skillStruct)
        {
            if (skillStruct.Skill.Key != EthelSkills.Challenge || skillStruct.Caster != Owner)
                return;

            ref ValueListPool<IActualStatusScript> ownerEffects = ref skillStruct.CasterEffects;
            ownerEffects.Add(MarkedScript);
        }
    }
}