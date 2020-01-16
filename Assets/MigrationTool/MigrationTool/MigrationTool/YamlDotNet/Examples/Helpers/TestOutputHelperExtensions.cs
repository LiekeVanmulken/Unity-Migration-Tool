#if UNITY_EDITOR || UNITY_EDITOR_BETA
namespace YamlDotNet.Samples.Helpers
{
    public static class TestOutputHelperExtensions
    {
        public static void WriteLine(this ITestOutputHelper output)
        {
            output.WriteLine(string.Empty);
        }
    }
}
#endif