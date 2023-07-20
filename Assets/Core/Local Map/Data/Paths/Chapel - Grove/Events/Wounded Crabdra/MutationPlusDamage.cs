
using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using JetBrains.Annotations;

namespace Core.Local_Map.Data.Paths.Chapel___Grove.Events.Wounded_Crabdra
{
    public class MutationPlusDamage : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            MutationPlusDamageInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record MutationPlusDamageRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(MutationPlusDamageRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance(CharacterStateMachine owner, DirectCharacterEnumerator allCharacters) => new MutationPlusDamageInstance(owner, this);
    }

    public class MutationPlusDamageInstance : PerkInstance, ISkillModifier
    {
        private const int DamageMultiplier = 110;
        
        public MutationPlusDamageInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public MutationPlusDamageInstance(CharacterStateMachine owner, [NotNull] PerkRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe() => Owner.SkillModule.SkillModifiers.Add(this);
        protected override void OnUnsubscribe() => Owner.SkillModule.SkillModifiers.Remove(this);

        public void Modify(ref SkillStruct skillStruct)
        {
            if (skillStruct.Caster != Owner)
                return;

            int count = skillStruct.TargetProperties.Count;
            for (int i = 0; i < count; i++)
            {
                ref TargetProperties targetProperties = ref skillStruct.TargetProperties[i];
                if (targetProperties.Power.TrySome(out int basePower) && targetProperties.Target.Script.Race is Race.Mutation)
                    targetProperties.Power = (basePower * DamageMultiplier) / 100;
            }
        }

        [NotNull]
        public override PerkRecord GetRecord() => new MutationPlusDamageRecord(Key);
        [NotNull]
        public string SharedId => nameof(MutationPlusDamage);
        public int Priority => 2;
    }
}
