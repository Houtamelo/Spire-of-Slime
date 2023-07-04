using System.Collections.Generic;
using Core.Combat.Scripts.Behaviour;
using Core.Pause_Menu.Scripts;
using UnityEngine;

namespace Core.Combat.Scripts.Skills.Action
{
    public interface IActionSequence
    {
        public static float SpeedMultiplier => PauseMenuManager.CombatAnimationSpeedHandler.Value;
        public static float DurationMultiplier => 1f / SpeedMultiplier;
        
        public static readonly Vector3 CharacterScale = new(x: 1.25f, y: 1.25f, z: 1.25f);
        
        public const float YPosition = 0f;
        public const float DefaultMiddlePadding = 2f;
        public const float DefaultInBetweenPadding = 2f;
        
        private const float PopDurationBase = 0.1f;
        public static float PopDuration => PopDurationBase * DurationMultiplier;
        
        private const float AnimationDurationBase = 2f;
        public static float AnimationDuration => AnimationDurationBase * DurationMultiplier;
        
        private const float StartDurationBase = 1.75f;
        public static float StartDuration => StartDurationBase * DurationMultiplier;
        
        private const float BarsFadeDurationBase = 0.25f;
        public static float BarsFadeDuration => BarsFadeDurationBase * DurationMultiplier;
        
        private const float ShakeDurationBase = 0.2f;
        public static float ShakeDuration => ShakeDurationBase * DurationMultiplier;
        
        IReadOnlyCollection<CharacterStateMachine> Targets { get; }
        IReadOnlyCollection<CharacterStateMachine> Outsiders { get; }
        CharacterStateMachine Caster { get; }
        
        bool IsPlaying { get; }
        bool IsDone { get; }
        void Play();
        void ForceStop();
        
        void UpdateCharactersStartPosition();
        void InstantMoveOutsideCharacters();
        void AddOutsider(CharacterStateMachine summoned);
    }
}