using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Shared.FileSystem
{
    internal interface IFileSystem
    {
        TextReader ReadFile(string path);

        Stream GetFileStream(string path, FileMode mode, FileAccess access, FileShare share);

        string ReadFileAllText(string path);

        byte[] ReadFileAllBytes(string path);

        IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        IEnumerable<string> EnumerateDirectories(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        FileAttributes GetAttributes(string path);

        DateTime GetLastWriteTimeUtc(string path);

        bool DirectoryExists(string path);

        bool FileExists(string path);

        bool FileOrDirectoryExists(string path);
    }


    internal static class FileSystems
    {
        public static IFileSystem Default = GetFileSystem();

        private static IFileSystem GetFileSystem()
        {
#if __
            // 不支持Windows，所以不考虑这个路径
            if (NativeMethods.IsWindows)
            {
                return MSBuildOnWindowsFileSystem.Singleton();
            }
#endif
            return ManagedFileSystem.Singleton();
        }
    }
}
