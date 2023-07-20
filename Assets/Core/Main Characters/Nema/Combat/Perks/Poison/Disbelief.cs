using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.Poison;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;

namespace Core.Main_Characters.Nema.Combat.Perks.Poison
{
    public class Disbelief : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            DisbeliefInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record DisbeliefRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(DisbeliefRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            DisbeliefInstance instance = new DisbeliefInstance(owner, this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class DisbeliefInstance : PerkInstance, IPoisonModifier
    {
        private static readonly TSpan ExtraPoisonDuration = TSpan.FromSeconds(1.0);
        
        public DisbeliefInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public DisbeliefInstance(CharacterStateMachine owner, [NotNull] DisbeliefRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.StatusApplierModule.PoisonApplyModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.StatusApplierModule.PoisonApplyModifiers.Remove(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new DisbeliefRecord(Key);

        public void Modify([NotNull] ref PoisonToApply effectStruct)
        {
            effectStruct.Duration += ExtraPoisonDuration;
        }

        public int Priority => 0;
        [NotNull]
        public string SharedId => nameof(DisbeliefInstance);
    }
}