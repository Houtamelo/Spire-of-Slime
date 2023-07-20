using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;

namespace Core.Main_Characters.Ethel.Combat.Perks.Tank
{
    public class Conspicuous : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            ConspicuousInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record ConspicuousRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(ConspicuousRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            ConspicuousInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class ConspicuousInstance : PerkInstance, IChangeMark
    {
        private const float MaxTargetBonus = 0.15f;
        private const float MinTargetBonus = 0.2f;
        private const float ChanceMultiplier = 1.5f;

        public ConspicuousInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public ConspicuousInstance(CharacterStateMachine owner, [NotNull] ConspicuousRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.AIModule.MarkModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.AIModule.MarkModifiers.Remove(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new ConspicuousRecord(Key);

        public void ChangeMultiplierTargetChance(CharacterStateMachine caster, CharacterStateMachine target, ref float chance)
        {
            if (target == Owner)
                chance *= ChanceMultiplier;
        }

        public void ChangeMaxTargetChance(CharacterStateMachine caster, CharacterStateMachine target, ref float chance)
        {
            chance += MaxTargetBonus;
        }

        public void ChangeMinTargetChance(CharacterStateMachine caster, CharacterStateMachine target, ref float chance)
        {
            chance += MinTargetBonus;
        }

        [NotNull]
        public string SharedId => nameof(ConspicuousInstance);
        public int Priority => 0;
    }
}