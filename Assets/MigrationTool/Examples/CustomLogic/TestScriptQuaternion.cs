#if UNITY_EDITOR || UNITY_EDITOR_BETA
using UnityEngine;


namespace migrationtool.examples.customlogic
{
    /// <summary>
    /// To test this, copy this file to the old project and add it to a scene.
    /// Change the type of the 'testQuaternion' to a Quaternion and export the ID's
    /// Now add the following in the constants.cs
    /// <example>
    ///
    ///  public readonly Dictionary<string, ICustomMappingLogic> CustomLogicMapping =
    ///  new Dictionary<string, ICustomMappingLogic>()
    ///  {
    ///      {typeof(TestScriptQuaternion).FullName, new QuaternionCustomMappingLogic()}
    ///  };
    /// 
    ///</example>
    ///Then you can export the quaternion to the vector3 by using custom logic.
    /// 
    /// </summary>
    public class TestScriptQuaternion : MonoBehaviour
    {
        public Vector3 testQuaternion;
    }
}
#endif