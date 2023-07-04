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
using Main_Database.Combat;
using Save_Management;
using UnityEngine;
using Utils.Extensions;

namespace Core.Main_Characters.Ethel.Combat.Perks.Debuffer
{
    public class UnnervingAura : PerkScriptable
    {
        public override PerkInstance CreateInstance(CharacterStateMachine character)
        {
            UnnervingAuraInstance instance = new(character, Key);
            character.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public record UnnervingAuraRecord(CleanString Key, float AccumulatedTime) : PerkRecord(Key)
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

        public override PerkInstance CreateInstance(CharacterStateMachine owner, CharacterEnumerator allCharacters)
        {
            UnnervingAuraInstance instance = new(owner, record: this);
            owner.PerksModule.Add(instance);
            return instance;
        }
    }
    
    public class UnnervingAuraInstance : PerkInstance, ITick
    {
        private const float IntervalBetweenDebuff = 3f;
        private const float DebuffDuration = 3f;
        private const float DebuffModifier = -0.1f;
        private const float BaseApplyChance = 1f;

        private static readonly BuffOrDebuffScript Debuff = new(Permanent: false, DebuffDuration, BaseApplyChance, default, DebuffModifier);
        
        private float _accumulatedTime;
        public UnnervingAuraInstance(CharacterStateMachine owner, CleanString key) : base(owner, key)
        {
        }
        
        public UnnervingAuraInstance(CharacterStateMachine owner, UnnervingAuraRecord record) : base(owner, record)
        {
            _accumulatedTime = record.AccumulatedTime;
        }

        protected override void OnSubscribe()
        {
            Owner.SubscribedTickers.Add(this);
        }

        protected override void OnUnsubscribe()
        {
            Owner.SubscribedTickers.Remove(this);
        }
        
        public override PerkRecord GetRecord() => new UnnervingAuraRecord(Key, _accumulatedTime);

        public void Tick(float timeStep)
        {
            _accumulatedTime += timeStep;
            if (_accumulatedTime < IntervalBetweenDebuff || Owner.Display.IsNone)
                return;
            
            _accumulatedTime -= IntervalBetweenDebuff;
            CombatManager combatManager = Owner.Display.Value.CombatManager;
            
            int activeCount = 0;
            foreach (CharacterStateMachine _ in combatManager.Characters.GetEnemies(Owner))
                activeCount++;
                
            if (activeCount == 0)
                return;
                
            int randomIndex = Random.Range(0, activeCount);
            int currentIndex = 0;
                
            foreach (CharacterStateMachine enemy in combatManager.Characters.GetEnemies(Owner))
            {
                if (currentIndex != randomIndex)
                {
                    currentIndex++;
                    continue;
                }

                CombatStat stat = CombatUtils.GetRandomCombatStat();
                BuffOrDebuffToApply debuffStruct = (BuffOrDebuffToApply)Debuff.GetStatusToApply(Owner, enemy, false, null);
                debuffStruct.Stat = stat;
                BuffOrDebuffScript.ProcessModifiersAndTryApply(debuffStruct);
                return;
            }
        }
    }
}