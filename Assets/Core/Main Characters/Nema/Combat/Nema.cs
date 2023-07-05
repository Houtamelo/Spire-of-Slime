using System;
using System.Collections.Generic;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using ListPool;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Main_Characters.Nema.Combat
{
    public class Nema : CharacterScriptable
    {
        public static readonly CleanString GlobalKey = "nema";
        public override CleanString Key => GlobalKey;
        public override string CharacterName => "Nema";

        private readonly List<ISkill> _cachedSkills = new();
        private int _cachedHash;

        public override IReadOnlyList<ISkill> Skills
        {
            get
            {
                Save save = Save.Current;
                if (save == null)
                {
                    Debug.LogWarning("Save is null, returning cached skill list", this);
                    return _cachedSkills;
                }
                
                save.GetSkills(GlobalKey, destinationList: _cachedSkills, character: this, ref _cachedHash);
                return _cachedSkills;
            }
        }
        
        public override ReadOnlySpan<IPerk> GetStartingPerks
        {
            get
            {
                if (Save.AssertInstance(out Save save) == false || save.GetReadOnlyStats(GlobalKey).AssertSome(out IReadonlyCharacterStats stats) == false)
                    return ReadOnlySpan<IPerk>.Empty;

                ValueListPool<CleanString> unlockedPerks = stats.GetUnlockedPerks(save);
                ReadOnlySpan<IPerk> perkScripts = PerkDatabase.GetPerks(ref unlockedPerks);
                unlockedPerks.Dispose();
                return perkScripts;
            }
        }

        [SerializeField, Required] private RuntimeAnimatorController portraitAnimation;
        public override Option<RuntimeAnimatorController> GetPortraitAnimation => portraitAnimation;

        public override float Speed => Save.Current.NemaStats.GetValue(GeneralStat.Speed);
        public override (uint lower, uint upper) Damage
        {
            get
            {
                IReadonlyCharacterStats stats = Save.Current.NemaStats;
                return ((uint) stats.GetValue(GeneralStat.DamageLower), (uint) stats.GetValue(GeneralStat.DamageUpper));
            }
        }
        
        public override uint Stamina => (uint) Save.Current.NemaStats.GetValue(GeneralStat.Stamina);
        public override uint StaminaAmplitude => 0;
        public override float Resilience => Save.Current.NemaStats.GetValue(GeneralStat.Resilience);
        
        public override float Accuracy => Save.Current.NemaStats.GetValue(GeneralStat.Accuracy);
        public override float Dodge => Save.Current.NemaStats.GetValue(GeneralStat.Dodge);
        public override float Critical => Save.Current.NemaStats.GetValue(GeneralStat.CriticalChance);
        
        public override float DebuffResistance => Save.Current.NemaStats.GetValue(GeneralStat.DebuffResistance);
        public override float DebuffApplyChance => Save.Current.NemaStats.GetValue(GeneralStat.DebuffApplyChance);
        
        public override float MoveResistance => Save.Current.NemaStats.GetValue(GeneralStat.MoveResistance);
        public override float MoveApplyChance => Save.Current.NemaStats.GetValue(GeneralStat.MoveApplyChance);
        
        public override float PoisonResistance => Save.Current.NemaStats.GetValue(GeneralStat.PoisonResistance);
        public override float PoisonApplyChance => Save.Current.NemaStats.GetValue(GeneralStat.PoisonApplyChance);
        
        public override float ArousalApplyChance => Save.Current.NemaStats.GetValue(GeneralStat.ArousalApplyChance);
        
        public override uint Lust => (uint) Save.Current.NemaStats.GetValue(GeneralStat.Lust);
        public override ClampedPercentage Temptation => Save.Current.NemaStats.GetValue(GeneralStat.Temptation);
        public override float Composure => Save.Current.NemaStats.GetValue(GeneralStat.Composure);
        public override uint OrgasmLimit => (uint) Save.Current.NemaStats.GetValue(GeneralStat.OrgasmLimit);
        public override uint OrgasmCount => (uint) Save.Current.NemaStats.GetValue(GeneralStat.OrgasmCount);
        
        public override float ExpMultiplier => 1f;
        public override float StunRecoverySpeed => Save.Current.NemaStats.GetValue(GeneralStat.StunRecoverySpeed);
        
        public override bool IsGirl => true;
        public override bool IsControlledByPlayer => true;

        public override Option<(string parameter, float graphicalX)> DoesActiveSex(CharacterStateMachine other) => Option.None;
        public override Option<float> GetSexGraphicalX(string parameter) => Option.None;
    }
}