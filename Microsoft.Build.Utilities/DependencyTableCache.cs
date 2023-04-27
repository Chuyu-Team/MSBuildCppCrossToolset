using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Utilities
{
    internal static class DependencyTableCache
    {
        private class TaskItemItemSpecIgnoreCaseComparer : IEqualityComparer<ITaskItem>
        {
            public bool Equals(ITaskItem x, ITaskItem y)
            {
                if (x == y)
                {
                    return true;
                }
                if (x == null || y == null)
                {
                    return false;
                }
                return string.Equals(x.ItemSpec, y.ItemSpec, StringComparison.OrdinalIgnoreCase);
            }

            public int GetHashCode(ITaskItem obj)
            {
                if (obj != null)
                {
                    return StringComparer.OrdinalIgnoreCase.GetHashCode(obj.ItemSpec);
                }
                return 0;
            }
        }

        private static readonly char[] s_numerals = new char[10] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };

        private static readonly TaskItemItemSpecIgnoreCaseComparer s_taskItemComparer = new TaskItemItemSpecIgnoreCaseComparer();

        internal static Dictionary<string, DependencyTableCacheEntry> DependencyTable { get; } = new Dictionary<string, DependencyTableCacheEntry>(StringComparer.OrdinalIgnoreCase);


        private static bool DependencyTableIsUpToDate(DependencyTableCacheEntry dependencyTable)
        {
            DateTime tableTime = dependencyTable.TableTime;
            ITaskItem[] tlogFiles = dependencyTable.TlogFiles;
            for (int i = 0; i < tlogFiles.Length; i++)
            {
                if (NativeMethods.GetLastWriteFileUtcTime(FileUtilities.NormalizePath(tlogFiles[i].ItemSpec)) > tableTime)
                {
                    return false;
                }
            }
            return true;
        }

        internal static DependencyTableCacheEntry GetCachedEntry(string tLogRootingMarker)
        {
            if (DependencyTable.TryGetValue(tLogRootingMarker, out var value))
            {
                if (DependencyTableIsUpToDate(value))
                {
                    return value;
                }
                DependencyTable.Remove(tLogRootingMarker);
            }
            return null;
        }

        internal static string FormatNormalizedTlogRootingMarker(ITaskItem[] tlogFiles)
        {
            HashSet<ITaskItem> hashSet = new HashSet<ITaskItem>(s_taskItemComparer);
            for (int i = 0; i < tlogFiles.Length; i++)
            {
                ITaskItem taskItem = new TaskItem(tlogFiles[i]);
                taskItem.ItemSpec = NormalizeTlogPath(tlogFiles[i].ItemSpec);
                hashSet.Add(taskItem);
            }
            return FileTracker.FormatRootingMarker(hashSet.ToArray());
        }

        private static string NormalizeTlogPath(string tlogPath)
        {
            if (tlogPath.IndexOfAny(s_numerals) == -1)
            {
                return tlogPath;
            }
            StringBuilder stringBuilder = new StringBuilder();
            int num = tlogPath.Length - 1;
            while (num >= 0 && tlogPath[num] != '\\')
            {
                if (tlogPath[num] == '.' || tlogPath[num] == '-')
                {
                    stringBuilder.Append(tlogPath[num]);
                    int num2 = num - 1;
                    while (num2 >= 0 && tlogPath[num2] != '\\' && tlogPath[num2] >= '0' && tlogPath[num2] <= '9')
                    {
                        num2--;
                    }
                    if (num2 >= 0 && tlogPath[num2] == '.')
                    {
                        stringBuilder.Append("]DI[");
                        stringBuilder.Append(tlogPath[num2]);
                        num = num2;
                    }
                }
                else
                {
                    stringBuilder.Append(tlogPath[num]);
                }
                num--;
            }
            StringBuilder stringBuilder2 = new StringBuilder(num + stringBuilder.Length);
            if (num >= 0)
            {
                stringBuilder2.Append(tlogPath, 0, num + 1);
            }
            for (int num3 = stringBuilder.Length - 1; num3 >= 0; num3--)
            {
                stringBuilder2.Append(stringBuilder[num3]);
            }
            return stringBuilder2.ToString();
        }
    }
}
