using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Localization.Scripts;
using Core.Save_Management.SaveObjects;
using Core.Utils.Math;
using Core.Utils.Patterns;
using UnityEngine;

namespace Core.Combat.Scripts.Interfaces
{
    public interface ICharacterScript
    {
        CleanString Key { get; }
        LocalizedText CharacterName { get; }
        
        GameObject RendererPrefab { get; }
        float IdleGraphicalX { get; }
        Sprite TimelineIcon { get; }

        Option<RuntimeAnimatorController> GetPortraitAnimation { get; }
        Option<Color> GetPortraitBackgroundColor { get; }
        Option<Sprite> GetPortrait { get; }

        byte Size { get; }
        Race Race { get; }

        int Speed { get; }
        (int lower, int upper) Damage { get; }

        int Stamina { get; }
        int StaminaAmplitude { get; }
        int Resilience { get; }

        int PoisonResistance { get; }
        int PoisonApplyChance { get; }

        int DebuffResistance { get; }
        int DebuffApplyChance { get; }

        int MoveResistance { get; }
        int MoveApplyChance { get; }
        
        int ArousalApplyChance { get; }

        int StunMitigation { get; }

        int Accuracy { get; }
        int CriticalChance { get; }
        int Dodge { get; }

        bool IsControlledByPlayer { get; }
        bool IsGirl { get; }
        bool CanActAsGirl => IsGirl && CombatManager.Instance.TrySome(out CombatManager combatManager) && combatManager.CombatSetupInfo.AllowLust;

        double ExpMultiplier { get; }

        ReadOnlySpan<ISkill> Skills { get; }
        ReadOnlySpan<ISkill> GetAllPossibleSkills();
        ReadOnlySpan<IPerk> GetStartingPerks { get; }

        int Lust { get; }
        int Temptation { get; }
        int Composure { get; }
        int OrgasmLimit { get; }
        int OrgasmCount { get; }
        
        TSpan OrgasmDuration { get; }
        TSpan DownedTime { get; }

        Option<string> GetBark(BarkType barkType, CharacterStateMachine character);
        
        bool BecomesCorpseOnDefeat(out CombatAnimation combatAnimation);

        /// <summary> Returns none if character does not have sex. </summary>
        Option<(string parameter, float graphicalX)> DoesActiveSex(CharacterStateMachine other);

        Option<float> GetSexGraphicalX(string parameter);
    }
}