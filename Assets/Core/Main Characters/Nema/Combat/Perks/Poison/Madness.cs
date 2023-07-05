using System;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.Poison;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;

namespace Core.Main_Characters.Nema.Combat.Perks.Poison
{
    public class Madness : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            MadnessInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record MadnessRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(MadnessRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            MadnessInstance instance = new(owner, this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class MadnessInstance : PerkInstance, IPoisonModifier, IBaseFloatAttributeModifier
    {
        private const float DamageModifier = 0.5f;
        public string SharedId => nameof(MadnessInstance);
        public int Priority => 999;

        private readonly Action<CharacterStateMachine> _onCharacterSetup;

        public MadnessInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
            _onCharacterSetup = OnCharacterSetup;
        }

        public MadnessInstance(CharacterStateMachine owner, MadnessRecord record) : base(owner, record)
        {
            _onCharacterSetup = OnCharacterSetup;
        }

        protected override void OnSubscribe()
        {
            Owner.StatusApplierModule.PoisonApplyModifiers.Add(this);
            if (Owner.Display.IsNone)
                return;

            CombatManager combatManager = Owner.Display.Value.CombatManager;
            foreach (CharacterStateMachine character in combatManager.Characters.GetAllFixed())
                if (character.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse)
                    character.StatsModule.SubscribePower(this, allowDuplicates: false);

            combatManager.Characters.CharacterSetup += _onCharacterSetup;
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatusApplierModule.PoisonApplyModifiers.Remove(this);
            if (Owner.Display.IsNone)
                return;
            
            CombatManager combatManager = Owner.Display.Value.CombatManager;
            foreach (CharacterStateMachine character in combatManager.Characters.GetAllFixed())
                character.StatsModule.UnsubscribePower(this);

            combatManager.Characters.CharacterSetup -= _onCharacterSetup;
        }

        public override PerkRecord GetRecord() => new MadnessRecord(Key);

        private void OnCharacterSetup(CharacterStateMachine character)
        {
            character.StatsModule.SubscribePower(this, allowDuplicates: false);
        }

        public void Modify(ref float value, CharacterStateMachine self)
        {
            foreach (StatusInstance status in self.StatusModule.GetAll)
            {
                if (status is Core.Combat.Scripts.Effects.Types.Poison.Poison poison && poison.Caster == Owner && status.IsActive)
                {
                    value += DamageModifier;
                    return;
                }
            }
        }

        public void Modify(ref PoisonToApply effectStruct)
        {
            effectStruct.PoisonPerTime *= 2;
        }
    }
}