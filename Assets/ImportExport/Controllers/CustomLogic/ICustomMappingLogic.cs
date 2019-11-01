using importerexporter.models;
using YamlDotNet.RepresentationModel;

namespace importerexporter.controllers.customlogic
{
    public interface ICustomMappingLogic
    {
        /// <summary>
        /// Called when a script needs to have custom logic to transform data.
        /// 
        /// </summary>
        /// <example>An example of this can be found in the <see cref="QuaternionCustomMappingLogic"/></example>
        /// <param name="scene">The actual latest changed version of the scene file to transform</param>
        /// <param name="yamlDocument">The Yaml of the scene with the data in it</param>
        /// <param name="foundScript">The mapping that it has, will be null if nothing has changed</param>
        void CustomLogic(ref string[] scene, ref YamlDocument yamlDocument, FoundScript foundScript=null);
    }
}