using MEC;

using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace SlApi.Extensions
{
    public class TaskResult<T>
    {
        public T Result;

        public volatile bool IsSuccesfull;
        public volatile bool IsFinished;

        public volatile Exception Error;

        public float Wait()
            => Timing.WaitUntilTrue(() => IsFinished);
    }

    public static class TaskUtils
    {
        public static void RunTask(Task task, TaskResult<bool> result)
        {
            Timing.RunCoroutine(WaitForTaskCoroutine(task, result));
        }

        public static void RunTask<T>(ValueTask<T> task, TaskResult<T> result)
        {
            Timing.RunCoroutine(WaitForTaskCoroutine<T>(task.AsTask(), result));
        }

        public static void RunTask<T>(Task<T> task, TaskResult<T> result)
        {
            Timing.RunCoroutine(WaitForTaskCoroutine(task, result));
        }

        private static IEnumerator<float> WaitForTaskCoroutine(Task task, TaskResult<bool> result)
        {
            if (result == null)
                yield break;

            try
            {
                task.ContinueWith(x =>
                {
                    result.IsSuccesfull = !x.IsFaulted && !x.IsCanceled;
                    result.Error = x.Exception;
                    result.Result = true;
                    result.IsFinished = true;
                });
            }
            catch (Exception ex)
            {
                result.Error = ex;
                result.IsSuccesfull = false;
                result.Result = default;
            }

            yield return Timing.WaitUntilTrue(() => result.IsFinished);
        }

        private static IEnumerator<float> WaitForTaskCoroutine<T>(Task<T> task, TaskResult<T> result)
        {
            if (result == null)
                yield break;

            try
            {
                task.ContinueWith(x =>
                {
                    result.IsSuccesfull = !x.IsFaulted && !x.IsCanceled;
                    result.Error = x.Exception;
                    result.Result = x.Result;
                    result.IsFinished = true;
                });
            }
            catch (Exception ex)
            {
                result.Error = ex;
                result.IsSuccesfull = false;
                result.Result = default;
            }

            yield return Timing.WaitUntilTrue(() => result.IsFinished);
        }
    }
}