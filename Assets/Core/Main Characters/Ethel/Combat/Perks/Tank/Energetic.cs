using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Main_Database.Combat;
using Save_Management;
using Utils.Extensions;
using Utils.Math;

namespace Core.Main_Characters.Ethel.Combat.Perks.Tank
{
    public class Energetic : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
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

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            EnergeticInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class EnergeticInstance : PerkInstance
    {
        private const float StaminaMultiplier = 0.3f;
        private readonly uint _staminaToAdd;

        public EnergeticInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
            if (owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
            {
                _staminaToAdd = (staminaModule.BaseMax * StaminaMultiplier).CeilToUInt();
            }
        }
        
        public EnergeticInstance(CharacterStateMachine owner, EnergeticRecord record) : base(owner, record)
        {
            _staminaToAdd = 0;
        }

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

        public override PerkRecord GetRecord() => new EnergeticRecord(Key);
    }
}