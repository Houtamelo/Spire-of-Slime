using System;
using System.Runtime.Serialization;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Combat.Scripts
{
    [Serializable, DataContract]
    public struct GeneralPaddingSettings
    {
        private const float DefaultMiddle = 1.5f;
        private const float DefaultInBetween = 2.125f;
        private const float DefaultEvenY = 1.2f;
        private const float DefaultOddY = 0.8f;
        public static readonly Vector3 OutsideBounds = new(0, -100f, 0);

        public static readonly GeneralPaddingSettings Default = new()
        {
            leftMiddle = DefaultMiddle,
            rightMiddle = DefaultMiddle,
            inBetween = DefaultInBetween,
            evenY = DefaultEvenY,
            oddY = DefaultOddY
        };
        
        [SerializeField, DataMember, Range(0f, 10f), LabelText("Left Middle")]
        private float leftMiddle;
        public float LeftMiddle => leftMiddle;

        [SerializeField, DataMember, Range(0f, 10f), LabelText("Right Middle")]
        private float rightMiddle;
        public float RightMiddle => rightMiddle;

        [SerializeField, DataMember, Range(0f, 10f), LabelText("In Between")]
        private float inBetween;
        public float InBetween => inBetween;
        
        [SerializeField, DataMember, Range(0f, 10f), LabelText("Even Y")]
        private float evenY;
        public float EvenY => evenY;
        
        [SerializeField, DataMember, Range(0f, 10f), LabelText("Odd Y")]
        private float oddY;
        public float OddY => oddY;
        
        public float GetY(int index) => index % 2 == 0 ? evenY : oddY;
        public float GetY(bool isEven) => isEven ? evenY : oddY;
        
        public float GetSplitX(bool isLeft) => isLeft ? leftMiddle : rightMiddle;
    }
}