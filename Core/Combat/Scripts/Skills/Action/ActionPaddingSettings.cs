using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Scripts.Skills.Action
{
    [Serializable]
    public struct ActionPaddingSettings
    {
        [SerializeField, Range(0f, 10f), LabelText("Ally Middle")]
        private float allyMiddlePadding;
        public float AllyMiddlePadding => allyMiddlePadding;

        [SerializeField, Range(0f, 10f), LabelText("Enemy Middle")]
        private float enemyMiddlePadding;
        public float EnemyMiddlePadding => enemyMiddlePadding;

        [SerializeField, Range(0f, 10f), LabelText("In Between")]
        private float inBetweenPadding;
        public float InBetweenPadding => inBetweenPadding;
        
        public ReadOnlyPaddingSettings AsReadOnly() => new(this);
        
        public static ActionPaddingSettings Default() => new() { 
            allyMiddlePadding = IActionSequence.DefaultMiddlePadding, 
            enemyMiddlePadding = IActionSequence.DefaultMiddlePadding, 
            inBetweenPadding = IActionSequence.DefaultInBetweenPadding };
    }
}