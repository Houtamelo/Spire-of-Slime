using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Behaviour.Modules;
using Core.Combat.Scripts.Effects.Types.Poison;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Data.Main_Characters.Nema;
using JetBrains.Annotations;

namespace Core.Main_Characters.Nema.Combat.Perks.Poison
{
    public class Melancholy : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            MelancholyInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record MelancholyRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(MelancholyRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            MelancholyInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class MelancholyInstance : PerkInstance, IPoisonModifier
    {
        private const int PoisonApplyChanceModifier = 20;

        public MelancholyInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public MelancholyInstance(CharacterStateMachine owner, [NotNull] MelancholyRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            IStatusApplierModule applierModule = Owner.StatusApplierModule;
            applierModule.PoisonApplyModifiers.Add(this);
            
            if (CreatedFromLoad)
                return;
            
            applierModule.BasePoisonApplyChance += PoisonApplyChanceModifier;
        }

        protected override void OnUnsubscribe()
        {
            IStatusApplierModule applierModule = Owner.StatusApplierModule;
            applierModule.PoisonApplyModifiers.Remove(this);
            applierModule.BasePoisonApplyChance -= PoisonApplyChanceModifier;
        }

        [NotNull]
        public override PerkRecord GetRecord() => new MelancholyRecord(Key);

        public void Modify([NotNull] ref PoisonToApply effectStruct)
        {
            if (effectStruct.FromSkill == false || effectStruct.GetSkill().Key != NemaSkills.Woe.key)
                return;

            effectStruct.PoisonPerSecond += 1;
        }

        [NotNull]
        public string SharedId => nameof(MelancholyInstance);
        public int Priority => 0;
    }
}