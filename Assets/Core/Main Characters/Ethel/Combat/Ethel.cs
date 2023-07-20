using System;
using System.Collections.Generic;
using Core.Combat.Scripts;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Localization.Scripts;
using Core.Main_Database.Combat;
using Core.Save_Management.SaveObjects;
using Core.Utils.Collections;
using Core.Utils.Patterns;
using ListPool;
using Sirenix.OdinInspector;
using UnityEngine;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Main_Characters.Ethel.Combat
{
    public class Ethel : CharacterScriptable
    {
        public static readonly CleanString GlobalKey = "ethel";
        public override CleanString Key => GlobalKey;

        public static readonly LocalizedText NameTrans = new("charactername_ethel");
        public override LocalizedText CharacterName => NameTrans;

        private readonly ListPool<ISkill> _cachedSkills = new(SkillSet.MaxSkills);
        private int _cachedHash;

        public override ReadOnlySpan<ISkill> Skills
        {
            get
            {
                if (Save.AssertInstance(out Save save))
                    save.FillSkills(GlobalKey, fillMe: _cachedSkills, character: this, ref _cachedHash);
                
                return _cachedSkills.AsSpan();
            }
        }
        
        public override ReadOnlySpan<IPerk> GetStartingPerks
        {
            get
            {
                if (Save.AssertInstance(out Save save) == false || save.GetReadOnlyStats(GlobalKey).AssertSome(out IReadonlyCharacterStats stats) == false)
                    return ReadOnlySpan<IPerk>.Empty;

                CustomValuePooledList<CleanString> unlockedPerks = stats.GetEnabledPerks(save);
                ReadOnlySpan<IPerk> perkScripts = PerkDatabase.GetPerks(ref unlockedPerks);
                unlockedPerks.Dispose();
                
                return perkScripts;
            }
        }

        [SerializeField, Required]
        private RuntimeAnimatorController portraitAnimation;
        public override Option<RuntimeAnimatorController> GetPortraitAnimation => portraitAnimation;
        
        public override double ExpMultiplier => 1.0;

        public override (int lower, int upper) Damage
        {
            get
            {
                IReadonlyCharacterStats stats = Save.Current.EthelStats;
                return (stats.GetValue(GeneralStat.DamageLower), stats.GetValue(GeneralStat.DamageUpper));
            }
        }

        public override int Speed => Save.Current.EthelStats.GetValue(GeneralStat.Speed);

        public override int Stamina => Save.Current.EthelStats.GetValue(GeneralStat.Stamina);
        public override int StaminaAmplitude => 0;
        public override int Resilience => Save.Current.EthelStats.GetValue(GeneralStat.Resilience);

        public override int Accuracy => Save.Current.EthelStats.GetValue(GeneralStat.Accuracy);
        public override int Dodge => Save.Current.EthelStats.GetValue(GeneralStat.Dodge);
        public override int CriticalChance => Save.Current.EthelStats.GetValue(GeneralStat.CriticalChance);

        public override int StunMitigation => Save.Current.EthelStats.GetValue(GeneralStat.StunMitigation);
        
        public override int DebuffResistance => Save.Current.EthelStats.GetValue(GeneralStat.DebuffResistance);
        public override int DebuffApplyChance => Save.Current.EthelStats.GetValue(GeneralStat.DebuffApplyChance);

        public override int MoveResistance => Save.Current.EthelStats.GetValue(GeneralStat.MoveResistance);
        public override int MoveApplyChance => Save.Current.EthelStats.GetValue(GeneralStat.MoveApplyChance);

        public override int PoisonResistance => Save.Current.EthelStats.GetValue(GeneralStat.PoisonResistance);
        public override int PoisonApplyChance => Save.Current.EthelStats.GetValue(GeneralStat.PoisonApplyChance);
        
        public override int ArousalApplyChance => Save.Current.EthelStats.GetValue(GeneralStat.ArousalApplyChance);
        
        public override int Lust => Save.Current.EthelStats.GetValue(GeneralStat.Lust);
        public override int Temptation => Save.Current.EthelStats.GetValue(GeneralStat.Temptation);
        public override int Composure => Save.Current.EthelStats.GetValue(GeneralStat.Composure);
        public override int OrgasmLimit => Save.Current.EthelStats.GetValue(GeneralStat.OrgasmLimit);
        public override int OrgasmCount => Save.Current.EthelStats.GetValue(GeneralStat.OrgasmCount);

        public override bool IsControlledByPlayer => true;
        public override bool IsGirl => true;
        
        public override Option<(string parameter, float graphicalX)> DoesActiveSex(CharacterStateMachine other) => Option.None;
        public override Option<float> GetSexGraphicalX(string parameter) => Option.None;
    }
}