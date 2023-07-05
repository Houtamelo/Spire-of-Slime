using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Utils.Patterns;

namespace Core.Main_Characters.Nema.Combat.Perks.BattleMage
{
    public class Triumph : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            TriumphInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record TriumphRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(TriumphRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            TriumphInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class TriumphInstance : PerkInstance
    {
        private const int LustModifier = -10;
        private const float SpeedBuffDuration = 3f;
        private const float SpeedModifier = 0.25f;

        private static readonly BuffOrDebuffScript Buff = new(false, SpeedBuffDuration, 1f, CombatStat.Speed, SpeedModifier);
        
        private readonly CharacterManager.DefeatedDelegate _onCharacterDefeated;
        
        public TriumphInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
            _onCharacterDefeated = OnCharacterDefeated;
        }
        
        public TriumphInstance(CharacterStateMachine owner, TriumphRecord record) : base(owner, record)
        {
            _onCharacterDefeated = OnCharacterDefeated;
        }

        protected override void OnSubscribe()
        {
            if (Owner.Display.IsSome)
                Owner.Display.Value.CombatManager.Characters.DefeatedEvent += _onCharacterDefeated;
        }

        protected override void OnUnsubscribe()
        {
            if (Owner.Display.IsSome)
                Owner.Display.Value.CombatManager.Characters.DefeatedEvent -= _onCharacterDefeated;
        }

        public override PerkRecord GetRecord() => new TriumphRecord(Key);

        private void OnCharacterDefeated(CharacterStateMachine defeated, Option<CharacterStateMachine> lastDamager)
        {
            if (Owner.StateEvaluator.PureEvaluate() is CharacterState.Defeated or CharacterState.Corpse or CharacterState.Downed or CharacterState.Grappled || lastDamager.IsNone || lastDamager.Value != Owner)
                return;
            
            if (Owner.LustModule.IsSome)
                Owner.LustModule.Value.ChangeLust(LustModifier);
            
            BuffOrDebuffToApply effectStruct = (BuffOrDebuffToApply)Buff.GetStatusToApply(Owner, Owner, false, null);
            BuffOrDebuffScript.ProcessModifiersAndTryApply(effectStruct);
        }
    }
}