using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Main_Characters.Ethel.Combat.Perks.Tank
{
    public class Energetic : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            EnergeticInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record EnergeticRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(EnergeticRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            EnergeticInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class EnergeticInstance : PerkInstance
    {
        private const double StaminaMultiplier = 0.3;
        private readonly int _staminaToAdd;

        public EnergeticInstance([NotNull] CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
            if (owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                _staminaToAdd = (staminaModule.BaseMax * StaminaMultiplier).CeilToInt();
        }
        
        public EnergeticInstance(CharacterStateMachine owner, [NotNull] EnergeticRecord record) : base(owner, record) => _staminaToAdd = 0;

        protected override void OnSubscribe()
        {
            if (Owner.StaminaModule.IsNone || CreatedFromLoad)
                return;
            
            IStaminaModule staminaModule = Owner.StaminaModule.Value;
            staminaModule.SetMax(staminaModule.ActualMax + _staminaToAdd);
            staminaModule.SetCurrent(staminaModule.GetCurrent() + _staminaToAdd);
        }

        protected override void OnUnsubscribe()
        {
            if (Owner.StaminaModule.IsNone)
                return;
            
            IStaminaModule staminaModule = Owner.StaminaModule.Value;
            staminaModule.SetMax(staminaModule.ActualMax - _staminaToAdd);
            staminaModule.SetCurrent(staminaModule.GetCurrent() - _staminaToAdd);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new EnergeticRecord(Key);
    }
}