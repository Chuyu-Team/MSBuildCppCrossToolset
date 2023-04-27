using System;
using System.Collections.Generic;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.Utilities
{
    internal static class CanonicalTrackedFilesHelper
    {
        internal const int MaxLogCount = 100;

        internal static bool RootContainsAllSubRootComponents(string compositeRoot, string compositeSubRoot)
        {
            if (string.Equals(compositeRoot, compositeSubRoot, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            string[] array = compositeSubRoot.Split(MSBuildConstants.PipeChar);
            foreach (string value in array)
            {
                if (!compositeRoot.Contains(value))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool FilesExistAndRecordNewestWriteTime(ICollection<ITaskItem> files, TaskLoggingHelper log, out DateTime outputNewestTime, out string outputNewestFilename)
        {
            return FilesExistAndRecordRequestedWriteTime(files, log, getNewest: true, out outputNewestTime, out outputNewestFilename);
        }

        internal static bool FilesExistAndRecordOldestWriteTime(ICollection<ITaskItem> files, TaskLoggingHelper log, out DateTime outputOldestTime, out string outputOldestFilename)
        {
            return FilesExistAndRecordRequestedWriteTime(files, log, getNewest: false, out outputOldestTime, out outputOldestFilename);
        }

        private static bool FilesExistAndRecordRequestedWriteTime(ICollection<ITaskItem> files, TaskLoggingHelper log, bool getNewest, out DateTime requestedTime, out string requestedFilename)
        {
            bool result = true;
            requestedTime = (getNewest ? DateTime.MinValue : DateTime.MaxValue);
            requestedFilename = string.Empty;
            if (files == null || files.Count == 0)
            {
                return false;
            }
            foreach (ITaskItem file in files)
            {
                DateTime lastWriteFileUtcTime = NativeMethods.GetLastWriteFileUtcTime(file.ItemSpec);
                if (lastWriteFileUtcTime == DateTime.MinValue)
                {
                    FileTracker.LogMessageFromResources(log, MessageImportance.Low, "Tracking_OutputDoesNotExist", file.ItemSpec);
                    return false;
                }
                if ((getNewest && lastWriteFileUtcTime > requestedTime) || (!getNewest && lastWriteFileUtcTime < requestedTime))
                {
                    requestedTime = lastWriteFileUtcTime;
                    requestedFilename = file.ItemSpec;
                }
            }
            return result;
        }
    }
}
