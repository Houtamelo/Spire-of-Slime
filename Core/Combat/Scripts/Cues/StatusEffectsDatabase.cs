using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Core.Combat.Scripts.Effects;
using Main_Database;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Utils.Patterns;

namespace Core.Combat.Scripts.Cues
{
    public sealed class StatusEffectsDatabase : SerializedScriptableObject
    {
        private static DatabaseManager Instance => DatabaseManager.Instance;
        
        [OdinSerialize]
        private Dictionary<EffectType, CueData> StatusSprites { get; set; } = new();
        
        [Pure] 
        public static Option<(Sprite sprite, AudioClip clip)> GetStatusSpriteAndSfx(EffectType effectType) =>
            Instance.StatusEffectsDatabase.StatusSprites.TryGetValue(effectType, out CueData data) ? Option<(Sprite sprite, AudioClip clip)>.Some(data) : Option.None;

        [Pure] 
        public static Option<Sprite> GetStatusIcon(EffectType effectType) =>
            Instance.StatusEffectsDatabase.StatusSprites.TryGetValue(effectType, out CueData data) ? Option<Sprite>.Some(data.serializedIcon) : Option.None;

        [Serializable]
        private struct CueData
        {
            [SerializeField, LabelText("Icon"), LabelWidth(50f)]
            public Sprite serializedIcon;

            [SerializeField, LabelText("Audio"), LabelWidth(50f)]
            public AudioClip serializedClip;
            
            public void Deconstruct(out Sprite icon, out AudioClip clip)
            {
                icon = serializedIcon;
                clip = serializedClip;
            }
            
            public static implicit operator (Sprite icon, AudioClip clip)(CueData data)
            {
                return (data.serializedIcon, data.serializedClip);
            }
        }
    }
}