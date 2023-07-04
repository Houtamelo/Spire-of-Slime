using TMPro;
using UnityEngine;

namespace Core.Game_Manager.Scripts
{
    public readonly struct WorldCueOptions
    {
        public readonly string Text;
        
        /// <summary> This is divided by 100 before being fed to the cue, world cues are 100x bigger. </summary>
        public readonly float Size;
        public readonly Vector3 WorldPosition;
        public readonly Color Color;
        public readonly float StayDuration;
        public readonly float FadeDuration;
        public readonly Vector3 Speed;
        public readonly HorizontalAlignmentOptions Alignment;
        public readonly bool StopOthers;
        
        public WorldCueOptions(string text, float size, Vector3 worldPosition, Color color, float stayDuration, float fadeDuration, Vector3 speed, HorizontalAlignmentOptions alignment, bool stopOthers)
        {
            Text = text;
            Size = size / 100f;
            worldPosition.z = 0f;
            WorldPosition = worldPosition;
            Color = color;
            StayDuration = stayDuration;
            FadeDuration = fadeDuration;
            Speed = speed;
            Alignment = alignment;
            StopOthers = stopOthers;
        }
    }
}