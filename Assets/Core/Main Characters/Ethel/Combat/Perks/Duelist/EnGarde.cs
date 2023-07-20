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
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;

namespace Core.Main_Characters.Ethel.Combat.Perks.Duelist
{
    public class EnGarde : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            EnGardeInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class EnGardeInstance : PerkInstance, IBaseAttributeModifier, IRiposteModifier
    {
        private const int RiposteModifier = 35;
        private const int SpeedModifier = -20;

        public EnGardeInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public EnGardeInstance(CharacterStateMachine owner, [NotNull] EnGardeRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.StatsModule.SubscribeSpeed(this, allowDuplicates: false);
            Owner.StatusReceiverModule.RiposteReceiveModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatsModule.UnsubscribeSpeed(this);
            Owner.StatusReceiverModule.RiposteReceiveModifiers.Remove(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new EnGardeRecord(Key);

        public void Modify(ref int value, [NotNull] CharacterStateMachine self)
        {
            foreach (StatusInstance status in self.StatusReceiverModule.GetAll)
            {
                if (status.EffectType is not EffectType.Riposte || status.IsDeactivated)
                    continue;
                
                value += SpeedModifier;
                return;
            }
        }

        public void Modify([NotNull] ref RiposteToApply effectStruct)
        {
            effectStruct.Power += RiposteModifier;
        }

        public int Priority => 0;
        [NotNull]
        public string SharedId => nameof(EnGardeInstance);
    }
}