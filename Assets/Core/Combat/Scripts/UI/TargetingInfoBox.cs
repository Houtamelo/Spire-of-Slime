using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using ListPool;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using Utils.Patterns;

namespace Core.Combat.Scripts.UI
{
    public class TargetingInfoBox : MonoBehaviour
    {
        private static readonly StringBuilder Builder = new();
        
        [SerializeField, Required, SceneObjectsOnly]
        private Camera uiCamera;

        [SerializeField, Required, SceneObjectsOnly]
        private RectTransform selfRect;

        [SerializeField]
        private Vector2 screenOffset;
        
        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text targetingHitChanceTmp,
                         targetingCriticalChanceTmp,
                         targetingDamageTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private TMP_Text effectsTmp;

        [SerializeField, Required, SceneObjectsOnly]
        private GameObject targetingHitChanceObject,
                           targetingCriticalChanceObject,
                           targetingDamageObject;
        
        [SerializeField, Required, SceneObjectsOnly]
        private GameObject effectsObject;

        private void FixedUpdate()
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            Vector2 desiredScreenPosition = mouseScreenPosition + screenOffset;
            Vector3 desiredWorldPosition = uiCamera.ScreenToWorldPoint(desiredScreenPosition);
            desiredWorldPosition.z = 0;
            selfRect.position = desiredWorldPosition;
        }

        public void UpdateInterface(ref SkillStruct skillStruct,
                                    Option<float> hitChance, ComparisonResult hitChanceComparison, 
                                    Option<float> criticalChance, ComparisonResult criticalChanceComparison,
                                    Option<(uint lowerDamage, uint upperDamage)> damage, ComparisonResult damageComparison)
        {
            ISkill skill = skillStruct.Skill;
            ref ValueListPool<TargetProperties> allProperties = ref skillStruct.TargetProperties;
            if (allProperties.Count == 0)
            {
                Debug.LogWarning($"No TargetProperties for {skill.DisplayName}");
                Hide();
                return;
            }

            CharacterStateMachine caster = skillStruct.Caster;
            ReadOnlyProperties targetProperties = allProperties[0].ToReadOnly();
            CharacterStateMachine target = targetProperties.Target;
            switch (hitChance.IsSome, hitChanceComparison)
            {
                case (false, _): targetingHitChanceObject.SetActive(false); break;
                case (true, ComparisonResult.Equals):
                    targetingHitChanceTmp.text = hitChance.Value.ToPercentageString();
                    targetingHitChanceObject.SetActive(true);
                    break;
                case (true, ComparisonResult.Bigger):
                    Builder.Clear();
                    Builder.Append(ColorReferences.BuffedRichText.start, hitChance.Value.ToPercentageString(), ColorReferences.BuffedRichText.end);
                    targetingHitChanceTmp.text = Builder.ToString();
                    targetingHitChanceObject.SetActive(true);
                    break;
                case (true, ComparisonResult.Smaller):
                    Builder.Clear();
                    Builder.Append(ColorReferences.DebuffedRichText.start, hitChance.Value.ToPercentageString(), ColorReferences.DebuffedRichText.end);
                    targetingHitChanceTmp.text = Builder.ToString();
                    targetingHitChanceObject.SetActive(true);
                    break;
                default:
                    targetingHitChanceTmp.text = hitChance.Value.ToPercentageString();
                    targetingHitChanceObject.SetActive(true);
                    break;
            }
            
            switch (criticalChance.IsSome, criticalChanceComparison)
            {
                case (false, _): targetingCriticalChanceObject.SetActive(false); break;
                case (true, ComparisonResult.Equals):
                    targetingCriticalChanceTmp.text = criticalChance.Value.ToPercentageString();
                    targetingCriticalChanceObject.SetActive(true);
                    break;
                case (true, ComparisonResult.Bigger):
                    Builder.Clear();
                    Builder.Append(ColorReferences.BuffedRichText.start, criticalChance.Value.ToPercentageString(), ColorReferences.BuffedRichText.end);
                    targetingCriticalChanceTmp.text = Builder.ToString();
                    targetingCriticalChanceObject.SetActive(true);
                    break;
                case (true, ComparisonResult.Smaller):
                    Builder.Clear();
                    Builder.Append(ColorReferences.DebuffedRichText.start, criticalChance.Value.ToPercentageString(), ColorReferences.DebuffedRichText.end);
                    targetingCriticalChanceTmp.text = Builder.ToString();
                    targetingCriticalChanceObject.SetActive(true);
                    break;
                default:
                    targetingCriticalChanceTmp.text = criticalChance.Value.ToPercentageString();
                    targetingCriticalChanceObject.SetActive(true);
                    break;
            }
            
            switch (damage.IsSome, damageComparison)
            {
                case (false, _): targetingDamageObject.SetActive(false); break;
                case (true, ComparisonResult.Equals):
                    targetingDamageTmp.text = damage.Value.ToDamageFormat();
                    targetingDamageObject.SetActive(true);
                    break;
                case (true, ComparisonResult.Bigger):
                    Builder.Clear();
                    Builder.Append(ColorReferences.BuffedRichText.start, damage.Value.ToDamageFormat(), ColorReferences.BuffedRichText.end);
                    targetingDamageTmp.text = Builder.ToString();
                    targetingDamageObject.SetActive(true);
                    break;
                case (true, ComparisonResult.Smaller):
                    Builder.Clear();
                    Builder.Append(ColorReferences.DebuffedRichText.start, damage.Value.ToDamageFormat(), ColorReferences.DebuffedRichText.end);
                    targetingDamageTmp.text = Builder.ToString();
                    targetingDamageObject.SetActive(true);
                    break;
                default:
                    targetingDamageTmp.text = damage.Value.ToDamageFormat();
                    targetingDamageObject.SetActive(true);
                    break;
            }

            Builder.Clear();
            if (skill.MultiTarget == false && target != skillStruct.FirstTarget) // likely redirected by Guard status
            {
                Builder.Append("<color=red>!Redirected to ", target.Script.CharacterName, "!</color>");
            }
            
            ref ValueListPool<IActualStatusScript> targetEffects = ref skillStruct.TargetEffects;
            foreach (IActualStatusScript statusScript in targetEffects)
            {
                StatusToApply record = statusScript.GetStatusToApply(caster, target, crit: false, skill);
                record.ProcessModifiers();
                Builder.AppendLine(record.GetCompactDescription());
            }
            
            ref ValueListPool<IActualStatusScript> casterEffects = ref skillStruct.CasterEffects;
            if (casterEffects.Count > 0)
            {
                Builder.AppendLine("<align=center>Self:</align>");
                foreach (IActualStatusScript statusScript in casterEffects)
                {
                    StatusToApply record = statusScript.GetStatusToApply(caster, caster, crit: false, skill);
                    record.ProcessModifiers();
                    Builder.AppendLine(record.GetCompactDescription());
                }
            }
            
            bool anyEffect = Builder.Length > 0;
            effectsObject.SetActive(anyEffect);
            if (anyEffect)
                effectsTmp.text = Builder.ToString();

            bool anySome = hitChance.IsSome || criticalChance.IsSome || damage.IsSome || anyEffect;
            gameObject.SetActive(anySome);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}