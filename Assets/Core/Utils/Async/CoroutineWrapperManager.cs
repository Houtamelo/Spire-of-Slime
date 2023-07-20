using System.Collections;
using JetBrains.Annotations;
using UnityEngine;

namespace Core.Utils.Async
{
    public static class CoroutineWrapperManager
    {
        private static readonly EmptyMonoBehaviour Instance;

        static CoroutineWrapperManager()
        {
            Instance = new GameObject("CoroutineWrapperManager").AddComponent<EmptyMonoBehaviour>();
            Object.DontDestroyOnLoad(Instance);
            Instance.gameObject.SetActive(true);
        }
        
        public class TaskState
        {
            public bool Running { get; private set; }

            public bool Paused { get; private set; }

            public delegate void FinishedHandler(bool manual);

            public event FinishedHandler Finished;

#pragma warning disable IDE0044 // Add readonly modifier
            // ReSharper disable once FieldCanBeMadeReadOnly.Local
            private IEnumerator _coroutine;
#pragma warning restore IDE0044 // Add readonly modifier
            private bool _stopped;

            public TaskState(IEnumerator c) => _coroutine = c;

            public void Pause()
            {
                Paused = true;
            }

            public void Unpause()
            {
                Paused = false;
            }

            public void Start()
            {
                Running = true;
                Instance.StartCoroutine(CallWrapper());
            }

            public void Stop()
            {
                _stopped = true;
                Running = false;
                Paused = false;
            }

            private IEnumerator CallWrapper()
            {
                yield return null;

                IEnumerator e = _coroutine;
                while (Running)
                {
                    if (Paused)
                    {
                        yield return null;
                    }
                    else
                    {
                        if (e?.MoveNext() == true)
                            yield return e.Current;
                        else
                            Running = false;
                    }
                }

                FinishedHandler handler = Finished;
                handler?.Invoke(manual: _stopped);
            }

            public void ForceFinish()
            {
                if (Running == false)
                    return;
                
                Stop();
                FinishedHandler handler = Finished;
                handler?.Invoke(manual: _stopped);
            }
        }
        

        [NotNull]
        public static TaskState CreateTask(IEnumerator coroutine) => new(c: coroutine);
    }
}