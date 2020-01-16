using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace migrationtool.utility
{
    public class TypeUtility
    {
        public static KeyValuePair<string, Type>[] GetAllTypesInAssembliesByNameLower()
        {
            List<Type> types = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    types.AddRange(assembly.GetTypes());
                }
                catch (Exception e)
                {
                    Debug.LogError("Could not read types by Name Lower for assembly: " + assembly.FullName + "\r\nException:\r\n" + e);
                }
            }
            return types.Select(type => new KeyValuePair<string, Type>(type.Name.ToLower(), type)).ToArray();
        }
        public static KeyValuePair<string, Type>[] GetAllTypesInAssembliesByFullName()
        {
            List<Type> types = new List<Type>();
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    types.AddRange(assembly.GetTypes());
                }
                catch (Exception e)
                {
                    Debug.LogError("Could not read types by fullname for assembly: " + assembly.FullName + "\r\nException:\r\n" + e);
                }
            }
            return types.Select(type => new KeyValuePair<string, Type>(type.FullName, type)).ToArray();
        }
    }
}