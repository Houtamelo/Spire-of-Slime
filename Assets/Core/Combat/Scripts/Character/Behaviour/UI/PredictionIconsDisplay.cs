using System.Collections.Generic;
using Core.Combat.Scripts.Effects.BaseTypes;
using Core.Combat.Scripts.Skills.Action;
using Core.Utils.Collections;
using Core.Utils.Extensions;
using Core.Utils.Patterns;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Utils.Patterns;

namespace Core.Combat.Scripts.Behaviour.UI
{
    public class PredictionIconsDisplay : MonoBehaviour
    {
        [SerializeField, Required]
        private PredictionIconDatabase database;
        
        [SerializeField, Required]
        private Transform iconsParent;

        [SerializeField, Required]
        private Image iconPrefab;

        private readonly IndexableHashSet<Image> _spawnedIcons = new();
        private readonly HashSet<IconType> _reusableSet = new();

        public void SetPlan(Option<PlannedSkill> plan)
        {
            if (plan.IsNone)
            {
                _spawnedIcons.DoForEach(icon => icon.gameObject.SetActive(false));
                return;
            }
            
            _reusableSet.Clear();
            
            foreach (IBaseStatusScript effect in plan.Value.Skill.TargetEffects)
            {
                Option<IconType> iconType = effect.GetPredictionIconType();
                if (iconType.IsSome)
                    _reusableSet.Add(iconType.Value);
            }

            if (plan.Value.Skill.BaseDamageMultiplier.IsSome)
                _reusableSet.Add(IconType.Damage);

            for (int j = _spawnedIcons.Count; j < _reusableSet.Count; j++)
            {
                Image icon = iconPrefab.InstantiateWithFixedLocalScale(iconsParent);
                _spawnedIcons.Add(icon);
                icon.transform.SetSiblingIndex(j);
            }

            int i = 0;
            foreach (IconType iconType in _reusableSet)
            {
                Image icon = _spawnedIcons[i];
                icon.sprite = database.GetIcon(iconType);
                icon.gameObject.SetActive(true);
                i++;
            }

            for (; i < _spawnedIcons.Count; i++)
                _spawnedIcons[i].gameObject.SetActive(false);
        }

        public enum IconType
        {
            Damage = 0,
            Buff = 1,
            Debuff = 2,
            Lust = 3,
            Heal = 4,
            Poison = 5,
            Summon
        }
    }
}