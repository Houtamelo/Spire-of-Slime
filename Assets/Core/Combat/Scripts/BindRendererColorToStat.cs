using System;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Enums;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Scripts
{
    [DisallowMultipleComponent]
    public class BindRendererColorToStat : MonoBehaviour
    {
        [SerializeField, Required]
        private SpriteRenderer targetRenderer;

        [SerializeField]
        private StatType type;

        [SerializeField, ShowIf(nameof(ShowStat))]
        private CombatStat stat;

        [SerializeField, ShowIf(nameof(ShowEffect))]
        private EffectType effect;

        [SerializeField]
        private bool overrideAlpha;

        private void Start()
        {
            UpdateColor();
        }

        private void Reset()
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }
        
        private void UpdateColor()
        {
            Color desiredColor = type switch
            {
                StatType.Stat   => stat.GetColor(),
                StatType.Effect => effect.GetColor(),
                _               => throw new ArgumentOutOfRangeException($"Unexpected {nameof(StatType)}: {type}")
            };

            if (overrideAlpha == false)
                desiredColor.a = targetRenderer.color.a;

            targetRenderer.color = desiredColor;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<SpriteRenderer>();
                if (targetRenderer == null)
                {
                    Debug.LogWarning($"No target graphic found on {name}", context: this);
                    return;
                }
                
                UnityEditor.EditorUtility.SetDirty(this);
            }

            UpdateColor();
        }
#endif        

        private bool ShowStat => type == StatType.Stat;
        private bool ShowEffect => type == StatType.Effect;

        private enum StatType
        {
            Stat,
            Effect
        }
    }
}