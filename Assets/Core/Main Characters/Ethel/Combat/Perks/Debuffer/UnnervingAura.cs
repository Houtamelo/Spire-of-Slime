using System.Collections.Generic;
using System.Text;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.Types.BuffOrDebuff;
using Core.Combat.Scripts.Enums;
using Core.Combat.Scripts.Interfaces;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Managers.Enumerators;
using Core.Combat.Scripts.Perks;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Math;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Main_Characters.Ethel.Combat.Perks.Debuffer
{
    public class UnnervingAura : PerkScriptable
    {
        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine character)
        {
            UnnervingAuraInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record UnnervingAuraRecord(CleanString Key, TSpan AccumulatedTime) : PerkRecord(Key)
    {
        public override bool IsDataValid(StringBuilder errors, ICollection<CharacterRecord> allCharacters)
        {
            if (PerkDatabase.GetPerk(Key).IsNone)
            {
                errors.AppendLine("Invalid ", nameof(UnnervingAuraRecord), " data. ", nameof(Key), " with key: ", Key.ToString(), " does not exist in database.");
                return false;
            }
            
            return true;
        }

        [NotNull]
        public override PerkInstance CreateInstance([NotNull] CharacterStateMachine owner, DirectCharacterEnumerator allCharacters)
        {
            UnnervingAuraInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class UnnervingAuraInstance : PerkInstance, ITick
    {
        private static readonly TSpan IntervalBetweenDebuffs = TSpan.FromSeconds(3.0);
        private const int DebuffModifier = -10;
        private const int BaseApplyChance = 100;
        private static readonly TSpan DebuffDuration = TSpan.FromSeconds(3.0);

        private static readonly BuffOrDebuffScript Debuff = new(Permanent: false, DebuffDuration, BaseApplyChance, Stat: default, DebuffModifier);
        
        private TSpan _accumulatedTime;
        
        public UnnervingAuraInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public UnnervingAuraInstance(CharacterStateMachine owner, [NotNull] UnnervingAuraRecord record) : base(owner, record) => _accumulatedTime = record.AccumulatedTime;

        protected override void OnSubscribe()
        {
            Owner.SubscribedTickers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SubscribedTickers.Remove(this);
        }
        
        [NotNull]
        public override PerkRecord GetRecord() => new UnnervingAuraRecord(Key, _accumulatedTime);

        public void Tick(TSpan timeStep)
        {
            _accumulatedTime += timeStep;
            if (_accumulatedTime < IntervalBetweenDebuffs || Owner.Display.IsNone)
                return;
            
            _accumulatedTime -= IntervalBetweenDebuffs;
            CombatManager combatManager = Owner.Display.Value.CombatManager;
            
            int activeCount = 0;
            foreach (CharacterStateMachine _ in combatManager.Characters.GetEnemies(Owner))
                activeCount++;
                
            if (activeCount == 0)
                return;
                
            int randomIndex = Save.Random.Next(activeCount);
            int currentIndex = 0;
                
            foreach (CharacterStateMachine enemy in combatManager.Characters.GetEnemies(Owner))
            {
                if (currentIndex != randomIndex)
                {
                    currentIndex++;
                    continue;
                }

                CombatStat stat = CombatUtils.GetRandomCombatStat();
                BuffOrDebuffToApply debuffStruct = (BuffOrDebuffToApply)Debuff.GetStatusToApply(caster: Owner, target: enemy, crit: false, skill: null);
                debuffStruct.Stat = stat;
                BuffOrDebuffScript.ProcessModifiersAndTryApply(debuffStruct);
                return;
            }
        }
    }
}