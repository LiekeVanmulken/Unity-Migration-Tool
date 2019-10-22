using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using importerexporter.models;
using UnityEngine;

namespace importerexporter.utility
{
    /// <summary>
    /// Generate all the fields on a class
    /// </summary>
    public static class FieldDataGenerationUtility
    {
        private static Constants constants = Constants.Instance;
        private static Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

        /// <summary>
        /// Gets all the fields on a class
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static FieldModel[] GenerateFieldData(string name)
        {
            Type type = assemblies.SelectMany(x => x.GetTypes())
                .FirstOrDefault(x => x.FullName == name);
            if (type == null)
            {
                Debug.LogError("Could not find class of name to search for members: " + name);
                return null;
            }

            return GenerateFieldData(type, 0);
        }

        /// <summary>
        /// Gets all the fields on a class
        /// </summary>
        /// <param name="type"></param>
        /// <param name="iteration">Times it has ran, used to recursively get the children</param>
        /// <returns></returns>
        public static FieldModel[] GenerateFieldData(Type type, int iteration)
        {
            if (type.Name.ToLower().Contains("testsubclas"))
            {
                Debug.Log("test2");
            }

            Match match = constants.IsListOrArrayRegex.Match(type.FullName);
            if (match.Success)
            {
                type = assemblies.SelectMany(x => x.GetTypes())
                    .FirstOrDefault(x => x.FullName == match.Value);
                if (type == null)
                {
                    throw new NullReferenceException("Type of list or array could not be found : " + match.Value);
                }
            }

            iteration++;
            List<FieldModel> values = new List<FieldModel>();

            FieldInfo[] publicFields =
                type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static |
                               BindingFlags.FlattenHierarchy);
            FieldInfo[] privateSerializedFields = type
                .GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static |
                           BindingFlags.FlattenHierarchy)
                .Where(info => Attribute.IsDefined(info, typeof(SerializeField))).ToArray();

            List<FieldInfo> members = new List<FieldInfo>();
            members.AddRange(publicFields);
            members.AddRange(privateSerializedFields);

            for (var i = 0; i < members.Count; i++)
            {
                
                FieldInfo member = members[i];
                Type currentType = member.FieldType;
                
                bool isIterable = false;
                
                if (currentType.IsArray)
                {
                    isIterable = true;
                    currentType = currentType.GetElementType();
                }

                if (currentType.IsGenericList())
                {
                    isIterable = true;
                    currentType = currentType.GetGenericArguments()[0];
                }

                values.Add(new FieldModel(currentType.FullName, currentType, isIterable, iteration));
            }

            return values.ToArray();
        }
    }
}