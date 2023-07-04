using System;
using System.Collections.Generic;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Interfaces;
using ListPool;
using Main_Database.Combat;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Patterns;
using Save = Save_Management.Save;

namespace Data.Main_Characters.Ethel
{
    public class Ethel : CharacterScriptable
    {
        public static readonly CleanString GlobalKey = "ethel";

        public override CleanString Key => GlobalKey;
        public override string CharacterName => "Ethel";

        private readonly List<ISkill> _cachedSkills = new();
        private int _cachedHash;

        public override IReadOnlyList<ISkill> Skills
        {
            get
            {
                Save save = Save.Current;
                if (save == null)
                    return _cachedSkills;
                
                save.GetSkills(GlobalKey, _cachedSkills, character: this, ref _cachedHash);
                return _cachedSkills;
            }
        }
        
        public override ReadOnlySpan<IPerk> GetStartingPerks
        {
            get
            {
                if (Save.AssertInstance(out Save save) == false || save.GetReadOnlyStats(GlobalKey).AssertSome(out IReadonlyCharacterStats stats) == false)
                    return ReadOnlySpan<IPerk>.Empty;

                ValueListPool<CleanString> unlockedPerks = stats.GetEnabledPerks(save);
                ReadOnlySpan<IPerk> perkScripts = PerkDatabase.GetPerks(ref unlockedPerks);
                unlockedPerks.Dispose();
                return perkScripts;
            }
        }

        [SerializeField, Required]
        private RuntimeAnimatorController portraitAnimation;
        public override Option<RuntimeAnimatorController> GetPortraitAnimation => portraitAnimation;
        
        public override float ExpMultiplier => 1f;

        public override (uint lower, uint upper) Damage
        {
            get
            {
                IReadonlyCharacterStats stats = Save.Current.EthelStats;
                return ((uint)stats.GetValue(GeneralStat.DamageLower), (uint)stats.GetValue(GeneralStat.DamageUpper));
            }
        }

        public override float Speed => Save.Current.EthelStats.GetValue(GeneralStat.Speed);

        public override uint Stamina => (uint) Save.Current.EthelStats.GetValue(GeneralStat.Stamina);
        public override uint StaminaAmplitude => 0;
        public override float Resilience => Save.Current.EthelStats.GetValue(GeneralStat.Resilience);

        public override float Accuracy => Save.Current.EthelStats.GetValue(GeneralStat.Accuracy);
        public override float Dodge => Save.Current.EthelStats.GetValue(GeneralStat.Dodge);
        public override float Critical => Save.Current.EthelStats.GetValue(GeneralStat.CriticalChance);

        public override float StunRecoverySpeed => Save.Current.EthelStats.GetValue(GeneralStat.StunRecoverySpeed);
        
        public override float DebuffResistance => Save.Current.EthelStats.GetValue(GeneralStat.DebuffResistance);
        public override float DebuffApplyChance => Save.Current.EthelStats.GetValue(GeneralStat.DebuffApplyChance);

        public override float MoveResistance => Save.Current.EthelStats.GetValue(GeneralStat.MoveResistance);
        public override float MoveApplyChance => Save.Current.EthelStats.GetValue(GeneralStat.MoveApplyChance);

        public override float PoisonResistance => Save.Current.EthelStats.GetValue(GeneralStat.PoisonResistance);
        public override float PoisonApplyChance => Save.Current.EthelStats.GetValue(GeneralStat.PoisonApplyChance);
        
        public override float ArousalApplyChance => Save.Current.EthelStats.GetValue(GeneralStat.ArousalApplyChance);
        
        public override uint Lust => (uint) Save.Current.EthelStats.GetValue(GeneralStat.Lust);
        public override ClampedPercentage Temptation => Save.Current.EthelStats.GetValue(GeneralStat.Temptation);
        public override float Composure => Save.Current.EthelStats.GetValue(GeneralStat.Composure);
        public override uint OrgasmLimit => (uint) Save.Current.EthelStats.GetValue(GeneralStat.OrgasmLimit);
        public override uint OrgasmCount => (uint) Save.Current.EthelStats.GetValue(GeneralStat.OrgasmCount);

        public override bool IsControlledByPlayer => true;
        public override bool IsGirl => true;
        
        public override Option<(string parameter, float graphicalX)> DoesActiveSex(CharacterStateMachine other) => Option.None;
        public override Option<float> GetSexGraphicalX(string parameter) => Option.None;
    }
}