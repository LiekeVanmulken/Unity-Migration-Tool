using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using migrationtool.utility;
using migrationtool.windows;
using UnityEngine;

public class ThreadUtil
{
    private static Constants constants = Constants.Instance;

    /// <summary>
    /// Run a thread and wait for it to finish, it also includes error handling so it never becomes a loose thread
    /// </summary>
    /// <param name="mainLogic"></param>
    public static void RunWaitThread(Action mainLogic)
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
                        Debug.LogError("Migration failed because project was recompiled mid processing");
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
    public static void RunThread(Action mainLogic)
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
                        Debug.LogError("Migration failed because project was recompiled mid processing");
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
    public static void RunWaitMainThread(Action mainLogic)
    {
        bool completed = false;
        MigrationWindow.Instance().Enqueue(() =>
        {
            try
            {
                mainLogic();
            }
            catch (Exception e)
            {
                if (e is ThreadAbortException)
                {
                    Debug.LogError("Migration failed this might be because the project was recompiled mid processing. If it was not, please report the occurence. \r\nException: " + e);
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
    public static void RunMainThread(Action mainLogic)
    {
        MigrationWindow.Instance().Enqueue(() =>
        {
            try
            {
                mainLogic();
            }
            catch (Exception e)
            {
                if (e is ThreadAbortException)
                {
                    Debug.LogError("Migration failed because project was recompiled mid processing");
                }
                else
                {
                    Debug.LogError(e);
                }
            }
        });
    }
}