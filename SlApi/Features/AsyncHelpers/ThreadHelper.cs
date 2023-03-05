using System;
using System.Threading;

namespace SlApi.Features.AsyncHelpers {
    public static class ThreadHelper {
        public static void RunAsAsyncUnsafe(Action action, Action continueWith = null) {
            new Thread(() => {
                action?.Invoke();
                continueWith?.Invoke();
            }).Start();
        }

        public static void RunAsAsyncUnsafe<T>(Func<T> func, Action<T> continueWith = null) {
            new Thread(() => {
                var res = func.Invoke();
                continueWith?.Invoke(res);
            }).Start();
        }
    }
}