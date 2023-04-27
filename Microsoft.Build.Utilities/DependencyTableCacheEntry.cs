using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Utilities
{
    internal class DependencyTableCacheEntry
    {
        public ITaskItem[] TlogFiles { get; }

        public DateTime TableTime { get; }

        public IDictionary DependencyTable { get; }

        internal DependencyTableCacheEntry(ITaskItem[] tlogFiles, IDictionary dependencyTable)
        {
            TlogFiles = new ITaskItem[tlogFiles.Length];
            TableTime = DateTime.MinValue;
            for (int i = 0; i < tlogFiles.Length; i++)
            {
                string text = FileUtilities.NormalizePath(tlogFiles[i].ItemSpec);
                TlogFiles[i] = new TaskItem(text);
                DateTime lastWriteFileUtcTime = NativeMethods.GetLastWriteFileUtcTime(text);
                if (lastWriteFileUtcTime > TableTime)
                {
                    TableTime = lastWriteFileUtcTime;
                }
            }
            DependencyTable = dependencyTable;
        }
    }
}
