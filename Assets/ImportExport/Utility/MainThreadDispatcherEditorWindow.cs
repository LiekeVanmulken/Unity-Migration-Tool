using System;
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// EditorWindow that can run actions on the MainThread for user input 
/// </summary>
public class MainThreadDispatcherEditorWindow : EditorWindow
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();

    public MainThreadDispatcherEditorWindow()
    {
        _instance = this;
    }

    public void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    private static MainThreadDispatcherEditorWindow _instance = null;

    public static bool Exists()
    {
        return _instance != null;
    }

    public void Enqueue(Action action)
    {
        _executionQueue.Enqueue(action);
    }

    public static MainThreadDispatcherEditorWindow Instance()
    {
        return _instance;
    }
}