using System;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Utils.Handlers
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
        
        public static bool operator >([NotNull] IntHandler handler, int value) => handler.Value > value;
        public static bool operator <([NotNull] IntHandler handler, int value) => handler.Value < value;
        public static int operator +([NotNull] IntHandler handler) => handler.Value;
        public static int operator -([NotNull] IntHandler handler) => -handler.Value;
        public static int operator /([NotNull] IntHandler handler, [NotNull] IntHandler handler1) => handler.Value / handler1.Value;
        public static int operator *([NotNull] IntHandler handler, [NotNull] IntHandler handler1) => handler.Value * handler1.Value;
        public static bool operator <=([NotNull] IntHandler handler, int value) => handler.Value <= value;
        public static bool operator >=([NotNull] IntHandler handler, int value) => handler.Value >= value;
    }
}