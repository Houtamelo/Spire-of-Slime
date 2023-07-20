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

namespace Core.Main_Characters.Nema.Combat
{
    public class Nema : CharacterScriptable
    {
        public static readonly CleanString GlobalKey = "nema";
        public override CleanString Key => GlobalKey;

        public static readonly LocalizedText NameTrans = new("charactername_nema");
        public override LocalizedText CharacterName => NameTrans;

        private readonly ListPool<ISkill> _cachedSkills = new (SkillSet.MaxSkills);
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

                CustomValuePooledList<CleanString> unlockedPerks = stats.GetUnlockedPerks(save);
                ReadOnlySpan<IPerk> perkScripts = PerkDatabase.GetPerks(ref unlockedPerks);
                unlockedPerks.Dispose();
                
                return perkScripts;
            }
        }

        [SerializeField, Required] private RuntimeAnimatorController portraitAnimation;
        public override Option<RuntimeAnimatorController> GetPortraitAnimation => portraitAnimation;

        public override int Speed => Save.Current.NemaStats.GetValue(GeneralStat.Speed);
        public override (int lower, int upper) Damage
        {
            get
            {
                IReadonlyCharacterStats stats = Save.Current.NemaStats;
                return (stats.GetValue(GeneralStat.DamageLower), stats.GetValue(GeneralStat.DamageUpper));
            }
        }
        
        public override int Stamina => Save.Current.NemaStats.GetValue(GeneralStat.Stamina);
        public override int StaminaAmplitude => 0;
        public override int Resilience => Save.Current.NemaStats.GetValue(GeneralStat.Resilience);
        
        public override int Accuracy => Save.Current.NemaStats.GetValue(GeneralStat.Accuracy);
        public override int Dodge => Save.Current.NemaStats.GetValue(GeneralStat.Dodge);
        public override int CriticalChance => Save.Current.NemaStats.GetValue(GeneralStat.CriticalChance);
        
        public override int DebuffResistance => Save.Current.NemaStats.GetValue(GeneralStat.DebuffResistance);
        public override int DebuffApplyChance => Save.Current.NemaStats.GetValue(GeneralStat.DebuffApplyChance);
        
        public override int MoveResistance => Save.Current.NemaStats.GetValue(GeneralStat.MoveResistance);
        public override int MoveApplyChance => Save.Current.NemaStats.GetValue(GeneralStat.MoveApplyChance);
        
        public override int PoisonResistance => Save.Current.NemaStats.GetValue(GeneralStat.PoisonResistance);
        public override int PoisonApplyChance => Save.Current.NemaStats.GetValue(GeneralStat.PoisonApplyChance);
        
        public override int ArousalApplyChance => Save.Current.NemaStats.GetValue(GeneralStat.ArousalApplyChance);
        
        public override int Lust => Save.Current.NemaStats.GetValue(GeneralStat.Lust);
        public override int Temptation => Save.Current.NemaStats.GetValue(GeneralStat.Temptation);
        public override int Composure => Save.Current.NemaStats.GetValue(GeneralStat.Composure);
        public override int OrgasmLimit => Save.Current.NemaStats.GetValue(GeneralStat.OrgasmLimit);
        public override int OrgasmCount => Save.Current.NemaStats.GetValue(GeneralStat.OrgasmCount);
        
        public override double ExpMultiplier => 1f;
        public override int StunMitigation => Save.Current.NemaStats.GetValue(GeneralStat.StunMitigation);
        
        public override bool IsGirl => true;
        public override bool IsControlledByPlayer => true;

        public override Option<(string parameter, float graphicalX)> DoesActiveSex(CharacterStateMachine other) => Option.None;
        public override Option<float> GetSexGraphicalX(string parameter) => Option.None;
    }
}