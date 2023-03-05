using MEC;

using PluginAPI.Core;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SlApi.Features.AsyncHelpers
{
    public static class TaskHelper {
        public static void RunAsAsyncUnsafe(Task task, Action continueWith = null) {
            Task.Run(async () => {
                try {
                    await task;
                }
                catch (Exception ex) {
                    Log.Error($"RunAsAsyncUnsafe: An error was thrown by the targeted task", "SL API::TaskHelper");
                    Log.Error(ex.ToString(), "SL API::TaskHelper");
                }

                try {
                    continueWith?.Invoke();
                }
                catch (Exception ex) {
                    Log.Error($"RunAsAsyncUnsage: An error was thrown by the targeted continue with method.", "SL API::TaskHelper");
                    Log.Error($"{ex}", "SL API::TaskHelper");
                }
            });
        }

        public static void RunAsAsyncUnsafe(ValueTask task, Action continueWith = null) {
            Task.Run(async () => {
                try {
                    await task;
                }
                catch (Exception ex) {
                    Log.Error($"RunAsAsyncUnsafe: An error was thrown by the targeted task", "SL API::TaskHelper");
                    Log.Error(ex.ToString(), "SL API::TaskHelper");
                }

                try {
                    continueWith?.Invoke();
                }
                catch (Exception ex) {
                    Log.Error($"RunAsAsyncUnsage: An error was thrown by the targeted continue with method.", "SL API::TaskHelper");
                    Log.Error($"{ex}", "SL API::TaskHelper");
                }
            });
        }

        public static void RunAsAsyncUnsafe<T>(Task<T> task, Action<T> continueWith = null) {
            Task.Run(async () => {
                T res = default;

                try {
                    res = await task;
                }
                catch (Exception ex) {
                    Log.Error($"RunAsAsyncUnsafe: An error was thrown by the targeted task", "SL API::TaskHelper");
                    Log.Error(ex.ToString(), "SL API::TaskHelper");

                    return;
                }

                try {
                    continueWith?.Invoke(res);
                }
                catch (Exception ex) {
                    Log.Error($"RunAsAsyncUnsage: An error was thrown by the targeted continue with method.", "SL API::TaskHelper");
                    Log.Error($"{ex}", "SL API::TaskHelper");
                }
            });
        }

        public static void RunAsAsyncUnsafe<T>(ValueTask<T> task, Action<T> continueWith = null) {
            Task.Run(async () => {
                T res = default;

                try {
                    res = await task;
                }
                catch (Exception ex) {
                    Log.Error($"RunAsAsyncUnsafe: An error was thrown by the targeted task", "SL API::TaskHelper");
                    Log.Error(ex.ToString(), "SL API::TaskHelper");

                    return;
                }

                try {
                    continueWith?.Invoke(res);
                }
                catch (Exception ex) {
                    Log.Error($"RunAsAsyncUnsage: An error was thrown by the targeted continue with method.", "SL API::TaskHelper");
                    Log.Error($"{ex}", "SL API::TaskHelper");
                }
            });
        }

        public static void RunAsAsyncSafe(Task task, Action continueWith = null) {
            Timing.RunCoroutine(SafeTask(task, continueWith));
        }

        public static void RunAsAsyncSafe(ValueTask task, Action continueWith = null) {
            Timing.RunCoroutine(SafeTask(task, continueWith));
        }

        public static void RunAsAsyncSafe<T>(Task<T> task, Action<T> continueWith = null) {
            Timing.RunCoroutine(SafeTask<T>(task, continueWith));
        }

        public static void RunAsAsyncSafe<T>(ValueTask<T> task, Action<T> continueWith = null) {
            Timing.RunCoroutine(SafeTask<T>(task, continueWith));
        }

        private static IEnumerator<float> SafeTask(Task task, Action continueWith) {
            try {
                if (task.Status != TaskStatus.Running)
                    task.Start();
            } catch { }

            while (!task.IsCompleted)
                yield return Timing.WaitForOneFrame;

            continueWith?.Invoke();
        }

        private static IEnumerator<float> SafeTask(ValueTask task, Action continueWith) {
            while (!task.IsCompleted)
                yield return Timing.WaitForOneFrame;

            continueWith?.Invoke();
        }

        private static IEnumerator<float> SafeTask<T>(Task<T> task, Action<T> continueWith) {
            try {
                if (task.Status != TaskStatus.Running)
                    task.Start();
            } catch { }

            while (!task.IsCompleted)
                yield return Timing.WaitForOneFrame;

            continueWith?.Invoke(task.Result);
        }

        private static IEnumerator<float> SafeTask<T>(ValueTask<T> task, Action<T> continueWith) {
            while (!task.IsCompleted)
                yield return Timing.WaitForOneFrame;

            continueWith?.Invoke(task.Result);
        }
    }
}