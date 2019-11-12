using System;
using System.Collections.Generic;
using System.Linq;
using migrationtool.models;
using YamlDotNet.RepresentationModel;

namespace migrationtool.utility
{
    public static class ExtensionMethods
    {
        /// <summary>
        /// Get the name of the document. Can be used to check which type of document it is.
        /// E.g. if it's a MonoBehaviour or a BoxCollider or a Transform. This can be any type unity defines.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static string GetName(this YamlDocument document)
        {
            return (string) ((YamlMappingNode) document.RootNode).Children.First().Key;
        }

        /// <summary>
        /// Get the children of a yaml node
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static IDictionary<YamlNode, YamlNode> GetChildren(this YamlNode node)
        {
            return ((YamlMappingNode) node).Children;
        }

        /// <summary>
        /// Get items of a YamlSequenceNode
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static IList<YamlNode> GetItems(this YamlNode node)
        {
            return ((YamlSequenceNode) node).Children;
        }

        /// <summary>
        /// Replaces the first instance of the found script
        /// </summary>
        /// <param name="original"></param>
        /// <param name="search"></param>
        /// <param name="replace"></param>
        /// <returns></returns>
        public static string ReplaceFirst(this string original, string search, string replace)
        {
            int pos = original.IndexOf(search);
            if (pos < 0)
            {
                return original;
            }

            return original.Substring(0, pos) + replace + original.Substring(pos + search.Length);
        }

        /// <summary>
        /// Checks if a array is null or empty
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this Array array)
        {
            return (array == null || array.Length == 0);
        }

        /// <summary>
        /// Checks if a list is a generic list.
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
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

        /// <summary>
        /// When using a prefab in a prefab in a scene, unity will append " stripped" to one of the documents.
        /// This is not yaml compliant and will crash yaml.net. Because we don't need it,
        /// we just strip it for the yaml and leave it in the export. 
        /// </summary>
        /// <param name="scene"></param>
        /// <returns></returns>
        public static string[] PrepareSceneForYaml(this string[] scene)
        {
            string[] parsedScene = new string[scene.Length];
            Array.Copy(scene,parsedScene,scene.Length);

            for (var i = 0; i < parsedScene.Length; i++)
            {
                string line = parsedScene[i];
                if (line.StartsWith("---") && line.EndsWith("stripped"))
                {
                    parsedScene[i] = line.Replace(" stripped","");
                }
            }

            return parsedScene;
        }


    }
}