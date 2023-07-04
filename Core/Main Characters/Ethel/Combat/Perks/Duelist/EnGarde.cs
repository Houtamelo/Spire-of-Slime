using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Effects.Types.Riposte;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Main_Database.Combat;
using Save_Management;
using Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Duelist
{
    public class EnGarde : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            EnGardeInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record EnGardeRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(EnGardeRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            EnGardeInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class EnGardeInstance : PerkInstance, IBaseFloatAttributeModifier, IRiposteModifier
    {
        private const float RiposteModifier = 0.35f;
        private const float SpeedModifier = -0.2f;
        public int Priority => 0;
        public string SharedId => nameof(EnGardeInstance);
        
        public EnGardeInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public EnGardeInstance(CharacterStateMachine owner, EnGardeRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.StatsModule.SubscribeSpeed(this, allowDuplicates: false);
            Owner.StatusModule.RiposteReceiveModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatsModule.UnsubscribeSpeed(this);
            Owner.StatusModule.RiposteReceiveModifiers.Remove(this);
        }

        public override PerkRecord GetRecord() => new EnGardeRecord(Key);

        public void Modify(ref float value, CharacterStateMachine self)
        {
            foreach (StatusInstance status in self.StatusModule.GetAll)
            {
                if (status.EffectType is not EffectType.Riposte || status.IsDeactivated)
                    continue;
                
                value += SpeedModifier;
                return;
            }
        }
        
        public void Modify(ref RiposteToApply effectStruct)
        {
            effectStruct.Power += RiposteModifier;
        }
    }
}