﻿using Core.Combat.Scripts.Behaviour.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils.Extensions;

namespace Core.Combat.Scripts.Managers
{
    public class TimelineIconsManager : MonoBehaviour
    {
        [SerializeField, Required, AssetsOnly]
        private TimelineIcon iconPrefab;
        
        [SerializeField, Required, SceneObjectsOnly]
        private Transform iconsParent;
        
        public TimelineIcon CreateIcon()
        {
            TimelineIcon icon = iconPrefab.InstantiateWithFixedLocalScaleAndAnchoredPosition(iconsParent);
            icon.AssignParent(iconsParent);
            return icon;
        }
    }
}