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
            this.IsIterable = isIterable;
            this.Name = name;
            if (iteration > constants.RECURSION_DEPTH
                || isStandardClass(type.FullName) || type.IsEnum )
            {
                this.Type = new ClassModel(type.FullName);
                return;
            }

            this.Type = new ClassModel(type, iteration);
        }
        private bool isStandardClass(string toCheck)
        {
            return toCheck == null || constants.StandardClassesRegex.Match(toCheck).Success;
        }
    }
}