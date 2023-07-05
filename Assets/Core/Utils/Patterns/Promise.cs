using System;

namespace Core.Utils.Patterns
{
    public sealed class Promise<T>
    {
        public bool Done { get; private set; }
        private bool _success;
        private T _result;
        private event Action<T> Resolved;

        public Option<T> AsOption()
        {
            if (!Done)
                throw new InvalidOperationException("Promise not done");

            return _success ? Option<T>.Some(_result) : Option<T>.None;
        }

        public void Resolve(Option<T> option)
        {
            if (Done)
                throw new InvalidOperationException("Promise already done");
            
            Done = true;
            _success = option.IsSome;
            _result = option.Value;
            Resolved?.Invoke(_result);
        }

        public Promise<T> OnResolve(Action<T> action)
        {
            if (Done)
                action(_result);
            else
                Resolved += action;
            
            return this;
        }
    }
}