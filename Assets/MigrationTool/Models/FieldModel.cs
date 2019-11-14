using System;
using migrationtool.utility;

namespace migrationtool.models
{
    /// <summary>
    /// Data model for a field on a class
    /// </summary>
    public class FieldModel
    {
        private readonly Constants constants = Constants.Instance;

        /// <summary>
        /// Name of the field on the class
        /// </summary>
        public string Name;

        /// <summary>
        /// Type of the field on the class
        /// </summary>
        public ClassModel Type;

        /// <summary>
        /// Says whether it is an array or a list
        /// </summary>
        public bool IsIterable;

        public FieldModel()
        {
        }

        public FieldModel(string name, Type type, bool isIterable, int iteration)
        {
            this.IsIterable = isIterable;
            this.Name = name;
            if (
                iteration > constants.RECURSION_DEPTH ||
                type.IsEnum ||
                isStandardClass(type.FullName)
            )
            {
                this.Type = new ClassModel(type.FullName);
                return;
            }

            this.Type = new ClassModel(type, iteration);
        }

        /// <summary>
        /// Checks whether this is an unity or system class, these will not check for child fields
        /// </summary>
        /// <param name="toCheck"></param>
        /// <returns></returns>
        private bool isStandardClass(string toCheck)
        {
//            return toCheck == null || constants.StandardClassesRegex.Match(toCheck).Success;
            return toCheck == null || toCheck.StartsWith("UnityEngine") || toCheck.StartsWith("System");
        }
    }
}