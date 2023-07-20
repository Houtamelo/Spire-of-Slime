using System;
using Core.Combat.Scripts.Behaviour;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Combat.Scripts.Cues
{
    public struct CombatCueOptions
    {
        public static readonly Vector3 DefaultSpeed = Vector3.up * 0.3f;
        public const float DefaultFontSize = 0.6f;
        public const float DefaultDuration = 2f;

        public bool CanShowOnTopOfOthers;
        public string Text;
        public Color Color;
        public Vector3 WorldPosition;
        public Vector3 Speed;
        public float Duration;
        public float FontSize;
        public bool FadeOnComplete;

        /// <summary>
        /// Shaking text ignores Speed
        /// </summary>
        public bool Shake;

        public Action OnPlay;

        public CombatCueOptions(bool canShowOnTopOfOthers, string text, Color color, Vector3 position, Vector3 speed, float duration, float fontSize, bool fadeOnComplete, bool shake)
        {
            position.z = 0;
            speed.z = 0;
            Text = text;
            Color = color;
            WorldPosition = position;
            Speed = speed;
            Duration = duration;
            FontSize = fontSize;
            FadeOnComplete = fadeOnComplete;
            Shake = shake;
            CanShowOnTopOfOthers = canShowOnTopOfOthers;
            OnPlay = null;
        }

        public static CombatCueOptions Default(string text, Color color, [NotNull] DisplayModule display) => new(canShowOnTopOfOthers: false, text: text, color: color, DefaultPosition(display), speed: DefaultSpeed, duration: DefaultDuration, fontSize: DefaultFontSize, fadeOnComplete: true, shake: false);

        public static Vector3 DefaultPosition([NotNull] DisplayModule display) => display.GetCuePosition().TrySome(out Vector3 position) ? position : display.transform.position;
    }
}