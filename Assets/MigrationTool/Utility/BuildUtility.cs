using System.Collections.Generic;
using System.IO;
using migrationtool.controllers;
using migrationtool.models;
using Newtonsoft.Json;

namespace migrationtool.utility
{
    /// <summary>
    /// Exposes the export  
    /// </summary>
    public class BuildUtility
    {
        /// <summary>
        /// Exposes the export from outside to call it from a build.
        /// </summary>
        /// <param name="exportPath"></param>
        public static void ExportClassData(string assetPath, string exportPath)
        {
            List<ClassModel> export = new IDController().ExportClassData(assetPath);
            string jsonExport = JsonConvert.SerializeObject(export, Formatting.Indented);

            if (!Directory.Exists(Path.GetDirectoryName(exportPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(exportPath));
            }

            File.WriteAllText(exportPath, jsonExport);
        }
    }
}