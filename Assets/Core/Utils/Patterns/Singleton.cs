using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Core.Utils.Patterns
{
    public abstract class Singleton<T> : SerializedMonoBehaviour where T : SerializedMonoBehaviour
    {
        private static Option<T> _instance;

        public static Option<T> Instance => _instance;

        public static bool AssertInstance(out T instance)
        {
            if (Instance.IsSome)
            {
                instance = Instance.Value;
                return true;
            }
            
            Debug.LogError($"No instance of {typeof(T).Name} exists!");
            instance = null;
            return false;
        }

        protected virtual void Awake()
        {
            if (Instance.IsNone)
            {
                SerializedMonoBehaviour serialized = this;
                _instance = (T) serialized;
            }
            else if (Instance.Value != this)
                throw new Exception(message: $"Duplicate Singleton: {gameObject.name}");
        }
        
        protected virtual void OnDestroy()
        {
            if (Instance.IsSome && Instance.Value == this)
                _instance = Option<T>.None;
        }
    }
}