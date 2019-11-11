#if UNITY_EDITOR

using importerexporter.windows;
using System.IO;
using importerexporter.controllers;
using Newtonsoft.Json;
using System.Collections.Generic;
using importerexporter.models;

namespace importerexporter.views
{
    public class IDExportView
    {
        private readonly IDController idController = IDController.Instance;

        public void ExportCurrentClassData(string rootPath)
        {
            string idExportPath = rootPath + "/ImportExport/Exports/Export.json";

            List<ClassModel> IDs = idController.ExportClassData(rootPath);

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                Formatting = Formatting.Indented
            };

            string jsonField = JsonConvert.SerializeObject(IDs, jsonSerializerSettings);

            if (!Directory.Exists(Path.GetDirectoryName(idExportPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(idExportPath));
            }

            File.WriteAllText(idExportPath, jsonField);

            MigrationWindow.DisplayDialog("Export complete",
                "All classes were exported to " + idExportPath + " . Open up the new project and import the scene.");
        }
    }
}
#endif