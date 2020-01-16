#if UNITY_EDITOR || UNITY_EDITOR_BETA
using System;
using System.Security.Cryptography;

namespace migrationtool.utility
{
    public static class FileIDUtil
    {
        public static int Compute(Type t)
        {
            string toBeHashed = "s\0\0\0" + t.Namespace + t.Name;

            using (HashAlgorithm hash = new MD4())
            {
                byte[] hashed = hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(toBeHashed));

                int result = 0;

                for (int i = 3; i >= 0; --i)
                {
                    result <<= 8;
                    result |= hashed[i];
                }
                
                return result;
            }
        }
    }
}
#endif