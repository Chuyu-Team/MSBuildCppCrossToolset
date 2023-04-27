using System.Collections.Generic;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using Microsoft.Build.Shared.FileSystem;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.Utilities
{
    public static class TrackedDependencies
    {
        public static ITaskItem[] ExpandWildcards(ITaskItem[] expand)
        {
            if (expand == null)
            {
                return null;
            }
            List<ITaskItem> list = new List<ITaskItem>(expand.Length);
            foreach (ITaskItem taskItem in expand)
            {
                if (FileMatcher.HasWildcards(taskItem.ItemSpec))
                {
                    string directoryName = Path.GetDirectoryName(taskItem.ItemSpec);
                    string fileName = Path.GetFileName(taskItem.ItemSpec);
                    string[] array = ((FileMatcher.HasWildcards(directoryName) || !FileSystems.Default.DirectoryExists(directoryName)) ? FileMatcher.Default.GetFiles(null, taskItem.ItemSpec) : Directory.GetFiles(directoryName, fileName));
                    string[] array2 = array;
                    foreach (string itemSpec in array2)
                    {
                        list.Add(new TaskItem(taskItem)
                        {
                            ItemSpec = itemSpec
                        });
                    }
                }
                else
                {
                    list.Add(taskItem);
                }
            }
            return list.ToArray();
        }

        internal static bool ItemsExist(ITaskItem[] files)
        {
            bool result = true;
            if (files != null && files.Length != 0)
            {
                for (int i = 0; i < files.Length; i++)
                {
                    if (!FileUtilities.FileExistsNoThrow(files[i].ItemSpec))
                    {
                        result = false;
                        break;
                    }
                }
            }
            else
            {
                result = false;
            }
            return result;
        }
    }

}
