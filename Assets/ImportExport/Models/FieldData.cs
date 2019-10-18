using System;
using importerexporter.utility;
using UnityEngine;

namespace importerexporter.models
{
    /// <summary>
    /// Data model for a field on a class
    /// </summary>
    [Serializable]
    public class FieldData
    {
        private readonly Constants constants = Constants.Instance;

        /// <summary>
        /// Name of the field on the class
        /// </summary>
        [SerializeField] public string Name;

        /// <summary>
        /// Type of the field on the class
        /// </summary>
        [SerializeField] public ClassData Type;

        public FieldData()
        {
        }

        public FieldData(string name, Type type, int iteration)
        {
            this.Name = name;
            if (iteration > constants.RECURSION_DEPTH
                || isStandardClass(type.FullName) || type.IsEnum)
            {
                this.Type = new ClassData();
                this.Type.FullName = type.FullName;
                return;
            }

            this.Type = new ClassData(type, iteration);
        }


        private bool isStandardClass(string toCheck)
        {
            return toCheck == null || constants.standardRegex.Match(toCheck).Success;
        }
    }
}