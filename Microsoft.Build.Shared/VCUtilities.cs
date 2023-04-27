using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace Microsoft.Build.Shared
{
    internal static class VCUtilities
    {
        internal static string GetHashString(string content)
        {
            using SHA256 sHA = new SHA256CryptoServiceProvider();
            byte[] array = sHA.ComputeHash(Encoding.UTF8.GetBytes(content)).Take(16).ToArray();
            char[] array2 = new char[16];
            for (int i = 0; i < array2.Length; i++)
            {
                array2[i] = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[(int)array[i] % "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".Length];
            }
            return new string(array2);
        }
    }
}
