using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;
using ListPool;

namespace Core.Main_Characters.Ethel.Combat.Perks.Bruiser
{
    public class FocusedSwing : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            FocusedSwingInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record FocusedSwingRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(FocusedSwingRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            FocusedSwingInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }

    public class FocusedSwingInstance : PerkInstance, ISkillModifier
    {
        private static readonly TSpan Duration = TSpan.FromSeconds(4.0);
        private const int ApplyChance = 100;
        private const int Delta = -25;

        private static readonly BuffOrDebuffScript ResilienceDebuff = new(Permanent: false, Duration, ApplyChance, CombatStat.Resilience, Delta);

        public FocusedSwingInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public FocusedSwingInstance(CharacterStateMachine owner, [NotNull] FocusedSwingRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        { 
            Owner.SkillModule.SkillModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SkillModule.SkillModifiers.Add(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new FocusedSwingRecord(Key);

        public void Modify(ref SkillStruct skillStruct)
        {
            CleanString skillKey = skillStruct.Skill.Key;
            if (skillKey != EthelSkills.Clash && skillKey != EthelSkills.Sever)
                return;
            
            ref CustomValuePooledList<IActualStatusScript> targetEffectsReference = ref skillStruct.TargetEffects;
            targetEffectsReference.Add(ResilienceDebuff);
        }

        [NotNull]
        public string SharedId => nameof(FocusedSwingInstance);
        public int Priority => 0;
    }
}