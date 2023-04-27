using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Build.Shared;
using Microsoft.Build.Shared.FileSystem;
using Microsoft.Build.Utilities;
using Microsoft.VisualBasic.FileIO;

namespace Microsoft.Build.Shared
{
    internal class FileMatcher
    {
        private class TaskOptions
        {
            public readonly int MaxTasks;

            public int AvailableTasks;

            public int MaxTasksPerIteration;

            public TaskOptions(int maxTasks)
            {
                MaxTasks = maxTasks;
            }
        }


        private struct RecursiveStepResult
        {
            public string RemainingWildcardDirectory;

            public bool ConsiderFiles;

            public bool NeedsToProcessEachFile;

            public string DirectoryPattern;

            public bool NeedsDirectoryRecursion;
        }

        private enum SearchAction
        {
            RunSearch,
            ReturnFileSpec,
            ReturnEmptyList
        }


        internal enum FileSystemEntity
        {
            Files,
            Directories,
            FilesAndDirectories
        }

        private class FilesSearchData
        {
            public string Filespec { get; }

            public string DirectoryPattern { get; }

            public Regex RegexFileMatch { get; }

            public bool NeedsRecursion { get; }

            public FilesSearchData(string filespec, string directoryPattern, Regex regexFileMatch, bool needsRecursion)
            {
                Filespec = filespec;
                DirectoryPattern = directoryPattern;
                RegexFileMatch = regexFileMatch;
                NeedsRecursion = needsRecursion;
            }
        }

        internal sealed class Result
        {
            internal bool isLegalFileSpec;

            internal bool isMatch;

            internal bool isFileSpecRecursive;

            internal string wildcardDirectoryPart = string.Empty;

            internal Result()
            {
            }
        }

        private struct RecursionState
        {
            public string BaseDirectory;

            public string RemainingWildcardDirectory;

            public bool IsInsideMatchingDirectory;

            public FilesSearchData SearchData;

            public bool IsLookingForMatchingDirectory
            {
                get
                {
                    if (SearchData.DirectoryPattern != null)
                    {
                        return !IsInsideMatchingDirectory;
                    }
                    return false;
                }
            }
        }

        private static readonly string s_directorySeparator = new string(Path.DirectorySeparatorChar, 1);

        private static readonly string s_thisDirectory = "." + s_directorySeparator;

        public static FileMatcher Default = new FileMatcher(FileSystems.Default);

        private static readonly char[] s_wildcardCharacters = new char[2] { '*', '?' };

        internal delegate IReadOnlyList<string> GetFileSystemEntries(FileSystemEntity entityType, string path, string pattern, string projectDirectory, bool stripProjectDirectory);

        private readonly ConcurrentDictionary<string, IReadOnlyList<string>> _cachedGlobExpansions;

        private readonly Lazy<ConcurrentDictionary<string, object>> _cachedGlobExpansionsLock = new Lazy<ConcurrentDictionary<string, object>>(() => new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase));

