using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;

namespace Core.Main_Characters.Ethel.Combat.Perks.Duelist
{
    public class Release : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            ReleaseInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record ReleaseRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(ReleaseRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            ReleaseInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class ReleaseInstance : PerkInstance, IRiposteActivatedListener
    {
        private const int LustModifier = -3;

        public ReleaseInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public ReleaseInstance(CharacterStateMachine owner, [NotNull] ReleaseRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.Events.RiposteActivatedListeners.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.Events.RiposteActivatedListeners.Remove(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new ReleaseRecord(Key);

        public void OnRiposteActivated(ref ActionResult actionResult)
        {
            if (actionResult.Missed || actionResult.Caster != Owner || actionResult.Caster.PositionHandler.IsLeftSide == actionResult.Target.PositionHandler.IsLeftSide || Owner.LustModule.IsNone)
                return;

            Owner.LustModule.Value.ChangeLust(LustModifier);
        }
    }
}