using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace importerexporter.utility
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
            int numberOfAssets = Regex.Matches(sceneLocation, "Assets").Count;
            if (numberOfAssets == 1)
            {
                return sceneLocation.Substring(0, sceneLocation.IndexOf("Assets", StringComparison.Ordinal) + 6);
            }

            if (numberOfAssets > 1)
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
                        Directory.Exists(path + @"\obj") &&
                        Directory.Exists(path + @"\Packages") &&
                        Directory.Exists(path + @"\Temp")
                    )
                    {
                        return match;
                    }
                }
            }

            Debug.LogError("Could not parse scene to project location : " + sceneLocation);
            return null;
        }
    }
}