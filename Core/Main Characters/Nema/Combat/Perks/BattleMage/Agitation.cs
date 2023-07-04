using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Interfaces.Modules;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Main_Database.Combat;
using Save_Management;
using Utils.Extensions;

namespace Core.Main_Characters.Nema.Combat.Perks.BattleMage
{
    public class Agitation : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            AgitationInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record AgitationRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(AgitationRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            AgitationInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class AgitationInstance : PerkInstance
    {
        private static readonly DamageMultiplierModifier DamageModifierInstance = new();
        private static readonly ComposureModifier ComposureModifierInstance = new();
        
        public AgitationInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public AgitationInstance(CharacterStateMachine owner, AgitationRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.StatsModule.SubscribePower(DamageModifierInstance, allowDuplicates: false);
            
            if (Owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.SubscribeComposure(ComposureModifierInstance, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatsModule.UnsubscribePower(DamageModifierInstance);
            
            if (Owner.LustModule.TrySome(out ILustModule lustModule))
                lustModule.UnsubscribeComposure(ComposureModifierInstance);
        }

        public override PerkRecord GetRecord() => new AgitationRecord(Key);

        private class DamageMultiplierModifier : IBaseFloatAttributeModifier
        {
            public string SharedId => nameof(AgitationInstance);
            public int Priority => 0;
            public void Modify(ref float value, CharacterStateMachine self)
            {
                float delta = self.StatsModule.GetSpeed() - 1;
                if (delta > 0)
                    value += delta;
            }
        }
        
        private class ComposureModifier : IBaseFloatAttributeModifier
        {
            public string SharedId => nameof(AgitationInstance);
            public int Priority => 0;
            public void Modify(ref float value, CharacterStateMachine self)
            {
                float delta = self.StatsModule.GetSpeed() - 1;
                if (delta < 0)
                    value += delta;
            }
        }
    }
}