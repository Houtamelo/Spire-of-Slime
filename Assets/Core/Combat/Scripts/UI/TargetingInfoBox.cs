using System.Text;
using Core.Combat.Scripts.Behaviour;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills;
using Core.Combat.Scripts.Skills.Interfaces;
using Core.Localization.Scripts;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Math;
using Core.Utils.Patterns;
using ListPool;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Core.Combat.Scripts.UI
{
    public class TargetingInfoBox : MonoBehaviour
    {
        private static readonly StringBuilder Builder = new();
        private static readonly LocalizedText RedirectedTrans = new("combat_targeting_redirected");
        
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
                                    Option<int> hitChance, ComparisonResult hitChanceComparison, 
                                    Option<int> criticalChance, ComparisonResult criticalChanceComparison,
                                    Option<(int lowerDamage, int upperDamage)> damage, ComparisonResult damageComparison)
        {
            ISkill skill = skillStruct.Skill;
            ref CustomValuePooledList<TargetProperties> allProperties = ref skillStruct.TargetProperties;
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
                case (IsSome: true, ComparisonResult.Equals):
                    targetingHitChanceTmp.text = hitChance.Value.ToString();
                    targetingHitChanceObject.SetActive(true); break;
                case (IsSome: true, ComparisonResult.Bigger):
                    targetingHitChanceTmp.text = Builder.Override(hitChance.Value.ToString()).Surround(ColorReferences.BuffedRichText).ToString();
                    targetingHitChanceObject.SetActive(true); break;
                case (IsSome: true, ComparisonResult.Smaller):
                    targetingHitChanceTmp.text = Builder.Override(hitChance.Value.ToString()).Surround(ColorReferences.DebuffedRichText).ToString();
                    targetingHitChanceObject.SetActive(true); break;
                case (IsSome: false, _): 
                    targetingHitChanceObject.SetActive(false); break;
                default:
                    targetingHitChanceTmp.text = hitChance.Value.ToString();
                    targetingHitChanceObject.SetActive(true); break;
            }
            
            switch (criticalChance.IsSome, criticalChanceComparison)
            {
                case (IsSome: true, ComparisonResult.Equals):
                    targetingCriticalChanceTmp.text = criticalChance.Value.ToString();
                    targetingCriticalChanceObject.SetActive(true); break;
                case (IsSome: true, ComparisonResult.Bigger):
                    targetingCriticalChanceTmp.text = Builder.Override(criticalChance.Value.ToString()).Surround(ColorReferences.BuffedRichText).ToString();
                    targetingCriticalChanceObject.SetActive(true); break;
                case (IsSome: true, ComparisonResult.Smaller):
                    targetingCriticalChanceTmp.text = Builder.Override(criticalChance.Value.ToString()).Surround(ColorReferences.DebuffedRichText).ToString();
                    targetingCriticalChanceObject.SetActive(true); break;
                case (IsSome: false, _):
                    targetingCriticalChanceObject.SetActive(false); break;
                default:
                    targetingCriticalChanceTmp.text = criticalChance.Value.ToString();
                    targetingCriticalChanceObject.SetActive(true); break;
            }
            
            switch (damage.IsSome, damageComparison)
            {
                case (IsSome: true, ComparisonResult.Equals):
                    targetingDamageTmp.text = damage.Value.ToDamageRangeFormat();
                    targetingDamageObject.SetActive(true); break;
                case (IsSome: true, ComparisonResult.Bigger):
                    targetingDamageTmp.text = Builder.Override(damage.Value.ToDamageRangeFormat()).Surround(ColorReferences.BuffedRichText).ToString();
                    targetingDamageObject.SetActive(true); break;
                case (IsSome: true, ComparisonResult.Smaller):
                    targetingDamageTmp.text = Builder.Override(damage.Value.ToDamageRangeFormat()).Surround(ColorReferences.DebuffedRichText).ToString();
                    targetingDamageObject.SetActive(true);
                    break;
                case (IsSome: false, _): 
                    targetingDamageObject.SetActive(false); break;
                default:
                    targetingDamageTmp.text = damage.Value.ToDamageRangeFormat();
                    targetingDamageObject.SetActive(true); break;
            }

            Builder.Clear();
            if (skill.MultiTarget == false && target != skillStruct.FirstTarget) // likely redirected by Guard status
                Builder.Append("<color=red>", RedirectedTrans.Translate().GetText(target.Script.CharacterName.Translate().GetText()), "</color>");

            ref CustomValuePooledList<IActualStatusScript> targetEffects = ref skillStruct.TargetEffects;
            foreach (IActualStatusScript statusScript in targetEffects)
            {
                StatusToApply record = statusScript.GetStatusToApply(caster, target, crit: false, skill);
                record.ProcessModifiers();
                Builder.AppendLine(record.GetCompactDescription());
            }
            
            ref CustomValuePooledList<IActualStatusScript> casterEffects = ref skillStruct.CasterEffects;
            if (casterEffects.Count > 0)
            {
                Builder.AppendLine("<align=center>", SkillUtils.SelfTrans.Translate().GetText(), "</align>");
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