        private static readonly Lazy<ConcurrentDictionary<string, IReadOnlyList<string>>> s_cachedGlobExpansions = new Lazy<ConcurrentDictionary<string, IReadOnlyList<string>>>(() => new ConcurrentDictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase));

        private static readonly Lazy<ConcurrentDictionary<string, object>> s_cachedGlobExpansionsLock = new Lazy<ConcurrentDictionary<string, object>>(() => new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase));
        
        private readonly IFileSystem _fileSystem;

        private readonly GetFileSystemEntries _getFileSystemEntries;

        internal static readonly char[] directorySeparatorCharacters = FileUtilities.Slashes;

        private static readonly char[] s_invalidPathChars = Path.GetInvalidPathChars();

        public FileMatcher(IFileSystem fileSystem, ConcurrentDictionary<string, IReadOnlyList<string>> fileEntryExpansionCache = null)
        : this(fileSystem, (FileSystemEntity entityType, string path, string pattern, string projectDirectory, bool stripProjectDirectory) => GetAccessibleFileSystemEntries(fileSystem, entityType, path, pattern, projectDirectory, stripProjectDirectory).ToArray(), fileEntryExpansionCache)
        {
        }

        internal FileMatcher(IFileSystem fileSystem, GetFileSystemEntries getFileSystemEntries, ConcurrentDictionary<string, IReadOnlyList<string>> getFileSystemDirectoryEntriesCache = null)
        {
            if (/*Traits.Instance.MSBuildCacheFileEnumerations*/false)
            {
                _cachedGlobExpansions = s_cachedGlobExpansions.Value;
                _cachedGlobExpansionsLock = s_cachedGlobExpansionsLock;
            }
            else
            {
                _cachedGlobExpansions = getFileSystemDirectoryEntriesCache;
            }
            _fileSystem = fileSystem;
            _getFileSystemEntries = ((getFileSystemDirectoryEntriesCache == null) ? getFileSystemEntries : ((GetFileSystemEntries)delegate (FileSystemEntity type, string path, string pattern, string directory, bool stripProjectDirectory)
            {
#if __
                if (ChangeWaves.AreFeaturesEnabled(ChangeWaves.Wave16_10))
                {
                    string key = type switch
                    {
                        FileSystemEntity.Files => "F",
                        FileSystemEntity.Directories => "D",
                        FileSystemEntity.FilesAndDirectories => "A",
                        _ => throw new NotImplementedException(),
                    } + ";" + path;
                    IReadOnlyList<string> orAdd = getFileSystemDirectoryEntriesCache.GetOrAdd(key, (string s) => getFileSystemEntries(type, path, "*", directory, stripProjectDirectory: false));
                    IEnumerable<string> enumerable2;
                    if (pattern == null || IsAllFilesWildcard(pattern))
                    {
                        IEnumerable<string> enumerable = orAdd;
                        enumerable2 = enumerable;
                    }
                    else
                    {
                        enumerable2 = orAdd.Where((string o) => IsMatch(Path.GetFileName(o), pattern));
                    }
                    IEnumerable<string> enumerable3 = enumerable2;
                    if (!stripProjectDirectory)
                    {
                        return enumerable3.ToArray();
                    }
                    return RemoveProjectDirectory(enumerable3, directory).ToArray();
                }
#endif
                return (type == FileSystemEntity.Directories) ? getFileSystemDirectoryEntriesCache.GetOrAdd("D;" + path + ";" + (pattern ?? "*"), (string s) => getFileSystemEntries(type, path, pattern, directory, stripProjectDirectory).ToArray()) : getFileSystemEntries(type, path, pattern, directory, stripProjectDirectory);
            }));
        }

        internal static bool HasWildcards(string filespec)
        {
            return -1 != filespec.LastIndexOfAny(s_wildcardCharacters);
        }

        private static IReadOnlyList<string> GetAccessibleFileSystemEntries(IFileSystem fileSystem, FileSystemEntity entityType, string path, string pattern, string projectDirectory, bool stripProjectDirectory)
        {
            path = FileUtilities.FixFilePath(path);
            switch (entityType)
            {
                case FileSystemEntity.Files:
                    return GetAccessibleFiles(fileSystem, path, pattern, projectDirectory, stripProjectDirectory);
                case FileSystemEntity.Directories:
                    return GetAccessibleDirectories(fileSystem, path, pattern);
                case FileSystemEntity.FilesAndDirectories:
                    return GetAccessibleFilesAndDirectories(fileSystem, path, pattern);
                default:
                    ErrorUtilities.VerifyThrow(condition: false, "Unexpected filesystem entity type.");
                    return Array.Empty<string>();
            }
        }

        private static IReadOnlyList<string> GetAccessibleFilesAndDirectories(IFileSystem fileSystem, string path, string pattern)
        {
            if (fileSystem.DirectoryExists(path))
            {
                try
                {
                    return (ShouldEnforceMatching(pattern) ? (from o in fileSystem.EnumerateFileSystemEntries(path, pattern)
                                                              where IsMatch(Path.GetFileName(o), pattern)
                                                              select o) : fileSystem.EnumerateFileSystemEntries(path, pattern)).ToArray();
                }
                catch (UnauthorizedAccessException)
                {
                }
                catch (SecurityException)
                {
                }
            }
            return Array.Empty<string>();
        }

        private static bool ShouldEnforceMatching(string searchPattern)
        {
            if (searchPattern == null)
            {
                return false;
            }
            if (searchPattern.IndexOf("?.", StringComparison.Ordinal) == -1 && (Path.GetExtension(searchPattern).Length != 4 || searchPattern.IndexOf('*') == -1))
            {
                return searchPattern.EndsWith("?", StringComparison.Ordinal);
            }
            return true;
        }

        private static IReadOnlyList<string> GetAccessibleFiles(IFileSystem fileSystem, string path, string filespec, string projectDirectory, bool stripProjectDirectory)
        {
            try
            {
                string path2 = ((path.Length == 0) ? s_thisDirectory : path);
                IEnumerable<string> enumerable;
                if (filespec == null)
                {
                    enumerable = fileSystem.EnumerateFiles(path2);
                }
                else
                {
                    enumerable = fileSystem.EnumerateFiles(path2, filespec);
                    if (ShouldEnforceMatching(filespec))
                    {
                        enumerable = enumerable.Where((string o) => IsMatch(Path.GetFileName(o), filespec));
                    }
                }
                if (stripProjectDirectory)
                {
                    enumerable = RemoveProjectDirectory(enumerable, projectDirectory);
                }
                else if (!path.StartsWith(s_thisDirectory, StringComparison.Ordinal))
                {
                    enumerable = RemoveInitialDotSlash(enumerable);
                }
                return enumerable.ToArray();
            }
            catch (SecurityException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }

        private static IReadOnlyList<string> GetAccessibleDirectories(IFileSystem fileSystem, string path, string pattern)
        {
            try
            {
                IEnumerable<string> enumerable = null;
                if (pattern == null)
                {
                    enumerable = fileSystem.EnumerateDirectories((path.Length == 0) ? s_thisDirectory : path);
                }
                else
                {
                    enumerable = fileSystem.EnumerateDirectories((path.Length == 0) ? s_thisDirectory : path, pattern);
                    if (ShouldEnforceMatching(pattern))
                    {
                        enumerable = enumerable.Where((string o) => IsMatch(Path.GetFileName(o), pattern));
                    }
                }
                if (!path.StartsWith(s_thisDirectory, StringComparison.Ordinal))
                {
                    enumerable = RemoveInitialDotSlash(enumerable);
                }
                return enumerable.ToArray();
            }
            catch (SecurityException)
            {
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException)
            {
                return Array.Empty<string>();
            }
        }

        private static IEnumerable<string> RemoveInitialDotSlash(IEnumerable<string> paths)
        {
            foreach (string path in paths)
            {
                if (path.StartsWith(s_thisDirectory, StringComparison.Ordinal))
                {
                    yield return path.Substring(2);
                }
                else
                {
                    yield return path;
                }
            }
        }

        internal static bool IsDirectorySeparator(char c)
        {
            if (c != Path.DirectorySeparatorChar)
            {
                return c == Path.AltDirectorySeparatorChar;
            }
            return true;
        }

        internal static IEnumerable<string> RemoveProjectDirectory(IEnumerable<string> paths, string projectDirectory)
        {
            bool directoryLastCharIsSeparator = IsDirectorySeparator(projectDirectory[projectDirectory.Length - 1]);
            foreach (string path in paths)
            {
                if (path.StartsWith(projectDirectory, StringComparison.Ordinal))
                {
                    if (!directoryLastCharIsSeparator)
                    {
                        if (path.Length <= projectDirectory.Length || !IsDirectorySeparator(path[projectDirectory.Length]))
                        {
                            yield return path;
                        }
                        else
                        {
                            yield return path.Substring(projectDirectory.Length + 1);
                        }
                    }
                    else
                    {
                        yield return path.Substring(projectDirectory.Length);
                    }
                }
                else
                {
                    yield return path;
                }
            }
        }

        internal static bool IsMatch(string input, string pattern)
        {
            if (input == null)
            {
                throw new ArgumentNullException("input");
            }
            if (pattern == null)
            {
                throw new ArgumentNullException("pattern");
            }
            int num = pattern.Length;
            int num2 = input.Length;
            int num3 = -1;
            int num4 = -1;
            int i = 0;
            int num5 = 0;
            bool flag = false;
            while (num5 < num2)
            {
                if (i < num)
                {
                    if (pattern[i] == '*')
                    {
                        while (++i < num && pattern[i] == '*')
                        {
                        }
                        if (i >= num)
                        {
                            return true;
                        }
                        if (!flag)
                        {
                            int num6 = num2;
                            int num7 = num;
                            while (i < num7 && num6 > num5)
                            {
                                num7--;
                                num6--;
                                if (pattern[num7] == '*')
                                {
                                    break;
                                }
                                if (!CompareIgnoreCase(input[num6], pattern[num7], num7, num6) && pattern[num7] != '?')
                                {
                                    return false;
                                }
                                if (i == num7)
                                {
                                    return true;
                                }
                            }
                            num2 = num6 + 1;
                            num = num7 + 1;
                            flag = true;
                        }
                        if (pattern[i] != '?')
                        {
                            while (!CompareIgnoreCase(input[num5], pattern[i], num5, i))
                            {
                                if (++num5 >= num2)
                                {
                                    return false;
                                }
                            }
                        }
                        num3 = i;
                        num4 = num5;
                        continue;
                    }
                    if (CompareIgnoreCase(input[num5], pattern[i], num5, i) || pattern[i] == '?')
                    {
                        i++;
                        num5++;
                        continue;
                    }
                }
                if (num3 < 0)
                {
                    return false;
                }
                i = num3;
                num5 = num4++;
            }
            for (; i < num && pattern[i] == '*'; i++)
            {
            }
            return i >= num;
            bool CompareIgnoreCase(char inputChar, char patternChar, int iIndex, int pIndex)
            {
                char c = (char)(inputChar | 0x20u);
                if (c >= 'a' && c <= 'z')
                {
                    return c == (patternChar | 0x20);
                }
                if (inputChar < '\u0080' || patternChar < '\u0080')
                {
                    return inputChar == patternChar;
                }
                return string.Compare(input, iIndex, pattern, pIndex, 1, StringComparison.OrdinalIgnoreCase) == 0;
            }
        }

        private static string ComputeFileEnumerationCacheKey(string projectDirectoryUnescaped, string filespecUnescaped, List<string> excludes)
        {
            int num = 0;
            if (excludes != null)
            {
                foreach (string exclude in excludes)
                {
                    num += exclude.Length;
                }
            }
            using ReuseableStringBuilder reuseableStringBuilder = new ReuseableStringBuilder(projectDirectoryUnescaped.Length + filespecUnescaped.Length + num);
            bool flag = false;
            try
            {
                string text = Path.Combine(projectDirectoryUnescaped, filespecUnescaped);
                if (text.Equals(filespecUnescaped, StringComparison.Ordinal))
                {
                    reuseableStringBuilder.Append(filespecUnescaped);
                }
                else
                {
                    reuseableStringBuilder.Append("p");
                    reuseableStringBuilder.Append(text);
                }
            }
            catch (Exception e) when (ExceptionHandling.IsIoRelatedException(e))
            {
                flag = true;
            }
            if (flag)
            {
                reuseableStringBuilder.Append("e");
                reuseableStringBuilder.Append("p");
                reuseableStringBuilder.Append(projectDirectoryUnescaped);
                reuseableStringBuilder.Append(filespecUnescaped);
            }
            if (excludes != null)
            {
                foreach (string exclude2 in excludes)
                {
                    reuseableStringBuilder.Append(exclude2);
                }
            }
            return reuseableStringBuilder.ToString();
        }

        internal string[] GetFiles(string projectDirectoryUnescaped, string filespecUnescaped, List<string> excludeSpecsUnescaped = null)
        {
            if (!HasWildcards(filespecUnescaped))
            {
                return CreateArrayWithSingleItemIfNotExcluded(filespecUnescaped, excludeSpecsUnescaped);
            }
            if (_cachedGlobExpansions == null)
            {
                return GetFilesImplementation(projectDirectoryUnescaped, filespecUnescaped, excludeSpecsUnescaped);
            }
            string key = ComputeFileEnumerationCacheKey(projectDirectoryUnescaped, filespecUnescaped, excludeSpecsUnescaped);
            if (!_cachedGlobExpansions.TryGetValue(key, out var value))
            {
                lock (_cachedGlobExpansionsLock.Value.GetOrAdd(key, (string _) => new object()))
                {
                    if (!_cachedGlobExpansions.TryGetValue(key, out value))
                    {
                        value = _cachedGlobExpansions.GetOrAdd(key, (string _) => GetFilesImplementation(projectDirectoryUnescaped, filespecUnescaped, excludeSpecsUnescaped));
                    }
                }
            }
            return value.ToArray();
        }

        internal static bool RawFileSpecIsValid(string filespec)
        {
            if (-1 != filespec.IndexOfAny(s_invalidPathChars))
            {
                return false;
            }
            if (-1 != filespec.IndexOf("...", StringComparison.Ordinal))
            {
                return false;
            }
            int num = filespec.LastIndexOf(":", StringComparison.Ordinal);
            if (-1 != num && 1 != num)
            {
                return false;
            }
            return true;
        }

        private static void PreprocessFileSpecForSplitting(string filespec, out string fixedDirectoryPart, out string wildcardDirectoryPart, out string filenamePart)
        {
            filespec = FileUtilities.FixFilePath(filespec);
            int num = filespec.LastIndexOfAny(directorySeparatorCharacters);
            if (-1 == num)
            {
                fixedDirectoryPart = string.Empty;
                wildcardDirectoryPart = string.Empty;
                filenamePart = filespec;
                return;
            }
            int num2 = filespec.IndexOfAny(s_wildcardCharacters);
            if (-1 == num2 || num2 > num)
            {
                fixedDirectoryPart = filespec.Substring(0, num + 1);
                wildcardDirectoryPart = string.Empty;
                filenamePart = filespec.Substring(num + 1);
                return;
            }
            int num3 = filespec.Substring(0, num2).LastIndexOfAny(directorySeparatorCharacters);
            if (-1 == num3)
            {
                fixedDirectoryPart = string.Empty;
                wildcardDirectoryPart = filespec.Substring(0, num + 1);
                filenamePart = filespec.Substring(num + 1);
            }
            else
            {
                fixedDirectoryPart = filespec.Substring(0, num3 + 1);
                wildcardDirectoryPart = filespec.Substring(num3 + 1, num - num3);
                filenamePart = filespec.Substring(num + 1);
            }
        }

        internal string GetLongPathName(string path)
        {
            return GetLongPathName(path, _getFileSystemEntries);
        }

        internal static string GetLongPathName(string path, GetFileSystemEntries getFileSystemEntries)
        {
            return path;
        }
        
        internal void SplitFileSpec(string filespec, out string fixedDirectoryPart, out string wildcardDirectoryPart, out string filenamePart)
        {
            PreprocessFileSpecForSplitting(filespec, out fixedDirectoryPart, out wildcardDirectoryPart, out filenamePart);
            if ("**" == filenamePart)
            {
                wildcardDirectoryPart += "**";
                wildcardDirectoryPart += s_directorySeparator;
                filenamePart = "*.*";
            }
            fixedDirectoryPart = GetLongPathName(fixedDirectoryPart, _getFileSystemEntries);
        }

        private static bool HasDotDot(string str)
        {
            for (int i = 0; i < str.Length - 1; i++)
            {
                if (str[i] == '.' && str[i + 1] == '.')
                {
                    return true;
                }
            }
            return false;
        }

        private static bool HasMisplacedRecursiveOperator(string str)
        {
            for (int i = 0; i < str.Length - 1; i++)
            {
                bool num = str[i] == '*' && str[i + 1] == '*';
                bool flag = (i == 0 || FileUtilities.IsAnySlash(str[i - 1])) && i < str.Length - 2 && FileUtilities.IsAnySlash(str[i + 2]);
                if (num && !flag)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsLegalFileSpec(string wildcardDirectoryPart, string filenamePart)
        {
            if (!HasDotDot(wildcardDirectoryPart) && !HasMisplacedRecursiveOperator(wildcardDirectoryPart))
            {
                return !HasMisplacedRecursiveOperator(filenamePart);
            }
            return false;
        }

        internal delegate (string fixedDirectoryPart, string recursiveDirectoryPart, string fileNamePart) FixupParts(string fixedDirectoryPart, string recursiveDirectoryPart, string filenamePart);

        internal void GetFileSpecInfo(string filespec, out string fixedDirectoryPart, out string wildcardDirectoryPart, out string filenamePart, out bool needsRecursion, out bool isLegalFileSpec, FixupParts fixupParts = null)
        {
            needsRecursion = false;
            fixedDirectoryPart = string.Empty;
            wildcardDirectoryPart = string.Empty;
            filenamePart = string.Empty;
            if (!RawFileSpecIsValid(filespec))
            {
                isLegalFileSpec = false;
                return;
            }
            SplitFileSpec(filespec, out fixedDirectoryPart, out wildcardDirectoryPart, out filenamePart);
            if (fixupParts != null)
            {
                (fixedDirectoryPart, wildcardDirectoryPart, filenamePart) = fixupParts(fixedDirectoryPart, wildcardDirectoryPart, filenamePart);
            }
            isLegalFileSpec = IsLegalFileSpec(wildcardDirectoryPart, filenamePart);
            if (isLegalFileSpec)
            {
                needsRecursion = wildcardDirectoryPart.Length != 0;
            }
        }


        private SearchAction GetFileSearchData(string projectDirectoryUnescaped, string filespecUnescaped, out bool stripProjectDirectory, out RecursionState result)
        {
            stripProjectDirectory = false;
            result = default(RecursionState);
            GetFileSpecInfo(filespecUnescaped, out var fixedDirectoryPart, out var wildcardDirectoryPart, out var filenamePart, out var needsRecursion, out var isLegalFileSpec);
            if (!isLegalFileSpec)
            {
                return SearchAction.ReturnFileSpec;
            }
            string text = fixedDirectoryPart;
            if (projectDirectoryUnescaped != null)
            {
                if (fixedDirectoryPart != null)
                {
                    try
                    {
                        fixedDirectoryPart = Path.Combine(projectDirectoryUnescaped, fixedDirectoryPart);
                    }
                    catch (ArgumentException)
                    {
                        return SearchAction.ReturnEmptyList;
                    }
                    stripProjectDirectory = !string.Equals(fixedDirectoryPart, text, StringComparison.OrdinalIgnoreCase);
                }
                else
                {
                    fixedDirectoryPart = projectDirectoryUnescaped;
                    stripProjectDirectory = true;
                }
            }
            if (fixedDirectoryPart.Length > 0 && !_fileSystem.DirectoryExists(fixedDirectoryPart))
            {
                return SearchAction.ReturnEmptyList;
            }
            string text2 = null;
            if (wildcardDirectoryPart.Length > 0)
            {
                string text3 = wildcardDirectoryPart.TrimTrailingSlashes();
                int length = text3.Length;
                if (length > 6 && text3[0] == '*' && text3[1] == '*' && FileUtilities.IsAnySlash(text3[2]) && FileUtilities.IsAnySlash(text3[length - 3]) && text3[length - 2] == '*' && text3[length - 1] == '*' && text3.IndexOfAny(FileUtilities.Slashes, 3, length - 6) == -1)
                {
                    text2 = text3.Substring(3, length - 6);
                }
            }
            bool flag = wildcardDirectoryPart.Length > 0 && text2 == null && !IsRecursiveDirectoryMatch(wildcardDirectoryPart);
            FilesSearchData searchData = new FilesSearchData(flag ? null : filenamePart, text2, flag ? new Regex(RegularExpressionFromFileSpec(text, wildcardDirectoryPart, filenamePart), RegexOptions.IgnoreCase) : null, needsRecursion);
            result.SearchData = searchData;
            result.BaseDirectory = Normalize(fixedDirectoryPart);
            result.RemainingWildcardDirectory = Normalize(wildcardDirectoryPart);
            return SearchAction.RunSearch;
        }

        internal static string RegularExpressionFromFileSpec(string fixedDirectoryPart, string wildcardDirectoryPart, string filenamePart)
        {
            using ReuseableStringBuilder reuseableStringBuilder = new ReuseableStringBuilder(291);
            AppendRegularExpressionFromFixedDirectory(reuseableStringBuilder, fixedDirectoryPart);
            AppendRegularExpressionFromWildcardDirectory(reuseableStringBuilder, wildcardDirectoryPart);
            AppendRegularExpressionFromFilename(reuseableStringBuilder, filenamePart);
            return reuseableStringBuilder.ToString();
        }
        private static int LastIndexOfDirectorySequence(string str, int startIndex)
        {
            if (startIndex >= str.Length || !FileUtilities.IsAnySlash(str[startIndex]))
            {
                return startIndex;
            }
            int num = startIndex;
            bool flag = false;
            while (!flag && num < str.Length)
            {
                bool num2 = num < str.Length - 1 && FileUtilities.IsAnySlash(str[num + 1]);
                bool flag2 = num < str.Length - 2 && str[num + 1] == '.' && FileUtilities.IsAnySlash(str[num + 2]);
                if (num2)
                {
                    num++;
                }
                else if (flag2)
                {
                    num += 2;
                }
                else
                {
                    flag = true;
                }
            }
            return num;
        }

        private static int LastIndexOfDirectoryOrRecursiveSequence(string str, int startIndex)
        {
            if (startIndex >= str.Length - 1 || str[startIndex] != '*' || str[startIndex + 1] != '*')
            {
                return LastIndexOfDirectorySequence(str, startIndex);
            }
            int num = startIndex + 2;
            bool flag = false;
            while (!flag && num < str.Length)
            {
                num = LastIndexOfDirectorySequence(str, num);
                if (num < str.Length - 2 && str[num + 1] == '*' && str[num + 2] == '*')
                {
                    num += 3;
                }
                else
                {
                    flag = true;
                }
            }
            return num + 1;
        }

        private static void AppendRegularExpressionFromFixedDirectory(ReuseableStringBuilder regex, string fixedDir)
        {
            regex.Append("^");
            int num;
            //if (NativeMethodsShared.IsWindows && fixedDir.Length > 1 && fixedDir[0] == '\\')
            //{
            //    num = ((fixedDir[1] == '\\') ? 1 : 0);
            //    if (num != 0)
            //    {
            //        regex.Append("\\\\\\\\");
            //    }
            //}
            //else
            {
                num = 0;
            }
            for (int num2 = ((num != 0) ? (LastIndexOfDirectorySequence(fixedDir, 0) + 1) : LastIndexOfDirectorySequence(fixedDir, 0)); num2 < fixedDir.Length; num2 = LastIndexOfDirectorySequence(fixedDir, num2 + 1))
            {
                AppendRegularExpressionFromChar(regex, fixedDir[num2]);
            }
        }

        private static void AppendRegularExpressionFromWildcardDirectory(ReuseableStringBuilder regex, string wildcardDir)
        {
            regex.Append("(?<WILDCARDDIR>");
            if (wildcardDir.Length > 2 && wildcardDir[0] == '*' && wildcardDir[1] == '*')
            {
                regex.Append("((.*/)|(.*\\\\)|())");
            }
            for (int num = LastIndexOfDirectoryOrRecursiveSequence(wildcardDir, 0); num < wildcardDir.Length; num = LastIndexOfDirectoryOrRecursiveSequence(wildcardDir, num + 1))
            {
                char ch = wildcardDir[num];
                if (num < wildcardDir.Length - 2 && wildcardDir[num + 1] == '*' && wildcardDir[num + 2] == '*')
                {
                    regex.Append("((/)|(\\\\)|(/.*/)|(/.*\\\\)|(\\\\.*\\\\)|(\\\\.*/))");
                }
                else
                {
                    AppendRegularExpressionFromChar(regex, ch);
                }
            }
            regex.Append(")");
        }

        private static void AppendRegularExpressionFromFilename(ReuseableStringBuilder regex, string filename)
        {
            regex.Append("(?<FILENAME>");
            bool flag = filename.Length > 0 && filename[filename.Length - 1] == '.';
            int num = (flag ? (filename.Length - 1) : filename.Length);
            for (int i = 0; i < num; i++)
            {
                char c = filename[i];
                if (flag && c == '*')
                {
                    regex.Append("[^\\.]*");
                }
                else if (flag && c == '?')
                {
                    regex.Append("[^\\.].");
                }
                else
                {
                    AppendRegularExpressionFromChar(regex, c);
                }
                if (!flag && i < num - 2 && c == '*' && filename[i + 1] == '.' && filename[i + 2] == '*')
                {
                    i += 2;
                }
            }
            regex.Append(")");
            regex.Append("$");
        }

        private static void AppendRegularExpressionFromChar(ReuseableStringBuilder regex, char ch)
        {
            switch (ch)
            {
                case '*':
                    regex.Append("[^/\\\\]*");
                    return;
                case '?':
                    regex.Append(".");
                    return;
            }
            if (FileUtilities.IsAnySlash(ch))
            {
                regex.Append("[/\\\\]+");
            }
            else if (IsSpecialRegexCharacter(ch))
            {
                regex.Append('\\');
                regex.Append(ch);
            }
            else
            {
                regex.Append(ch);
            }
        }

        private static bool IsSpecialRegexCharacter(char ch)
        {
            if (ch != '$' && ch != '(' && ch != ')' && ch != '+' && ch != '.' && ch != '[' && ch != '^' && ch != '{')
            {
                return ch == '|';
            }
            return true;
        }

        private static bool IsValidDriveChar(char value)
        {
            if (value < 'A' || value > 'Z')
            {
                if (value >= 'a')
                {
                    return value <= 'z';
                }
                return false;
            }
            return true;
        }

        private static int SkipSlashes(string aString, int startingIndex)
        {
            int i;
            for (i = startingIndex; i < aString.Length && FileUtilities.IsAnySlash(aString[i]); i++)
            {
            }
            return i;
        }
        internal static bool IsRecursiveDirectoryMatch(string path)
        {
            return path.TrimTrailingSlashes() == "**";
        }

        internal static string Normalize(string aString)
        {
            if (string.IsNullOrEmpty(aString))
            {
                return aString;
            }
            StringBuilder stringBuilder = new StringBuilder(aString.Length);
            int num = 0;
            if (aString.Length >= 2 && aString[1] == ':' && IsValidDriveChar(aString[0]))
            {
                stringBuilder.Append(aString[0]);
                stringBuilder.Append(aString[1]);
                int num2 = SkipSlashes(aString, 2);
                if (num != num2)
                {
                    stringBuilder.Append('\\');
                }
                num = num2;
            }
            else if (aString.StartsWith("/", StringComparison.Ordinal))
            {
                stringBuilder.Append('/');
                num = SkipSlashes(aString, 1);
            }
            else if (aString.StartsWith("\\\\", StringComparison.Ordinal))
            {
                stringBuilder.Append("\\\\");
                num = SkipSlashes(aString, 2);
            }
            else if (aString.StartsWith("\\", StringComparison.Ordinal))
            {
                stringBuilder.Append("\\");
                num = SkipSlashes(aString, 1);
            }
            while (num < aString.Length)
            {
                int num3 = SkipSlashes(aString, num);
                if (num3 >= aString.Length)
                {
                    break;
                }
                if (num3 > num)
                {
                    stringBuilder.Append(s_directorySeparator);
                }
                int num4 = aString.IndexOfAny(directorySeparatorCharacters, num3);
                int num5 = ((num4 == -1) ? aString.Length : num4);
                stringBuilder.Append(aString, num3, num5 - num3);
                num = num5;
            }
            return stringBuilder.ToString();
        }
        private string[] GetFilesImplementation(string projectDirectoryUnescaped, string filespecUnescaped, List<string> excludeSpecsUnescaped)
        {
            bool stripProjectDirectory;
            RecursionState result;
            SearchAction fileSearchData = GetFileSearchData(projectDirectoryUnescaped, filespecUnescaped, out stripProjectDirectory, out result);
            switch (fileSearchData)
            {
                case SearchAction.ReturnEmptyList:
                    return Array.Empty<string>();
                case SearchAction.ReturnFileSpec:
                    return CreateArrayWithSingleItemIfNotExcluded(filespecUnescaped, excludeSpecsUnescaped);
                default:
                    throw new NotSupportedException(fileSearchData.ToString());
                case SearchAction.RunSearch:
                    {
                        List<RecursionState> list2 = null;
                        Dictionary<string, List<RecursionState>> dictionary = null;
                        HashSet<string> resultsToExclude = null;
                        if (excludeSpecsUnescaped != null)
                        {
                            list2 = new List<RecursionState>();
                            foreach (string item in excludeSpecsUnescaped)
                            {
                                bool stripProjectDirectory2;
                                RecursionState result2;
                                SearchAction fileSearchData2 = GetFileSearchData(projectDirectoryUnescaped, item, out stripProjectDirectory2, out result2);
                                switch (fileSearchData2)
                                {
                                    case SearchAction.ReturnFileSpec:
                                        if (resultsToExclude == null)
                                        {
                                            resultsToExclude = new HashSet<string>();
                                        }
                                        resultsToExclude.Add(item);
                                        break;
                                    default:
                                        throw new NotSupportedException(fileSearchData2.ToString());
                                    case SearchAction.RunSearch:
                                        {
                                            string baseDirectory = result2.BaseDirectory;
                                            string baseDirectory2 = result.BaseDirectory;
                                            if (!string.Equals(baseDirectory, baseDirectory2, StringComparison.OrdinalIgnoreCase))
                                            {
                                                if (baseDirectory.Length == baseDirectory2.Length)
                                                {
                                                    break;
                                                }
                                                if (baseDirectory.Length > baseDirectory2.Length)
                                                {
                                                    if (IsSubdirectoryOf(baseDirectory, baseDirectory2))
                                                    {
                                                        if (dictionary == null)
                                                        {
                                                            dictionary = new Dictionary<string, List<RecursionState>>(StringComparer.OrdinalIgnoreCase);
                                                        }
                                                        if (!dictionary.TryGetValue(baseDirectory, out var value))
                                                        {
                                                            value = (dictionary[baseDirectory] = new List<RecursionState>());
                                                        }
                                                        value.Add(result2);
                                                    }
                                                }
                                                else if (IsSubdirectoryOf(result.BaseDirectory, result2.BaseDirectory) && result2.RemainingWildcardDirectory.Length != 0)
                                                {
                                                    if (IsRecursiveDirectoryMatch(result2.RemainingWildcardDirectory))
                                                    {
                                                        result2.BaseDirectory = result.BaseDirectory;
                                                        list2.Add(result2);
                                                    }
                                                    else
                                                    {
                                                        result2.BaseDirectory = result.BaseDirectory;
                                                        result2.RemainingWildcardDirectory = "**" + s_directorySeparator;
                                                        list2.Add(result2);
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                string text = result.SearchData.Filespec ?? string.Empty;
                                                string text2 = result2.SearchData.Filespec ?? string.Empty;
                                                int num = Math.Min(text.Length - text.LastIndexOfAny(s_wildcardCharacters) - 1, text2.Length - text2.LastIndexOfAny(s_wildcardCharacters) - 1);
                                                if (string.Compare(text, text.Length - num, text2, text2.Length - num, num, StringComparison.OrdinalIgnoreCase) == 0)
                                                {
                                                    list2.Add(result2);
                                                }
                                            }
                                            break;
                                        }
                                    case SearchAction.ReturnEmptyList:
                                        break;
                                }
                            }
                        }
                        if (list2 != null && list2.Count == 0)
                        {
                            list2 = null;
                        }
                        ConcurrentStack<List<string>> concurrentStack = new ConcurrentStack<List<string>>();
                        try
                        {
                            int num2 = Math.Max(1, /*NativeMethodsShared.GetLogicalCoreCount()*/Environment.ProcessorCount / 2);
                            TaskOptions taskOptions = new TaskOptions(num2)
                            {
                                AvailableTasks = num2,
                                MaxTasksPerIteration = num2
                            };
                            GetFilesRecursive(concurrentStack, result, projectDirectoryUnescaped, stripProjectDirectory, list2, dictionary, taskOptions);
                        }
                        catch (AggregateException ex)
                        {
                            if (ex.Flatten().InnerExceptions.All(ExceptionHandling.IsIoRelatedException))
                            {
                                return CreateArrayWithSingleItemIfNotExcluded(filespecUnescaped, excludeSpecsUnescaped);
                            }
                            throw;
                        }
                        catch (Exception e) when (ExceptionHandling.IsIoRelatedException(e))
                        {
                            return CreateArrayWithSingleItemIfNotExcluded(filespecUnescaped, excludeSpecsUnescaped);
                        }
                        if (resultsToExclude == null)
                        {
                            return concurrentStack.SelectMany((List<string> list) => list).ToArray();
                        }
                        return (from f in concurrentStack.SelectMany((List<string> list) => list)
                                where !resultsToExclude.Contains(f)
                                select f).ToArray();
                    }
            }
        }

        private IEnumerable<string> GetFilesForStep(RecursiveStepResult stepResult, RecursionState recursionState, string projectDirectory, bool stripProjectDirectory)
        {
            if (!stepResult.ConsiderFiles)
            {
                return Enumerable.Empty<string>();
            }
            string pattern;
            if (/*NativeMethodsShared.IsLinux*/true && recursionState.SearchData.DirectoryPattern != null)
            {
                pattern = "*.*";
                stepResult.NeedsToProcessEachFile = true;
            }
            else
            {
                pattern = recursionState.SearchData.Filespec;
            }
            IEnumerable<string> enumerable = _getFileSystemEntries(FileSystemEntity.Files, recursionState.BaseDirectory, pattern, projectDirectory, stripProjectDirectory);
            if (!stepResult.NeedsToProcessEachFile)
            {
                return enumerable;
            }
            return enumerable.Where((string o) => MatchFileRecursionStep(recursionState, o));
        }

        private static bool IsAllFilesWildcard(string pattern)
        {
            return pattern?.Length switch
            {
                1 => pattern[0] == '*',
                3 => pattern[0] == '*' && pattern[1] == '.' && pattern[2] == '*',
                _ => false,
            };
        }

        private static bool MatchFileRecursionStep(RecursionState recursionState, string file)
        {
            if (IsAllFilesWildcard(recursionState.SearchData.Filespec))
            {
                return true;
            }
            if (recursionState.SearchData.Filespec != null)
            {
                return IsMatch(Path.GetFileName(file), recursionState.SearchData.Filespec);
            }
            return recursionState.SearchData.RegexFileMatch.IsMatch(file);
        }

        private static RecursiveStepResult GetFilesRecursiveStep(RecursionState recursionState)
        {
            RecursiveStepResult result = default(RecursiveStepResult);
            bool flag = false;
            if (recursionState.SearchData.DirectoryPattern != null)
            {
                flag = recursionState.IsInsideMatchingDirectory;
            }
            else if (recursionState.RemainingWildcardDirectory.Length == 0)
            {
                flag = true;
            }
            else if (recursionState.RemainingWildcardDirectory.IndexOf("**", StringComparison.Ordinal) == 0)
            {
                flag = true;
            }
            result.ConsiderFiles = flag;
            if (flag)
            {
                result.NeedsToProcessEachFile = recursionState.SearchData.Filespec == null;
            }
            if (recursionState.SearchData.NeedsRecursion && recursionState.RemainingWildcardDirectory.Length > 0)
            {
                string text = null;
                if (!IsRecursiveDirectoryMatch(recursionState.RemainingWildcardDirectory))
                {
                    int num = recursionState.RemainingWildcardDirectory.IndexOfAny(directorySeparatorCharacters);
                    text = ((num != -1) ? recursionState.RemainingWildcardDirectory.Substring(0, num) : recursionState.RemainingWildcardDirectory);
                    if (text == "**")
                    {
                        text = null;
                        recursionState.RemainingWildcardDirectory = "**";
                    }
                    else
                    {
                        recursionState.RemainingWildcardDirectory = ((num != -1) ? recursionState.RemainingWildcardDirectory.Substring(num + 1) : string.Empty);
                    }
                }
                result.NeedsDirectoryRecursion = true;
                result.RemainingWildcardDirectory = recursionState.RemainingWildcardDirectory;
                result.DirectoryPattern = text;
            }
            return result;
        }

        private void GetFilesRecursive(ConcurrentStack<List<string>> listOfFiles, RecursionState recursionState, string projectDirectory, bool stripProjectDirectory, IList<RecursionState> searchesToExclude, Dictionary<string, List<RecursionState>> searchesToExcludeInSubdirs, TaskOptions taskOptions)
        {
            ErrorUtilities.VerifyThrow(recursionState.SearchData.Filespec == null || recursionState.SearchData.RegexFileMatch == null, "File-spec overrides the regular expression -- pass null for file-spec if you want to use the regular expression.");
            ErrorUtilities.VerifyThrow(recursionState.SearchData.Filespec != null || recursionState.SearchData.RegexFileMatch != null, "Need either a file-spec or a regular expression to match files.");
            ErrorUtilities.VerifyThrow(recursionState.RemainingWildcardDirectory != null, "Expected non-null remaning wildcard directory.");
            RecursiveStepResult[] excludeNextSteps = null;
            if (searchesToExclude != null)
            {
                excludeNextSteps = new RecursiveStepResult[searchesToExclude.Count];
                for (int i = 0; i < searchesToExclude.Count; i++)
                {
                    RecursionState recursionState2 = searchesToExclude[i];
                    excludeNextSteps[i] = GetFilesRecursiveStep(searchesToExclude[i]);
                    if (!recursionState2.IsLookingForMatchingDirectory && recursionState2.SearchData.Filespec != null && recursionState2.RemainingWildcardDirectory == recursionState.RemainingWildcardDirectory && (IsAllFilesWildcard(recursionState2.SearchData.Filespec) || recursionState2.SearchData.Filespec == recursionState.SearchData.Filespec))
                    {
                        return;
                    }
                }
            }
            RecursiveStepResult nextStep = GetFilesRecursiveStep(recursionState);
            List<string> list = null;
            foreach (string item2 in GetFilesForStep(nextStep, recursionState, projectDirectory, stripProjectDirectory))
            {
                if (excludeNextSteps != null)
                {
                    bool flag = false;
                    for (int j = 0; j < excludeNextSteps.Length; j++)
                    {
                        if (excludeNextSteps[j].ConsiderFiles && MatchFileRecursionStep(searchesToExclude[j], item2))
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag)
                    {
                        continue;
                    }
                }
                if (list == null)
                {
                    list = new List<string>();
                }
                list.Add(item2);
            }
            if (list != null && list.Count > 0)
            {
                listOfFiles.Push(list);
            }
            if (!nextStep.NeedsDirectoryRecursion)
            {
                return;
            }
            Action<string> action = delegate (string subdir)
            {
                RecursionState recursionState3 = recursionState;
                recursionState3.BaseDirectory = subdir;
                recursionState3.RemainingWildcardDirectory = nextStep.RemainingWildcardDirectory;
                if (recursionState3.IsLookingForMatchingDirectory && DirectoryEndsWithPattern(subdir, recursionState.SearchData.DirectoryPattern))
                {
                    recursionState3.IsInsideMatchingDirectory = true;
                }
                List<RecursionState> list2 = null;
                if (excludeNextSteps != null)
                {
                    list2 = new List<RecursionState>();
                    for (int k = 0; k < excludeNextSteps.Length; k++)
                    {
                        if (excludeNextSteps[k].NeedsDirectoryRecursion && (excludeNextSteps[k].DirectoryPattern == null || IsMatch(Path.GetFileName(subdir), excludeNextSteps[k].DirectoryPattern)))
                        {
                            RecursionState item = searchesToExclude[k];
                            item.BaseDirectory = subdir;
                            item.RemainingWildcardDirectory = excludeNextSteps[k].RemainingWildcardDirectory;
                            if (item.IsLookingForMatchingDirectory && DirectoryEndsWithPattern(subdir, item.SearchData.DirectoryPattern))
                            {
                                item.IsInsideMatchingDirectory = true;
                            }
                            list2.Add(item);
                        }
                    }
                }
                if (searchesToExcludeInSubdirs != null && searchesToExcludeInSubdirs.TryGetValue(subdir, out var value))
                {
                    if (list2 == null)
                    {
                        list2 = new List<RecursionState>();
                    }
                    list2.AddRange(value);
                }
                GetFilesRecursive(listOfFiles, recursionState3, projectDirectory, stripProjectDirectory, list2, searchesToExcludeInSubdirs, taskOptions);
            };
            int num = 0;
            if (taskOptions.MaxTasks > 1 && taskOptions.MaxTasksPerIteration > 1)
            {
                if (taskOptions.MaxTasks == taskOptions.MaxTasksPerIteration)
                {
                    num = taskOptions.AvailableTasks;
                    taskOptions.AvailableTasks = 0;
                }
                else
                {
                    lock (taskOptions)
                    {
                        num = Math.Min(taskOptions.MaxTasksPerIteration, taskOptions.AvailableTasks);
                        taskOptions.AvailableTasks -= num;
                    }
                }
            }
            if (num < 2)
            {
                foreach (string item3 in _getFileSystemEntries(FileSystemEntity.Directories, recursionState.BaseDirectory, nextStep.DirectoryPattern, null, stripProjectDirectory: false))
                {
                    action(item3);
                }
            }
            else
            {
                Parallel.ForEach(_getFileSystemEntries(FileSystemEntity.Directories, recursionState.BaseDirectory, nextStep.DirectoryPattern, null, stripProjectDirectory: false), new ParallelOptions
                {
                    MaxDegreeOfParallelism = num
                }, action);
            }
            if (num <= 0)
            {
                return;
            }
            if (taskOptions.MaxTasks == taskOptions.MaxTasksPerIteration)
            {
                taskOptions.AvailableTasks = taskOptions.MaxTasks;
                return;
            }
            lock (taskOptions)
            {
                taskOptions.AvailableTasks += num;
            }
        }

        private static bool IsSubdirectoryOf(string possibleChild, string possibleParent)
        {
            if (possibleParent == string.Empty)
            {
                return true;
            }
            if (!possibleChild.StartsWith(possibleParent, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (directorySeparatorCharacters.Contains(possibleParent[possibleParent.Length - 1]))
            {
                return true;
            }
            return directorySeparatorCharacters.Contains(possibleChild[possibleParent.Length]);
        }

        private static bool DirectoryEndsWithPattern(string directoryPath, string pattern)
        {
            int num = directoryPath.LastIndexOfAny(FileUtilities.Slashes);
            if (num != -1)
            {
                return IsMatch(directoryPath.Substring(num + 1), pattern);
            }
            return false;
        }

        internal void GetFileSpecInfoWithRegexObject(string filespec, out Regex regexFileMatch, out bool needsRecursion, out bool isLegalFileSpec)
        {
            GetFileSpecInfo(filespec, out var fixedDirectoryPart, out var wildcardDirectoryPart, out var filenamePart, out needsRecursion, out isLegalFileSpec);
            if (isLegalFileSpec)
            {
                string pattern = RegularExpressionFromFileSpec(fixedDirectoryPart, wildcardDirectoryPart, filenamePart);
                regexFileMatch = new Regex(pattern, RegexOptions.IgnoreCase);
            }
            else
            {
                regexFileMatch = null;
            }
        }

        internal static void GetRegexMatchInfo(string fileToMatch, Regex fileSpecRegex, out bool isMatch, out string wildcardDirectoryPart, out string filenamePart)
        {
            Match match = fileSpecRegex.Match(fileToMatch);
            isMatch = match.Success;
            wildcardDirectoryPart = string.Empty;
            filenamePart = string.Empty;
            if (isMatch)
            {
                wildcardDirectoryPart = match.Groups["WILDCARDDIR"].Value;
                filenamePart = match.Groups["FILENAME"].Value;
            }
        }

        internal Result FileMatch(string filespec, string fileToMatch)
        {
            Result result = new Result();
            fileToMatch = GetLongPathName(fileToMatch, _getFileSystemEntries);
            GetFileSpecInfoWithRegexObject(filespec, out var regexFileMatch, out result.isFileSpecRecursive, out result.isLegalFileSpec);
            if (result.isLegalFileSpec)
            {
                GetRegexMatchInfo(fileToMatch, regexFileMatch, out result.isMatch, out result.wildcardDirectoryPart, out var _);
            }
            return result;
        }

        private static string[] CreateArrayWithSingleItemIfNotExcluded(string filespecUnescaped, List<string> excludeSpecsUnescaped)
        {
            if (excludeSpecsUnescaped != null)
            {
                foreach (string item in excludeSpecsUnescaped)
                {
                    if (FileUtilities.PathsEqual(filespecUnescaped, item))
                    {
                        return Array.Empty<string>();
                    }
                    Result result = Default.FileMatch(item, filespecUnescaped);
                    if (result.isLegalFileSpec && result.isMatch)
                    {
                        return Array.Empty<string>();
                    }
                }
            }
            return new string[1] { filespecUnescaped };
        }

    }
}