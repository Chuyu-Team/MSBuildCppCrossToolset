using Microsoft.Build.Framework;
using Microsoft.Build.Tasks;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Shared
{
    internal class FileTracker
    {
        public static bool FileIsExcludedFromDependencies(string fileName)
        {
#if __
            if (!FileIsUnderPath(fileName, s_applicationDataPath) && !FileIsUnderPath(fileName, s_localApplicationDataPath) && !FileIsUnderPath(fileName, s_localLowApplicationDataPath) && !FileIsUnderPath(fileName, s_tempShortPath) && !FileIsUnderPath(fileName, s_tempLongPath))
            {
                return s_commonApplicationDataPaths.Any((string p) => FileIsUnderPath(fileName, p));
            }
            return true;
#else
            // Linux下公共文件跟这些目录都没什么关系。
            return false;
#endif
        }

        public static bool FileIsUnderPath(string fileName, string path)
        {
            path = FileUtilities.EnsureTrailingSlash(path);
            return string.Compare(fileName, 0, path, 0, path.Length, StringComparison.OrdinalIgnoreCase) == 0;
        }

        public static string FormatRootingMarker(ITaskItem source)
        {
            return FormatRootingMarker(new ITaskItem[1] { source }, null);
        }

        public static string FormatRootingMarker(ITaskItem source, ITaskItem output)
        {
            return FormatRootingMarker(new ITaskItem[1] { source }, new ITaskItem[1] { output });
        }

        public static string FormatRootingMarker(ITaskItem[] sources)
        {
            return FormatRootingMarker(sources, null);
        }

        public static string FormatRootingMarker(ITaskItem[] sources, ITaskItem[] outputs)
        {
            ErrorUtilities.VerifyThrowArgumentNull(sources, "sources");
            if (outputs == null)
            {
                outputs = Array.Empty<ITaskItem>();
            }
            List<string> list = new List<string>(sources.Length + outputs.Length);
            ITaskItem[] array = sources;
            foreach (ITaskItem taskItem in array)
            {
                list.Add(FileUtilities.NormalizePath(taskItem.ItemSpec)/*.ToUpperInvariant()*/);
            }
            array = outputs;
            foreach (ITaskItem taskItem2 in array)
            {
                list.Add(FileUtilities.NormalizePath(taskItem2.ItemSpec)/*.ToUpperInvariant()*/);
            }
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return string.Join("|", list);
        }

        internal static void LogMessageFromResources(TaskLoggingHelper Log, MessageImportance importance, string messageResourceName, params object[] messageArgs)
        {
            if (Log != null)
            {
                ErrorUtilities.VerifyThrowArgumentNull(messageResourceName, "messageResourceName");
                Log.LogMessage(importance, messageResourceName, messageArgs);
            }
        }

        internal static void LogMessage(TaskLoggingHelper Log, MessageImportance importance, string message, params object[] messageArgs)
        {
            Log?.LogMessage(importance, message, messageArgs);
        }

        internal static void LogWarningWithCodeFromResources(TaskLoggingHelper Log, string messageResourceName, params object[] messageArgs)
        {
            Log?.LogWarning(messageResourceName, messageArgs);
            // Log?.LogWarningWithCodeFromResources(messageResourceName, messageArgs);
        }
    }
}
