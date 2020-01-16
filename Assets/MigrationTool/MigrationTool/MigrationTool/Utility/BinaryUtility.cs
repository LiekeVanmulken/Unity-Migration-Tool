#if UNITY_EDITOR || UNITY_EDITOR_BETA
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace migrationtool.utility
{
    public static class BinaryUtility
    {
        public static bool IsBinaryFile(string filePath)
        {
            IEnumerable<string> content = File.ReadLines(filePath);
            int upperLimit = 20;
            int counter = 0;
            foreach (string line in content)
            {
                counter++;
                if (counter > upperLimit)
                {
                    return false;
                }

                if (HasBinaryContent(line))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasBinaryContent(string content)
        {
            return content.Any(ch => char.IsControl(ch) && ch != '\r' && ch != '\n');
        }
    }
}
#endif