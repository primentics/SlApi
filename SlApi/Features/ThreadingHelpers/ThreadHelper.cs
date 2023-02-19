using System;
using System.Threading;
using System.Threading.Tasks;

namespace SlApi.Features.ThreadingHelpers
{
    public static class ThreadHelper
    {
        public static ThreadResult<T> RunThread<T>(Func<T> method, Action<ThreadResult<T>> continueWith = null) where T : class
        {
            var res = new ThreadResult<T>() { _continueWith = continueWith };
            var thread = new Thread(x =>
            {
                try
                {
                    var methodRes = method.Method.Invoke(method.Target, null) as T;

                    if (methodRes is null)
                        res.Fail(null);
                    else
                        res.Finish(methodRes);
                } 
                catch (Exception ex) 
                {
                    res.Fail(ex);
                }
            });

            thread.Start();

            return res;
        }

        public static ThreadResult<object> RunAsyncTask(Task task, Action<ThreadResult<object>> continueWith = null)
        {
            var res = new ThreadResult<object>() { _continueWith = continueWith };
            var thread = new Thread(async x =>
            {
                try
                {
                    await task;

                    res.Finish(null);
                }
                catch (Exception ex)
                {
                    res.Fail(ex);
                }
            });

            thread.Start();

            return res;
        }

        public static ThreadResult<T> RunAsyncTask<T>(Task<T> task, Action<ThreadResult<T>> continueWith = null) where T : class
        {
            var res = new ThreadResult<T>() { _continueWith = continueWith };
            var thread = new Thread(async x =>
            {
                try
                {
                    var methodRes = await task;

                    if (methodRes is null)
                        res.Fail(null);
                    else
                        res.Finish(methodRes);
                }
                catch (Exception ex)
                {
                    res.Fail(ex);
                }
            });

            thread.Start();

            return res;
        }
    }
}