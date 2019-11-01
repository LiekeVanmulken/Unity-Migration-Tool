using importerexporter.models;
using YamlDotNet.RepresentationModel;

namespace importerexporter.controllers.customlogic
{
    public interface ICustomMappingLogic
    {
        void CustomLogic(ref string[] scene, ref YamlDocument yamlDocument, FoundScript foundScript);
    }
}