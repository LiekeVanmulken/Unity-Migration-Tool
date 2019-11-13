#if UNITY_EDITOR

using System.Linq;
using System;
using System.Reflection;
using System.Threading;
using migrationtool.windows;
using UnityEngine;

namespace migrationtool.utility
{
    public class ThreadTestWindow : MainThreadDispatcherEditorWindow
    {
        // Add menu named "My Window" to the Window menu
//        [MenuItem("Window/ThreadTestWindow")]
//        static void Init()
//        {
//            // Get existing open window or if none, make a new one:
//            ThreadTestWindow window = (ThreadTestWindow) EditorWindow.GetWindow(typeof(ThreadTestWindow));
//            window.Show();
//        }

        private bool isRunning;

        void OnGUI()
        {
            if (GUILayout.Button("AssetDatabaseTest"))
            {
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                Type[] types = executingAssembly.GetTypes();


                string[] typeStringArray =
                    types.Select(type => type.FullName + " : " + type.Namespace + "." + type.Name).ToArray();
                string joinedTypeString = string.Join("\n", typeStringArray);
                Debug.Log(joinedTypeString);


//                string dllGuid = null;
//                string[] assets = AssetDatabase.FindAssets("");
//                foreach (string assetGuid in assets)
//                {
//                    string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
//                    Object[] assemblyObjects =
//                        AssetDatabase.LoadAllAssetsAtPath(assetPath);
//                    for (int i = 0; i < assemblyObjects.Length; i++)
//                    {
//                        long dllFileId;
//
//                        if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(assemblyObjects[i], out dllGuid, out dllFileId))
//                        {
//                            Debug.Log(assemblyObjects[i].name + " " + dllFileId.ToString() + " " + dllGuid );
//                        }
//                    }   
//                }
            }

            GUILayout.Space(20);
            if (GUILayout.Button("Start Thread"))
            {
                isRunning = true;
                var thread = new Thread(() =>
                {
                    try
                    {
                        test(() => OnComplete());
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                });
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
            ThreadTestWindow.Instance().Enqueue(() =>
            {
                OptionsWizard optionsWizard =
                    OptionsWizard.CreateWizard("Class cannot be found, select which one to choose",
                        "a", new string[] {"aa", "b", "c"},
                        resultCompleted => { Debug.Log("Select called, result: " + resultCompleted); },
                        () => { Debug.Log("Ignore called"); });
            });

            while (string.IsNullOrEmpty(result))
            {
                Debug.Log("result : " + result);
                Thread.Sleep(1000);
            }

            Debug.Log("result : " + result);
            Debug.Log("Completed");
            OnComplete();
        }
    }
}
#endif