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
using JetBrains.Annotations;

namespace Core.Main_Characters.Nema.Combat.Perks.Poison
{
    public class Madness : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            MadnessInstance instance = new(owner, this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class MadnessInstance : PerkInstance, IPoisonModifier, IBaseAttributeModifier
    {
        private const int DamageModifier = 50;

        private readonly Action<CharacterStateMachine> _onCharacterSetup;

        public MadnessInstance(CharacterStateMachine owner, CleanString key) : base(owner, key) => _onCharacterSetup = OnCharacterSetup;

        public MadnessInstance(CharacterStateMachine owner, [NotNull] MadnessRecord record) : base(owner, record) => _onCharacterSetup = OnCharacterSetup;

        protected override void OnSubscribe()
        {
            Owner.StatusApplierModule.PoisonApplyModifiers.Add(this);
            if (Owner.Display.IsNone)
                return;

            CombatManager combatManager = Owner.Display.Value.CombatManager;
            foreach (CharacterStateMachine character in combatManager.Characters.GetAllFixed())
            {
                if (character.StateEvaluator.PureEvaluate() is not (CharacterState.Defeated or CharacterState.Corpse))
                    character.StatsModule.SubscribePower(modifier: this, allowDuplicates: false);
            }

            combatManager.Characters.CharacterSetup += _onCharacterSetup;
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatusApplierModule.PoisonApplyModifiers.Remove(this);
            if (Owner.Display.IsNone)
                return;
            
            CombatManager combatManager = Owner.Display.Value.CombatManager;
            foreach (CharacterStateMachine character in combatManager.Characters.GetAllFixed())
                character.StatsModule.UnsubscribePower(modifier: this);

            combatManager.Characters.CharacterSetup -= _onCharacterSetup;
        }

        private void OnCharacterSetup([NotNull] CharacterStateMachine character)
        {
            character.StatsModule.SubscribePower(modifier: this, allowDuplicates: false);
        }

        public void Modify(ref int value, [NotNull] CharacterStateMachine self)
        {
            foreach (StatusInstance status in self.StatusReceiverModule.GetAll)
            {
                if (status is Core.Combat.Scripts.Effects.Types.Poison.Poison poison && poison.Caster == Owner && status.IsActive)
                {
                    value += DamageModifier;
                    return;
                }
            }
        }

        public void Modify([NotNull] ref PoisonToApply effectStruct)
        {
            effectStruct.PoisonPerSecond *= 2;
        }

        [NotNull]
        public override PerkRecord GetRecord() => new MadnessRecord(Key);

        [NotNull]
        public string SharedId => nameof(MadnessInstance);
        public int Priority => 999;
    }
}