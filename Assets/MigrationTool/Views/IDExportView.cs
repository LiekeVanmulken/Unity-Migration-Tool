#if UNITY_EDITOR || UNITY_EDITOR_BETA
using migrationtool.windows;
using System.IO;
using migrationtool.controllers;
using Newtonsoft.Json;
using System.Collections.Generic;
using migrationtool.models;
using migrationtool.utility;
using UnityEngine;

namespace migrationtool.views
{
    public class IDExportView
    {
        private readonly IDController idController = new IDController();
        private readonly Constants constants = Constants.Instance;

        public void ExportCurrentClassData(string rootPath)
        {
            string idExportPath = rootPath +  constants.RelativeExportPath;

            List<ClassModel> IDs = idController.ExportClassData(rootPath);

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = constants.IndentJson
            };

            string jsonField = JsonConvert.SerializeObject(IDs, jsonSerializerSettings);

            if (!Directory.Exists(Path.GetDirectoryName(idExportPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(idExportPath));
            }

            File.WriteAllText(idExportPath, jsonField);
            string logMessage = "All IDs were exported to " + constants.RelativeExportPath + ".";
            Debug.Log(logMessage);
            if (Administration.Instance.ShowInfoPopups)
            {
                MigrationWindow.DisplayDialog("Export completed successfully", logMessage);
            }
        }
    }
}
#endif