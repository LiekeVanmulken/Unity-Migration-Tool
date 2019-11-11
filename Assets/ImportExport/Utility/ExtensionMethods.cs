using System;
using System.Collections.Generic;
using System.Linq;
using importerexporter.models;
using UnityEditor;
using YamlDotNet.RepresentationModel;

namespace importerexporter.utility
{
    public static class ExtensionMethods
    {
        public static string GetName(this YamlDocument document)
        {
            return (string) ((YamlMappingNode) document.RootNode).Children.First().Key;
        }

        public static IDictionary<YamlNode, YamlNode> GetChildren(this YamlNode node)
        {
            return ((YamlMappingNode) node).Children;
        }

        public static IList<YamlNode> GetItems(this YamlNode node)
        {
            return ((YamlSequenceNode) node).Children;
        }

        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0)
            {
                return text;
            }

            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }

        public static bool IsNullOrEmpty(this Array array)
        {
            return (array == null || array.Length == 0);
        }

        public static bool IsGenericList(this Type type)
        {
            return (type.IsGenericType && (type.GetGenericTypeDefinition() == typeof(List<>)));
        }

        /// <summary>
        /// Merge two lists of foundScripts to one using the oldClassModel.Name
        /// </summary>
        /// <param name="originalFoundScripts"></param>
        /// <param name="foundScriptsToMerge"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public static List<FoundScript> Merge(this List<FoundScript> originalFoundScripts,
            List<FoundScript> foundScriptsToMerge)
        {
            if (originalFoundScripts == null || foundScriptsToMerge == null)
            {
                throw new NullReferenceException("Could not merge foundScripts for null foundScript list");
            }

            // Merge the MergeWindow changed FoundScripts with the originalFoundScripts
            for (var i = 0; i < originalFoundScripts.Count; i++)
            {
                FoundScript originalFoundScript = originalFoundScripts[i];
                FoundScript changedFoundScript = foundScriptsToMerge.FirstOrDefault(script =>
                    script.oldClassModel.FullName == originalFoundScript.oldClassModel.FullName);
                if (changedFoundScript != null)
                {
                    originalFoundScripts[i] = changedFoundScript;
                }
            }

            foreach (FoundScript foundScript in foundScriptsToMerge)
            {
                if (!originalFoundScripts.Exists(script =>
                    script.oldClassModel.FullName == foundScript.oldClassModel.FullName))
                {
                    originalFoundScripts.Add(foundScript);
                }
            }

            return originalFoundScripts;
        }

        public static string[] PrepareSceneForYaml(this string[] scene)
        {
            for (var i = 0; i < scene.Length; i++)
            {
                string line = scene[i];
                if (line.StartsWith("---") && line.EndsWith("stripped"))
                {
                    scene[i] = line.Replace(" stripped","");
                }
            }

            return scene;
        }


    }
}