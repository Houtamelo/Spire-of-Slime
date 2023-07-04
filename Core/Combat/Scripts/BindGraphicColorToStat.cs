using System;
using Core.Combat.Scripts.Effects;
using Core.Combat.Scripts.Enums;
using Save_Management;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Core.Combat.Scripts
{
    [DisallowMultipleComponent]
    public class BindGraphicColorToStat : MonoBehaviour
    {
        [SerializeField, Required]
        private Graphic targetGraphic;

        [SerializeField]
        private StatType type;

        [SerializeField, ShowIf(nameof(ShowStat))]
        private CombatStat stat;

        [SerializeField, ShowIf(nameof(ShowEffect))]
        private EffectType effect;

        [SerializeField, ShowIf(nameof(ShowOthers))]
        private OtherStats others;

        [SerializeField]
        private bool overrideAlpha;

        private void Start()
        {
            UpdateColor();
        }

        private void Reset()
        {
            targetGraphic = GetComponent<Graphic>();
        }
        
        private void UpdateColor()
        {
            Color desiredColor = type switch
            {
                StatType.Stat   => stat.GetColor(),
                StatType.Effect => effect.GetColor(),
                StatType.Others => others.GetColor(),
                _               => throw new ArgumentOutOfRangeException($"Unexpected {nameof(StatType)}: {type}")
            };

            if (overrideAlpha == false)
                desiredColor.a = targetGraphic.color.a;

            targetGraphic.color = desiredColor;
        }
        
#if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
                if (targetGraphic == null)
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
        private bool ShowOthers => type == StatType.Others;

        private enum StatType
        {
            Stat,
            Effect,
            Others
        }
    }
}