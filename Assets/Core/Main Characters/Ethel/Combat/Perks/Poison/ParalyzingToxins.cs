using System;
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Main_Characters.Ethel.Combat.Perks.Poison
{
    public class ParalyzingToxins : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            ParalyzingToxinsInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record ParalyzingToxinsRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(ParalyzingToxinsRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            ParalyzingToxinsInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class ParalyzingToxinsInstance : PerkInstance
    {
        private readonly Action<CharacterStateMachine> _onCharacterSetup;

        public ParalyzingToxinsInstance([NotNull] CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
            _onCharacterSetup = OnCharacterSetup;
            if (owner.Display.IsNone)
            {
                Debug.LogWarning("Owner display is none, can't access combat manager");
                return;
            }
            
            foreach (CharacterStateMachine character in owner.Display.Value.CombatManager.Characters.GetAllFixed())
            {
                if (character.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse)
                    character.StatsModule.SubscribeSpeed(new ParalyzingToxinsModifier(owner), allowDuplicates: false);
            }
        }
        
        public ParalyzingToxinsInstance([NotNull] CharacterStateMachine owner, [NotNull] ParalyzingToxinsRecord record) : base(owner, record)
        {
            _onCharacterSetup = OnCharacterSetup;
            if (owner.Display.IsNone)
            {
                Debug.LogWarning("Owner display is none, can't access combat manager");
                return;
            }
            
            foreach (CharacterStateMachine character in owner.Display.Value.CombatManager.Characters.GetAllFixed())
            {
                if (character.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse)
                    character.StatsModule.SubscribeSpeed(new ParalyzingToxinsModifier(owner), allowDuplicates: false);
            }
        }

        protected override void OnSubscribe()
        {
            if (Owner.Display.IsSome)
                Owner.Display.Value.CombatManager.Characters.CharacterSetup += _onCharacterSetup;
        }

        protected override void OnUnsubscribe()
        {
            if (Owner.Display.IsSome)
                Owner.Display.Value.CombatManager.Characters.CharacterSetup -= _onCharacterSetup;
        }

        [NotNull]
        public override PerkRecord GetRecord() => new ParalyzingToxinsRecord(Key);

        private void OnCharacterSetup([NotNull] CharacterStateMachine character)
        {
            character.StatsModule.SubscribeSpeed(new ParalyzingToxinsModifier(Owner), allowDuplicates: false);
        }
    }

    public class ParalyzingToxinsModifier : IBaseAttributeModifier
    {
        private const int MultiplierPerPoisonCount = 4;
        private const int MaxReduction = 30;

        private readonly CharacterStateMachine _caster;

        public ParalyzingToxinsModifier(CharacterStateMachine caster) => _caster = caster;

        public void Modify(ref int value, [NotNull] CharacterStateMachine self)
        {
            if (self.PositionHandler.IsLeftSide == _caster.PositionHandler.IsLeftSide)
                return;
            
            int poisonCount = 0;
            foreach (StatusInstance statusInstance in self.StatusReceiverModule.GetAll)
            {
                if (statusInstance is Core.Combat.Scripts.Effects.Types.Poison.Poison poison)
                    poisonCount += poison.DamagePerSecond;
            }

            int speedReduction = Mathf.Clamp(poisonCount * MultiplierPerPoisonCount, 0, MaxReduction);
            value -= speedReduction;
        }

        public int Priority => 0;
        [NotNull]
        public string SharedId => nameof(ParalyzingToxinsModifier);
    }
}