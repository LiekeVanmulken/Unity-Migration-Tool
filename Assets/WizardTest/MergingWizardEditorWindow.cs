//using System.Collections.Generic;
//using System.Threading;
//using UnityEngine;
//using UnityEditor;
//
//public class MergingWizardEditorWindow : EditorWindow
//{
//    // Add menu named "My Window" to the Window menu
//    [MenuItem("WizardTest/WizardEditorWindow")]
//    static void Init()
//    {
//        // Get existing open window or if none, make a new one:
//        MergingWizardEditorWindow window = (MergingWizardEditorWindow)EditorWindow.GetWindow(typeof(MergingWizardEditorWindow));
//        window.Show();
//    }
//
//    void OnGUI()
//    {
//        if (GUILayout.Button("Spawn window"))
//        {
////            KeyValuePair<string, string>[] data = new[]
////            {
////                new KeyValuePair<string, string>("a", "b"),
////                new KeyValuePair<string, string>("c", "d"),
////                new KeyValuePair<string, string>("e", "f"),
////                new KeyValuePair<string, string>("g", "h"),
////                new KeyValuePair<string, string>("i", "j"),
////            };
////            
////            MergingWizard wizard = MergingWizard.CreateWizard(data);
//////            while (!wizard.done)
//////            {
//////                Thread.Sleep(100);
//////            }    
////            Debug.Log("Stopped sleeping");
//        }
//    }
//}