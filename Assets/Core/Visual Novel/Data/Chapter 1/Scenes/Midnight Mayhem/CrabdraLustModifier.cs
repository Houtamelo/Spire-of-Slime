using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.Lust;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;

namespace Core.Visual_Novel.Data.Chapter_1.Scenes.Midnight_Mayhem
{
    public class CrabdraLustModifier : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character) => new CrabdraLustModifierInstance(character, Key);
    }
    
    public record CrabdraLustModifierRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(CrabdraLustModifierRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters) 
            => new CrabdraLustModifierInstance(owner, Key);
    }
    
    public class CrabdraLustModifierInstance : PerkInstance, ILustModifier
    {
        public string SharedId => nameof(CrabdraLustModifierInstance);
        public int Priority => 99999;

        public CrabdraLustModifierInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        protected override void OnSubscribe() => Owner.StatusApplierModule.LustApplyModifiers.Add(this);

        protected override void OnUnsubscribe() => Owner.StatusApplierModule.LustApplyModifiers.Remove(this);
        public override PerkRecord GetRecord() => new CrabdraLustModifierRecord(Key);

        public void Modify(ref LustToApply effectStruct) => effectStruct.Multiplier *= 3f;
    }
}