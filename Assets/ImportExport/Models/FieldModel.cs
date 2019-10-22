using System;
using System.Text.RegularExpressions;
using importerexporter.utility;
using UnityEngine;

namespace importerexporter.models
{
    /// <summary>
    /// Data model for a field on a class
    /// </summary>
    [Serializable]
    public class FieldModel
    {
        private readonly Constants constants = Constants.Instance;

        /// <summary>
        /// Name of the field on the class
        /// </summary>
        [SerializeField] public string Name;

        /// <summary>
        /// Type of the field on the class
        /// </summary>
        [SerializeField] public ClassModel Type;

        /// <summary>
        /// Says whether it is an array or a list
        /// </summary>
        [SerializeField] public bool IsIterable;

        public FieldModel()
        {
        }

        public FieldModel(string name, Type type, bool isIterable, int iteration)
        {
//            string fullname = initList(type.FullName);

            this.IsIterable = isIterable;
            this.Name = name;
            if (iteration > constants.RECURSION_DEPTH
                || isStandardClass(type.FullName) || type.IsEnum )
            {
                this.Type = new ClassModel(name);
                return;
            }

            this.Type = new ClassModel(type, iteration);
        }
//        /// <summary>
//        /// Checks if the class is a list or array
//        /// </summary>
//        /// <param name="fullName"></param>
//        /// <returns></returns>
//        private string initList(string fullName)
//        {
//            // Check if its a list or array and if so use the name of the class that it holds
//            Match match = constants.IsListOrArrayRegex.Match(fullName);
//            if (match.Success)
//            {
//                IsIterable = true;
//                fullName = match.Value;
//            }
//
//            return fullName;
//        }

        private bool isStandardClass(string toCheck)
        {
            return toCheck == null || constants.StandardClassesRegex.Match(toCheck).Success;
        }
    }
}