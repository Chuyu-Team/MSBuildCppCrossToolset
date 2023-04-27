using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.Build.Shared;
using Microsoft.Build.Shared.FileSystem;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.Shared
{

    internal static class FileUtilities
    {
        private static readonly IFileSystem DefaultFileSystem = FileSystems.Default;

        internal static readonly char[] Slashes = new char[2] { '/', '\\' };

        // Linux大小写敏感
        private static readonly ConcurrentDictionary<string, bool> FileExistenceCache = new ConcurrentDictionary<string, bool>(StringComparer.Ordinal);

        internal static bool IsSlash(char c)
        {
            if (c != Path.DirectorySeparatorChar)
            {
                return c == Path.AltDirectorySeparatorChar;
            }
            return true;
        }

        internal static string TrimTrailingSlashes(this string s)
        {
            return s.TrimEnd(Slashes);
        }

        internal static string FixFilePath(string path)
        {
            if (!string.IsNullOrEmpty(path) && Path.DirectorySeparatorChar != '\\')
            {
                return path.Replace('\\', '/');
            }
            return path;
        }

        /// <summary>
        /// If the given path doesn't have a trailing slash then add one.
        /// If the path is an empty string, does not modify it.
        /// </summary>
        /// <param name="fileSpec">The path to check.</param>
        /// <returns>A path with a slash.</returns>
        internal static string EnsureTrailingSlash(string fileSpec)
        {
            fileSpec = FixFilePath(fileSpec);
            if (fileSpec.Length > 0 && !IsSlash(fileSpec[fileSpec.Length - 1]))
            {
                fileSpec += Path.DirectorySeparatorChar;
            }

            return fileSpec;
        }

        internal static string NormalizePath(string path)
        {
            ErrorUtilities.VerifyThrowArgumentLength(path, "path");
            return FixFilePath(GetFullPath(path));
        }

        private static string GetFullPath(string path)
        {
#if __
            if (NativeMethods.IsWindows)
            {
                string fullPath = NativeMethods.GetFullPath(path);
                if (IsPathTooLong(fullPath))
                {
                    throw new PathTooLongException(ResourceUtilities.FormatString(AssemblyResources.GetString("Shared.PathTooLong"), path, NativeMethods.MaxPath));
                }
                Path.HasExtension(fullPath);
                if (!IsUNCPath(fullPath))
                {
                    return fullPath;
                }
                return Path.GetFullPath(fullPath);
            }
#endif
            return Path.GetFullPath(path);
        }

        internal static string EnsureNoTrailingSlash(string path)
        {
            path = FixFilePath(path);
            if (EndsWithSlash(path))
            {
                path = path.Substring(0, path.Length - 1);
            }
            return path;
        }

        internal static bool EndsWithSlash(string fileSpec)
        {
            if (fileSpec.Length <= 0)
            {
                return false;
            }
            return IsSlash(fileSpec[fileSpec.Length - 1]);
        }

        internal static string GetDirectoryNameOfFullPath(string fullPath)
        {
            if (fullPath != null)
            {
                int num = fullPath.Length;
                while (num > 0 && fullPath[--num] != Path.DirectorySeparatorChar && fullPath[num] != Path.AltDirectorySeparatorChar)
                {
                }
                return FixFilePath(fullPath.Substring(0, num));
            }
            return null;
        }

        internal static string AttemptToShortenPath(string path)
        {
#if __
            if (IsPathTooLong(path) || IsPathTooLongIfRooted(path))
            {
                path = GetFullPathNoThrow(path);
            }
#endif
            return FixFilePath(path);
        }

        internal static bool PathsEqual(string path1, string path2)
        {
            if (path1 == null && path2 == null)
            {
                return true;
            }
            if (path1 == null || path2 == null)
            {
                return false;
            }
            int num = path1.Length - 1;
            int num2 = path2.Length - 1;
            for (int num3 = num; num3 >= 0; num3--)
            {
                char c = path1[num3];
                if (c != '/' && c != '\\')
                {
                    break;
                }
                num--;
            }
            for (int num4 = num2; num4 >= 0; num4--)
            {
                char c2 = path2[num4];
                if (c2 != '/' && c2 != '\\')
                {
                    break;
                }
                num2--;
            }
            if (num != num2)
            {
                return false;
            }
            for (int i = 0; i <= num; i++)
            {
                uint num5 = path1[i];
                uint num6 = path2[i];
                if ((num5 | num6) > 127)
                {
                    return PathsEqualNonAscii(path1, path2, i, num - i + 1);
                }
                if (num5 - 97 <= 25)
                {
                    num5 -= 32;
                }
                if (num6 - 97 <= 25)
                {
                    num6 -= 32;
                }
                if (num5 == 92)
                {
                    num5 = 47u;
                }
                if (num6 == 92)
                {
                    num6 = 47u;
                }
                if (num5 != num6)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool PathsEqualNonAscii(string strA, string strB, int i, int length)
        {
            if (string.Compare(strA, i, strB, i, length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }
            string strA2 = strA.ToSlash();
            string strB2 = strB.ToSlash();
            if (string.Compare(strA2, i, strB2, i, length, StringComparison.OrdinalIgnoreCase) == 0)
            {
                return true;
            }
            return false;
        }

        internal static string ToSlash(this string s)
        {
            return s.Replace('\\', '/');
        }

        internal static bool FileExistsNoThrow(string fullPath, IFileSystem fileSystem = null)
        {
            fullPath = AttemptToShortenPath(fullPath);
            try
            {
                if (fileSystem == null)
                {
                    fileSystem = DefaultFileSystem;
                }
                return /*Traits.Instance.CacheFileExistence*/true ? FileExistenceCache.GetOrAdd(fullPath, (string fullPath) => fileSystem.FileExists(fullPath)) : fileSystem.FileExists(fullPath);
            }
            catch
            {
                return false;
            }
        }

        internal static StreamWriter OpenWrite(string path, bool append, Encoding encoding = null)
        {
            FileMode mode = (append ? FileMode.Append : FileMode.Create);
            Stream stream = new FileStream(path, mode, FileAccess.Write, FileShare.Read, 4096, FileOptions.SequentialScan);
            if (stream != null)
                FileExistenceCache[path] = true;

            if (encoding == null)
            {
                return new StreamWriter(stream);
            }
            return new StreamWriter(stream, encoding);
        }

        internal static bool IsAnySlash(char c)
        {
            if (c != '/')
            {
                return c == '\\';
            }
            return true;
        }

        internal static void ClearFileExistenceCache()
        {
            FileExistenceCache.Clear();
        }

        internal static void UpdateFileExistenceCache(string path)
        {
            bool value;
            FileExistenceCache.Remove(path, out value);
        }
    }
}
