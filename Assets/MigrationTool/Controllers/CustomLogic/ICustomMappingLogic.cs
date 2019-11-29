using migrationtool.models;
using YamlDotNet.RepresentationModel;

namespace migrationtool.controllers.customlogic
{
    /// <summary>
    /// Interface to get control over how the data is transformed
    /// To define that the tool needs to use your implementation, add it to the <see cref="Constants.CustomLogicMapping"/>
    /// </summary>
    public interface ICustomMappingLogic
    {
        /// <summary>
        /// Called when a script needs to have custom logic to transform data.
        /// The scene can be changed by changing the scene variable.
        /// </summary>
        /// <example>An example of this can be found in the <see cref="QuaternionCustomMappingLogic"/></example>
        /// <param name="scene">The actual latest changed version of the scene file to transform. This is the data you need to set to change the actual scene!</param>
        /// <param name="yamlDocument">The Yaml of the scene with the data in it</param>
        /// <param name="scriptMapping">The mapping that it has, will be null if nothing has changed</param>
        void CustomLogic(ref string[] scene, ref YamlDocument yamlDocument, ScriptMapping scriptMapping);
    }
}