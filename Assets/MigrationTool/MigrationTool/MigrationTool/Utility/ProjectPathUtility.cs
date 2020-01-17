#if UNITY_EDITOR || UNITY_EDITOR_BETA
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace migrationtool.utility
{
    public class ProjectPathUtility
    {
        /// <summary>
        /// Gets the topmost Unity project folder from a path 
        /// </summary>
        /// <param name="sceneLocation"></param>
        /// <returns></returns>
        public static string getProjectPathFromFile(string sceneLocation)
        {
            string previousMatches = "";
            Regex regex = new Regex(".*?Assets");
            MatchCollection matches = regex.Matches(sceneLocation);

            string[] matchedStrings = new string[matches.Count];

            for (var i = 0; i < matches.Count; i++)
            {
                Match match = matches[i];
                previousMatches += match;
                matchedStrings[i] = previousMatches;
            }

            matchedStrings = matchedStrings.Reverse().ToArray();
            foreach (string match in matchedStrings)
            {
                string path = Path.GetFullPath(Path.Combine(match, @"..\"));
                if (
                    Directory.Exists(path + @"\Library") &&
                    Directory.Exists(path + @"\Packages")
                )
                {
//                    Debug.Log(path);
                    return path.TrimEnd('\\');
                }
            }
            Debug.LogError("Could not parse scene to project location : " + sceneLocation);
            return null;
        }

        public static string AddTimestamp(string file)
        {
            DateTime now = DateTime.Now;

            string extension = null;
            //get the extension
            string[] fileParts = file.Split('.');
            if (file.EndsWith(".meta"))
            {
                extension = "." + fileParts[fileParts.Length - 2] + "." + fileParts[fileParts.Length - 1];
            }
            else
            {
                extension = "." + fileParts[fileParts.Length - 1];
            }

            //remove the extension
            file = file.Substring(0, file.Length - extension.Length);

            //Added the timestamp and re-added the extension
            file += "_changed_" + now.ToString("dd-MM-yy H_mm_ss") + extension;
            return file;
        }
    }
}
#endif
