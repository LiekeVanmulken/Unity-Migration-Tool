#if UNITY_EDITOR || UNITY_EDITOR_BETA
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace migrationtool.utility
{
    /// <summary>
    /// Source : https://gist.github.com/voxsoftware/590c3c87d5e337106f0bdb80536c0c0c
    /// </summary>
    public class Unzipper
    {
        public static List<string> ParseUnityPackagesToFiles(MemoryStream _memoryStream)
        {
            
            GZipStream gzip = new GZipStream(_memoryStream, CompressionMode.Decompress);

            const int chunk = 4096;
             MemoryStream memStr = new MemoryStream();

            int read;
            var buffer = new byte[chunk];
            do
            {
                read = gzip.Read(buffer, 0, chunk);
                memStr.Write(buffer, 0, read);
            } while (read == chunk);

            memStr.Seek(0, SeekOrigin.Begin);
            List<string> files = ParseUnityPackagesToFiles((Stream)memStr);

            gzip.Close();
            memStr.Close();
            return files;
        }

        public static List<string> ParseUnityPackagesToFiles(string filename)
        {
            FileStream stream = File.OpenRead(filename);

            GZipStream gzip = new GZipStream(stream, CompressionMode.Decompress);

            const int chunk = 4096;
            MemoryStream memStr = new MemoryStream();

            int read;
            var buffer = new byte[chunk];
            do
            {
                read = gzip.Read(buffer, 0, chunk);
                memStr.Write(buffer, 0, read);
            } while (read == chunk);

            memStr.Seek(0, SeekOrigin.Begin);
            List<string> files = ParseUnityPackagesToFiles((Stream)memStr);

            stream.Close();
            gzip.Close();
            memStr.Close();
            return files;
        }

        public static List<string> ParseUnityPackagesToFiles(Stream stream)
        {
            List<string> files = new List<string>();

            var buffer = new byte[100];
            while (true)
            {
                stream.Read(buffer, 0, 100);
                var name = Encoding.ASCII.GetString(buffer).Trim('\0');
                if (name == null || name == "")
                {
                    break;
                }

                stream.Seek(24, SeekOrigin.Current);
                stream.Read(buffer, 0, 12);
                var size = Convert.ToInt64(Encoding.UTF8.GetString(buffer, 0, 12).Trim('\0').Trim(), 8);


                stream.Seek(376L, SeekOrigin.Current);


                if (!name.EndsWith("/"))
                {
                    byte[] buf = new byte[size];
                    stream.Read(buf, 0, buf.Length);
                    if (name.EndsWith("pathname"))
                    {
                        string converted = Encoding.UTF8.GetString(buf, 0, buf.Length);
                        files.Add(converted);
                    }
                }

                var pos = stream.Position;

                var offset = 512 - (pos % 512);
                if (offset == 512)
                {
                    offset = 0;
                }

                stream.Seek(offset, SeekOrigin.Current);
            }

            return files;
        }

    }
}
#endif