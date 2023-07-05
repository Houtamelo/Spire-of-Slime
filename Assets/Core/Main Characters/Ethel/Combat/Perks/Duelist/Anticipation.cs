using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Duelist
{
    public class Anticipation : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            AnticipationInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record AnticipationRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(AnticipationRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            AnticipationInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class AnticipationInstance : PerkInstance, IBaseFloatAttributeModifier
    {
        public string SharedId => nameof(AnticipationInstance);
        public int Priority => 0;
        private const float Modifier = 0.15f;
        
        public AnticipationInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public AnticipationInstance(CharacterStateMachine owner, AnticipationRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.SubscribeResilience(this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.UnsubscribeResilience(this);
        }

        public override PerkRecord GetRecord() => new AnticipationRecord(Key);

        public void Modify(ref float value, CharacterStateMachine self)
        {
            foreach (StatusInstance statusInstance in self.StatusModule.GetAll)
            {
                if (statusInstance.EffectType is not EffectType.Riposte || statusInstance.IsDeactivated)
                    continue;
                
                value += Modifier;
                return;
            }
        }
    } 
}