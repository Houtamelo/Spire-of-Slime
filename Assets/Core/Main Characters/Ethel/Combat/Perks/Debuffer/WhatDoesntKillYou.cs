using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces.Events;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Action;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using JetBrains.Annotations;

namespace Core.Main_Characters.Ethel.Combat.Perks.Debuffer
{
    public class WhatDoesntKillYou : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            WhatDoesntKillYouInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record WhatDoesntKillYouRecord(CleanString Key) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(WhatDoesntKillYouRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            WhatDoesntKillYouInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class WhatDoesntKillYouInstance : PerkInstance, IStatusReceivedListener
    {
        private const int ApplyChance = 100;
        private static readonly BuffOrDebuffScript Buff = new(Permanent: false, BaseDuration: default, ApplyChance, Stat: default, BaseDelta: default);

        public WhatDoesntKillYouInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public WhatDoesntKillYouInstance(CharacterStateMachine owner, [NotNull] WhatDoesntKillYouRecord record) : base(owner, record)
        {
        }

        protected override void OnSubscribe() => Owner.Events.StatusReceivedListeners.Add(this);

        protected override void OnUnsubscribe() => Owner.Events.StatusReceivedListeners.Remove(this);

        [NotNull]
        public override PerkRecord GetRecord() => new WhatDoesntKillYouRecord(Key);

        private bool _recursionLock;

        public void OnStatusReceived(ref StatusResult result)
        {
            if (_recursionLock)
                return;
            
            Option<StatusInstance> instance = result.StatusInstance;
            if (result.IsFailure || instance.IsNone || instance.Value is not BuffOrDebuff buffOrDebuff || buffOrDebuff.IsPositive || result.Caster == Owner)
                return;

            _recursionLock = true;
            CombatStat debuffedStat = buffOrDebuff.Attribute;
            CombatStat statToApply = CombatUtils.GetRandomCombatStatExcept(debuffedStat);

            BuffOrDebuffToApply buffToApply = (BuffOrDebuffToApply)Buff.GetStatusToApply(caster: Owner, target: Owner, crit: false, skill: null);
            buffToApply.Stat = statToApply;
            buffToApply.Duration = buffOrDebuff.Duration;
            buffToApply.Delta = -1 * buffOrDebuff.GetDelta;

            BuffOrDebuffScript.ProcessModifiersAndTryApply(buffToApply);
            _recursionLock = false;
        }
    }
}