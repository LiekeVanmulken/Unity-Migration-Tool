//using System;
//
//namespace importerexporter.controllers.customlogic
//{
//    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
//    public class CustomMappingLogicAttribute : Attribute
//    {
//        public Type type;
//
//        public CustomMappingLogicAttribute(Type type)
//        {
//            if (!typeof(ICustomMappingLogic).IsAssignableFrom(type))
//            {
//                throw new ArgumentException(
//                    "Cannot use type that does not inherit from ICustomMappingLogic, current type : " + type.FullName);
//            }
//
//            this.type = type;
//        }
//    }
//}