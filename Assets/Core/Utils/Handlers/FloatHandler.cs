using System;
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
        
        public static bool operator >(FloatHandler handler, float value) => handler.Value > value;
        public static bool operator <(FloatHandler handler, float value) => handler.Value < value;
        public static float operator +(FloatHandler handler) => handler.Value;
        public static float operator -(FloatHandler handler) => -handler.Value;
        public static float operator /(FloatHandler handler, FloatHandler handler1) => handler.Value / handler1.Value;
        public static float operator *(FloatHandler handler, FloatHandler handler1) => handler.Value * handler1.Value;
        public static float operator *(FloatHandler handler, float handler1) => handler.Value * handler1;
        public static float operator *(float handler, FloatHandler handler1) => handler * handler1.Value;
        public static bool operator <=(FloatHandler handler, float value) => handler.Value <= value;
        public static bool operator >=(FloatHandler handler, float value) => handler.Value >= value;

        public void Add(float value) { SetValue(Value + value); }
    }
}