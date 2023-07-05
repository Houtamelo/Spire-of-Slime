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
using UnityEngine;

namespace Core.Main_Characters.Ethel.Combat.Perks.Poison
{
    public class ParalyzingToxins : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
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

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            ParalyzingToxinsInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class ParalyzingToxinsInstance : PerkInstance
    {
        private readonly Action<CharacterStateMachine> _onCharacterSetup;

        public ParalyzingToxinsInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
            _onCharacterSetup = OnCharacterSetup;
            if (owner.Display.IsNone)
            {
                Debug.LogWarning("Owner display is none, can't access combat manager");
                return;
            }
            
            foreach (CharacterStateMachine character in owner.Display.Value.CombatManager.Characters.GetAllFixed())
                if (character.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse)
                    character.StatsModule.SubscribeSpeed(new ParalyzingToxinsModifier(owner), allowDuplicates: false);
        }
        
        public ParalyzingToxinsInstance(CharacterStateMachine owner, ParalyzingToxinsRecord record) : base(owner, record)
        {
            _onCharacterSetup = OnCharacterSetup;
            if (owner.Display.IsNone)
            {
                Debug.LogWarning("Owner display is none, can't access combat manager");
                return;
            }
            
            foreach (CharacterStateMachine character in owner.Display.Value.CombatManager.Characters.GetAllFixed())
                if (character.StateEvaluator.PureEvaluate() is not CharacterState.Defeated and not CharacterState.Corpse)
                    character.StatsModule.SubscribeSpeed(new ParalyzingToxinsModifier(owner), allowDuplicates: false);
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

        public override PerkRecord GetRecord() => new ParalyzingToxinsRecord(Key);

        private void OnCharacterSetup(CharacterStateMachine character)
        {
            character.StatsModule.SubscribeSpeed(new ParalyzingToxinsModifier(Owner), allowDuplicates: false);
        }
    }

    public class ParalyzingToxinsModifier : IBaseFloatAttributeModifier
    {
        public int Priority => 0;
        public string SharedId => nameof(ParalyzingToxinsModifier);
        private const float MultiplierPerPoisonCount = 0.04f;
        private const float MaxReduction = 0.3f;
        
        private readonly CharacterStateMachine _caster;

        public ParalyzingToxinsModifier(CharacterStateMachine caster)
        {
            _caster = caster;
        }

        public void Modify(ref float value, CharacterStateMachine self)
        {
            if (self.PositionHandler.IsLeftSide == _caster.PositionHandler.IsLeftSide)
                return;
            
            uint poisonCount = 0;
            foreach (StatusInstance statusInstance in self.StatusModule.GetAll)
                if (statusInstance is Core.Combat.Scripts.Effects.Types.Poison.Poison poison)
                    poisonCount += poison.DamagePerTime;

            float speedReduction = Mathf.Clamp(poisonCount * MultiplierPerPoisonCount, 0, MaxReduction);
            value -= speedReduction;
        }
    }
}