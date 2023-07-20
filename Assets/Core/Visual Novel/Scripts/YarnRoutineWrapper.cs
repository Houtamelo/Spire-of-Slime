using System;
using System.Collections;
using Core.Utils.Async;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Visual_Novel.Scripts
{
    public class YarnRoutineWrapper
    {
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private IEnumerator _enumerator;
        private readonly CoroutineWrapper _coroutine;
        public bool Running => _coroutine is { Running: true };
        public bool Finished => Running == false;

        private YarnRoutineWrapper(IEnumerator enumerator, CoroutineWrapper coroutine)
        {
            _enumerator = enumerator;
            _coroutine = coroutine;
        }

        [NotNull]
        public static YarnRoutineWrapper FromEnumerator(IEnumerator enumerator, Action onFinish)
        {
            CoroutineWrapper coroutine = new(enumerator, "anonymous", context: null, autoStart: true);
            coroutine.Finished += (_, _) => onFinish?.Invoke();
            return new YarnRoutineWrapper(enumerator, coroutine);
        }

        public bool TryImmediateFinish()
        {
            if (Finished)
            {
                Debug.LogWarning("Trying to finish already finished routine");
                return false;
            }
            
            if (_enumerator is not YieldableCommandWrapper yieldableCommandWrapper)
                return false;

            if (yieldableCommandWrapper.TryImmediateFinish() == false)
                return false;
            
            _coroutine.ForceFinish();
            return true;
        }
    }
}