using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Interfaces;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;

namespace Core.Main_Characters.Ethel.Combat.Perks.Duelist
{
    public class Anticipation : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
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

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            AnticipationInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class AnticipationInstance : PerkInstance, IBaseAttributeModifier
    {
        private const int Modifier = 15;

        public AnticipationInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public AnticipationInstance(CharacterStateMachine owner, [NotNull] AnticipationRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.SubscribeResilience(modifier: this, allowDuplicates: false);
        }

        protected override void OnUnsubscribe()
        {
            if (Owner.StaminaModule.TrySome(out IStaminaModule staminaModule))
                staminaModule.UnsubscribeResilience(modifier: this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new AnticipationRecord(Key);

        public void Modify(ref int value, [NotNull] CharacterStateMachine self)
        {
            foreach (StatusInstance statusInstance in self.StatusReceiverModule.GetAll)
            {
                if (statusInstance.EffectType is not EffectType.Riposte || statusInstance.IsDeactivated)
                    continue;
                
                value += Modifier;
                return;
            }
        }

        [NotNull]
        public string SharedId => nameof(AnticipationInstance);
        public int Priority => 0;
    } 
}