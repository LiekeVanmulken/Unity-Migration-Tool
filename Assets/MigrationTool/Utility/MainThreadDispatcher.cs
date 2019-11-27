using System;
using System.Collections.Generic;
using UnityEditor;


/// <summary>
/// Call actions on the MainThread from a different thread.
/// This is mainly used for user input and updating the UI from a calculation thread that will wait for the user input. 
/// </summary>
[InitializeOnLoad]
public class MainThreadDispatcher
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    static MainThreadDispatcher()
    {
        EditorApplication.update += Update;
    }

    static void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    public static void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}