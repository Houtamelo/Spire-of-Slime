using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Core.Utils.Async
{
    public sealed class CoroutineWrapper : CustomYieldInstruction
    {
        /// Returns true if and only if the coroutine is running.  Paused tasks
        /// are considered to be running.
        public bool Running => _task.Running;

        /// Returns true if and only if the coroutine is currently paused.
        public bool Paused => _task.Paused;

        public delegate void StartedHandler(CoroutineWrapper reference);
        
        public event StartedHandler Started;
        
        public bool HasStarted { get; private set; }

        /// Delegate for termination subscribers.  manual is true if and only if
        /// the coroutine was stopped with an explicit call to Stop().
        public delegate void FinishedHandler(bool manual, CoroutineWrapper reference);

        /// Termination event.  Triggered when the coroutine completes execution.
        public event FinishedHandler Finished;

        public bool IsFinished { get; private set; }

        public readonly string RoutineName;
        public readonly Object Context;
        
        public readonly StackTrace StackTraceAtCreation;

        /// Creates a new Task object for the given coroutine.
        ///
        /// If autoStart is true (default) the task is automatically started
        /// upon construction.
        public CoroutineWrapper(IEnumerator c, string routineName, Object context = null, bool autoStart = true)
        {
            // save current stack trace for debugging purposes
            StackTraceAtCreation = new StackTrace(fNeedFileInfo: true);
            RoutineName = routineName;
            Context = context;
            _task = CoroutineWrapperManager.CreateTask(coroutine: c);
            _task.Finished += TaskFinished;
            if (autoStart)
                Start();
        }

        /// Begins execution of the coroutine
        public void Start()
        {
            if (HasStarted)
            {
                Console.WriteLine("CoroutineWrapper.Start() called on already started task");
                return;
            }
            
            HasStarted = true;
            _task.Start();
            Started?.Invoke(reference: this);
        }

        public void ForceFinish()
        {
            if (HasStarted == false)
            {
                HasStarted = true;
                Started?.Invoke(reference: this);
            }
            _task.ForceFinish();
        }

        public void StopEventless()
        {
            if (HasStarted == false)
            {
                HasStarted = true;
                Started?.Invoke(reference: this);
            }
            
            _task.Stop();
        }

        public void Pause()
        {
            _task.Pause();
        }

        public void UnPause()
        {
            _task.Unpause();
        }

        private void TaskFinished(bool manual)
        {
            if (HasStarted == false)
            {
                HasStarted = true;
                Started?.Invoke(reference: this);
            }
            
            IsFinished = true;
            FinishedHandler handler = Finished;
            handler?.Invoke(manual: manual, reference: this);
        }

#pragma warning disable IDE0044 // Add readonly modifier
        // ReSharper disable once FieldCanBeMadeReadOnly.Local
        private CoroutineWrapperManager.TaskState _task;
#pragma warning restore IDE0044 // Add readonly modifier
        public override bool keepWaiting => _task.Running;
    }
}