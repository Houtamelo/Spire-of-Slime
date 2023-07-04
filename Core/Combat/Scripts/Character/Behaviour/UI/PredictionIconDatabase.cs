using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Core.Combat.Scripts.Behaviour.UI
{
    public class PredictionIconDatabase : SerializedScriptableObject
    {
        [OdinSerialize, Required]
        private Dictionary<PredictionIconsDisplay.IconType, Sprite> _icons = new();
        
        public Sprite GetIcon(PredictionIconsDisplay.IconType iconType) => _icons[iconType];
    }
}