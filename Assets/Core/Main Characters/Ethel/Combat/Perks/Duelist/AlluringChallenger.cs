﻿using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.Marked;
using Core.Combat.Scripts.Effects.Types.Riposte;
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

namespace Core.Main_Characters.Ethel.Combat.Perks.Duelist
{
    public class AlluringChallenger : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            AlluringChallengerInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record AlluringChallengerRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(AlluringChallengerRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            AlluringChallengerInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class AlluringChallengerInstance : PerkInstance, IRiposteModifier, ISkillModifier
    {
        private static readonly TSpan MarkedDuration = TSpan.FromSeconds(4);
        private static readonly MarkedScript MarkedScript = new(Permanent: false, MarkedDuration);
        private static readonly TSpan ExtraRiposteDuration = TSpan.FromSeconds(1);

        public AlluringChallengerInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }

        public AlluringChallengerInstance(CharacterStateMachine owner, [NotNull] AlluringChallengerRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe()
        {
            Owner.SkillModule.SkillModifiers.Add(this);
            Owner.StatusReceiverModule.RiposteReceiveModifiers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SkillModule.SkillModifiers.Remove(this);
            Owner.StatusReceiverModule.RiposteReceiveModifiers.Remove(this);
        }

        [NotNull]
        public override PerkRecord GetRecord() => new AlluringChallengerRecord(Key);

        public void Modify([NotNull] ref RiposteToApply effectStruct)
        {
            effectStruct.Duration += ExtraRiposteDuration;
        }

        public void Modify(ref SkillStruct skillStruct)
        {
            if (skillStruct.Skill.Key != EthelSkills.Challenge || skillStruct.Caster != Owner)
                return;

            ref CustomValuePooledList<IActualStatusScript> ownerEffects = ref skillStruct.CasterEffects;
            ownerEffects.Add(MarkedScript);
        }

        [NotNull]
        public string SharedId => nameof(AlluringChallengerInstance);
        public int Priority => 0;
    }
}