using MEC;

using System;

namespace SlApi.Features.ThreadingHelpers
{
    public class ThreadResult<T>
    {
        internal Action<ThreadResult<T>> _continueWith;

        public T Result { get; private set; }

        public bool IsError { get; private set; }
        public bool IsFinished { get; private set; }

        public Exception Exception { get; private set; }

        public event Action<ThreadResult<T>> OnFinished;

        public void Fail(Exception ex = null)
        {
            Result = default;
            Exception = ex;
            IsError = true;
            IsFinished = true;

            _continueWith?.Invoke(this);
        }

        public void Finish(T result)
        {
            Result = result;

            IsError = false;
            IsFinished = true;

            _continueWith?.Invoke(this);
        }

        public float DoWait()
            => Timing.WaitUntilTrue(() => IsFinished);
    }
}
