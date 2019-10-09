using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using importerexporter.windows;
using UnityEditor;
using UnityEngine;

public class ThreadTestWindow : EditorWindow
{
    // Add menu named "My Window" to the Window menu
    [MenuItem("Window/ThreadTestWindow")]
    static void Init()
    {
        // Get existing open window or if none, make a new one:
        ThreadTestWindow window = (ThreadTestWindow) EditorWindow.GetWindow(typeof(ThreadTestWindow));
        window.Show();
    }

    private bool isRunning;

    void OnGUI()
    {
        if (GUILayout.Button("Start Thread"))
        {
            isRunning = true;
            var thread = new Thread(() => test(() => OnComplete()));
            thread.Start();
        }

        GUILayout.Label(isRunning ? "Running" : "Stopped");
    }

    private void OnComplete()
    {
        isRunning = false;
    }

    private void test(Action OnComplete)
    {
        
        string result = null;
        OptionsWizard optionsWizard =
            OptionsWizard.CreateWizard("Class cannot be found, select which one to choose",
                "a", new string[]{"aa", "b", "c"});

        optionsWizard.onComplete += (sender, wizardResult) => { result = wizardResult; };

        while (!string.IsNullOrEmpty(result))
        {
            Thread.Sleep(100);
        }
        
//        Debug.Log("Checking of this will break");
//        Thread.Sleep(1000);
//        Debug.Log("Checking of this will break2");

        OnComplete();
    }
}