using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.Poison;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Main_Database.Combat;
using Save_Management;
using Utils.Extensions;

namespace Core.Main_Characters.Nema.Combat.Perks.Poison
{
    public class Disbelief : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
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

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            DisbeliefInstance instance = new DisbeliefInstance(owner, this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class DisbeliefInstance : PerkInstance, IPoisonModifier
    {
        public int Priority => 0;
        public string SharedId => nameof(DisbeliefInstance);

        public DisbeliefInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public DisbeliefInstance(CharacterStateMachine owner, DisbeliefRecord record) : base(owner, record)
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

        public override PerkRecord GetRecord() => new DisbeliefRecord(Key);

        public void Modify(ref PoisonToApply effectStruct)
        {
            effectStruct.Duration += 1;
        }
    }
}