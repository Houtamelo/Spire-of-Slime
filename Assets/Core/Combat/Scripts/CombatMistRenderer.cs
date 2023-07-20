using System;
using System.Collections;
using Core.Combat.Scripts.Skills.Action;
using Core.Save_Management;
using Core.Save_Management.SaveObjects;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using CombatManager = Core.Combat.Scripts.Managers.CombatManager;
using Save = Core.Save_Management.SaveObjects.Save;

namespace Core.Combat.Scripts
{
    public class CombatMistRenderer : MonoBehaviour
    {
        private const float FadeDurationBase = 30f;
        private static float FadeDuration => FadeDurationBase * IActionSequence.DurationMultiplier;
        
        [SerializeField, Required]
        private RawImage texture;
        
        [SerializeField]
        private float lowAlpha = 0.6f, mediumAlpha = 0.8f, highAlpha = 1f;

        private Tween _fadeTween;

        private void Awake()
        {
            Save.NemaExhaustionChanged += OnNemaExhaustionChanged;
        }

        private IEnumerator Start()
        {
            while (CombatManager.Instance.IsNone || CombatManager.Instance.Value.Running == false)
                yield return null;
            
            if (Save.AssertInstance(out Save save) == false)
                yield break;
            
            bool mistExists = CombatManager.Instance.Value.CombatSetupInfo.MistExists;
            NemaStatus status = save.GetFullNemaStatus();
            float alpha = status.IsInCombat.current ? GetAlpha(mistExists, status.SetToClearMist.current, status.IsStanding.current, status.GetEnum().current) : highAlpha;
            texture.color = texture.color.WithAlpha(alpha);
        }

        private void OnDisable()
        {
            _fadeTween.KillIfActive();
        }

        private void OnDestroy()
        {
            Save.NemaExhaustionChanged -= OnNemaExhaustionChanged;
        }

        private void OnNemaExhaustionChanged(NemaStatus status)
        {
            (bool previous, bool current) isInCombat = status.IsInCombat;
            if (isInCombat.current == false)
            {
                FadeAlpha(highAlpha, duration: 1f);
                return;
            }
            
            Option<CombatManager> combatManager = CombatManager.Instance;
            if (combatManager.IsNone || !combatManager.Value.Running)
                return;

            bool mistExists = combatManager.Value.CombatSetupInfo.MistExists;
            ExhaustionEnum exhaustion = status.GetEnum().current;
            bool clearingMist = status.SetToClearMist.current;
            bool isStanding = status.IsStanding.current;

            float alpha = GetAlpha(mistExists, clearingMist, isStanding, exhaustion);

            FadeAlpha(alpha, FadeDuration);
        }

        private float GetAlpha(bool mistExists, bool clearingMist, bool isStanding, ExhaustionEnum exhaustion)
        {
            float alpha;
            if (mistExists == false)
                alpha = 0f;
            else if (clearingMist == false || isStanding == false)
                alpha = highAlpha;
            else
                alpha = exhaustion switch
                {
                    ExhaustionEnum.None => 0f, 
                    ExhaustionEnum.Low => lowAlpha, 
                    ExhaustionEnum.Medium => mediumAlpha,
                    ExhaustionEnum.High => highAlpha, _ => throw new ArgumentOutOfRangeException(nameof(exhaustion), exhaustion, message: null)
                };
            
            return alpha;
        }

        private void FadeAlpha(float value, float duration)
        {
            _fadeTween.KillIfActive();
            _fadeTween = texture.DOFade(value, duration);
        }
    }
}