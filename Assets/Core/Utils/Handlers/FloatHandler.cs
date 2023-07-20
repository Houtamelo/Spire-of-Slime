using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Utils.Handlers
{
    [Serializable]
    public class FloatHandler
    {
        [field: SerializeField]
        public float Value { get; private set; }
        
		public event Action<float> Event;

        public void SetValue(float value)
        {
            Value = value;
			Event?.Invoke(value);
        }
        
        public static bool operator >([NotNull] FloatHandler handler, float value) => handler.Value > value;
        public static bool operator <([NotNull] FloatHandler handler, float value) => handler.Value < value;
        public static float operator +([NotNull] FloatHandler handler) => handler.Value;
        public static float operator -([NotNull] FloatHandler handler) => -handler.Value;
        public static float operator /([NotNull] FloatHandler handler, [NotNull] FloatHandler handler1) => handler.Value / handler1.Value;
        public static float operator *([NotNull] FloatHandler handler, [NotNull] FloatHandler handler1) => handler.Value * handler1.Value;
        public static float operator *([NotNull] FloatHandler handler, float handler1) => handler.Value * handler1;
        public static float operator *(float handler, [NotNull] FloatHandler handler1) => handler * handler1.Value;
        public static bool operator <=([NotNull] FloatHandler handler, float value) => handler.Value <= value;
        public static bool operator >=([NotNull] FloatHandler handler, float value) => handler.Value >= value;

        public void Add(float value) { SetValue(Value + value); }
    }
}