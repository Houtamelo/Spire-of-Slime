using System;
using System.Collections.Generic;
using Core.Combat.Scripts.Animations;
using Core.Combat.Scripts.Barks;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Managers;
using Core.Combat.Scripts.Perks;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Save_Management.SaveObjects;
using Core.Utils.Patterns;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Interfaces
{
    public interface ICharacterScript
    {
        CleanString Key { get; }
        string CharacterName { get; }
        
        GameObject RendererPrefab { get; }
        float IdleGraphicalX { get; }
        Sprite TimelineIcon { get; }

        Option<RuntimeAnimatorController> GetPortraitAnimation { get; }
        Option<Color> GetPortraitBackgroundColor { get; }
        Option<Sprite> GetPortrait { get; }
        Sprite LustPromptPortrait { get; }

        byte Size { get; }
        Race Race { get; }

        float Speed { get; }
        (uint lower, uint upper) Damage { get; }

        uint Stamina { get; }
        uint StaminaAmplitude { get; }
        float Resilience { get; }

        float PoisonResistance { get; }
        float PoisonApplyChance { get; }

        float DebuffResistance { get; }
        float DebuffApplyChance { get; }

        float MoveResistance { get; }
        float MoveApplyChance { get; }
        
        float ArousalApplyChance { get; }

        float StunRecoverySpeed { get; }

        float Accuracy { get; }
        float Critical { get; }
        float Dodge { get; }

        bool IsControlledByPlayer { get; }
        bool IsGirl { get; }
        bool CanActAsGirl => IsGirl && CombatManager.Instance.TrySome(out CombatManager combatManager) && combatManager.CombatSetupInfo.AllowLust;

        float ExpMultiplier { get; }

        IReadOnlyList<ISkill> Skills { get; }
        IReadOnlyList<ISkill> GetAllPossibleSkills();
        ReadOnlySpan<IPerk> GetStartingPerks { get; }

        uint Lust { get; }
        ClampedPercentage Temptation { get; }
        float Composure { get; }
        uint OrgasmLimit { get; }
        uint OrgasmCount { get; }
        
        float OrgasmDuration { get; }
        float DownedTime { get; }

        Option<string> GetBark(BarkType barkType, CharacterStateMachine character);
        
        bool BecomesCorpseOnDefeat(out CombatAnimation combatAnimation);

        /// <summary> Returns none if character does not have sex. </summary>
        Option<(string parameter, float graphicalX)> DoesActiveSex(CharacterStateMachine other);

        Option<float> GetSexGraphicalX(string parameter);
    }
}