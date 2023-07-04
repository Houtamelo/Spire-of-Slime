using System;
using UnityEngine;

namespace Utils.Handlers
{
    [Serializable]
    public class IntHandler
    {
        [field: SerializeField] public int Value { get; private set; }
		public event Action<int> Event;

        public void SetValue(int value)
        {
            Value = value;
			Event?.Invoke(value);
        }
        
        public static bool operator >(IntHandler handler, int value) => handler.Value > value;
        public static bool operator <(IntHandler handler, int value) => handler.Value < value;
        public static int operator +(IntHandler handler) => handler.Value;
        public static int operator -(IntHandler handler) => -handler.Value;
        public static int operator /(IntHandler handler, IntHandler handler1) => handler.Value / handler1.Value;
        public static int operator *(IntHandler handler, IntHandler handler1) => handler.Value * handler1.Value;
        public static bool operator <=(IntHandler handler, int value) => handler.Value <= value;
        public static bool operator >=(IntHandler handler, int value) => handler.Value >= value;
    }
}