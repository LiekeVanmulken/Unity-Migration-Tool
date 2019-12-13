#if UNITY_EDITOR || UNITY_EDITOR 
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace migrationtool.utility
{
    public class ThreadUtility
    {
        private static Constants constants = Constants.Instance;

        /// <summary>
        /// Run a thread and wait for it to finish, it also includes error handling so it never becomes a loose thread
        /// </summary>
        /// <param name="mainLogic"></param>
        public static void RunWaitTask(Action mainLogic)
        {
            bool completed = false;
            new Task(() =>
                {
                    try
                    {
                        mainLogic();
                    }
                    catch (Exception e)
                    {
                        if (e is ThreadAbortException)
                        {
                            Debug.LogError(
                                "Migration failed because the thread was stopped mid processing. This might happen when Unity re-compiles. If it was not, please report the occurence. \r\nException: " +
                                e);
                        }
                        else
                        {
                            Debug.LogError(e);
                        }
                    }

                    completed = true;
                }
            ).Start();

            while (!completed)
            {
                Thread.Sleep(constants.THREAD_WAIT_TIME);
            }
        }

        /// <summary>
        /// Run a thread with error handling so it never becomes a loose thread
        /// </summary>
        /// <param name="mainLogic"></param>
        public static void RunTask(Action mainLogic)
        {
            new Task(() =>
                {
                    try
                    {
                        mainLogic();
                    }
                    catch (Exception e)
                    {
                        if (e is ThreadAbortException)
                        {
                            Debug.LogError(
                                "Migration failed because the thread was stopped mid processing. This might happen when Unity re-compiles. If it was not, please report the occurence. \r\nException: " +
                                e);
                        }
                        else
                        {
                            Debug.LogError(e);
                        }
                    }
                }
            ).Start();
        }

        /// <summary>
        /// Runs code on the main thread and wait for it.
        /// </summary>
        /// <param name="mainLogic"></param>
        public static void RunWaitMainTask(Action mainLogic)
        {
            bool completed = false;
            MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    mainLogic();
                }
                catch (Exception e)
                {
                    if (e is ThreadAbortException)
                    {
                        Debug.LogError(
                            "Migration failed because the thread was stopped mid processing. This might happen when Unity re-compiles. If it was not, please report the occurence. \r\nException: " +
                            e);
                    }
                    else
                    {
                        Debug.LogError(e);
                    }
                }

                completed = true;
            });
            while (!completed)
            {
                Thread.Sleep(constants.THREAD_WAIT_TIME);
            }
        }

        /// <summary>
        /// Runs on the main thread.
        /// </summary>
        /// <param name="mainLogic"></param>
        public static void RunMainTask(Action mainLogic)
        {
            MainThreadDispatcher.Enqueue(() =>
            {
                try
                {
                    mainLogic();
                }
                catch (Exception e)
                {
                    if (e is ThreadAbortException)
                    {
                        Debug.LogError(
                            "Migration failed because the thread was stopped mid processing. This might happen when Unity re-compiles. If it was not, please report the occurence. \r\nException: " +
                            e);
                    }
                    else
                    {
                        Debug.LogError(e);
                    }
                }
            });
        }
    }
}
#endif