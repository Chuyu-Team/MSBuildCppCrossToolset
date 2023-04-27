using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Tasks;
using Microsoft.Build.Shared;

namespace Microsoft.Build.CPPTasks
{
    internal static class MsbuildTaskUtilities
    {
        public static char[] semicolonSeparator = new char[1] { ';' };

        public static string[] GetWildcardExpandedFileListFromMetadata(IBuildEngine buildEngine, ITaskItem item, string metadataName, TaskLoggingHelper log = null, bool convertToUpperCase = true)
        {
            string metadata = item.GetMetadata(metadataName);
            string warningResource = null;
            if (metadataName == "AdditionalInputs")
            {
                warningResource = "CustomBuild.InvalidDependency";
            }
            else if (metadataName == "Outputs")
            {
                warningResource = "CustomBuild.InvalidOutput";
            }
            return GetWildcardExpandedFileList(buildEngine, metadata, log, warningResource, item.ItemSpec, convertToUpperCase);
        }

        public static string[] GetWildcardExpandedFileList(IBuildEngine buildEngine, string value, TaskLoggingHelper log = null, string warningResource = null, string itemName = null, bool convertToUpperCase = true)
        {
            List<string> list = new List<string>();
            CreateItem createItem = new CreateItem();
            createItem.BuildEngine = buildEngine;
            if (!string.IsNullOrEmpty(value))
            {
                string[] array = value.Split(semicolonSeparator);
                List<ITaskItem> list2 = new List<ITaskItem>();
                string[] array2 = array;
                foreach (string text in array2)
                {
                    string text2 = text.Trim();
                    if (!string.IsNullOrEmpty(text2))
                    {
                        list2.Add(new TaskItem(text2));
                    }
                }
                createItem.Include = list2.ToArray();
                createItem.Execute();
                ITaskItem[] include = createItem.Include;
                foreach (ITaskItem taskItem in include)
                {
                    try
                    {
                        string text3 = taskItem.GetMetadata("FullPath");
                        //if (convertToUpperCase)
                        //{
                        //    text3 = text3.ToUpperInvariant();
                        //}
                        list.Add(text3);
                    }
                    catch (Exception ex)
                    {
                        if (log != null && warningResource != null && itemName != null)
                        {
                            log.LogWarningWithCodeFromResources(warningResource, itemName, taskItem.ItemSpec);
                        }
                        ex.RethrowIfCritical();
                    }
                }
            }
            return list.ToArray();
        }

        public static string FileNameFromHash(string content)
        {
            return VCUtilities.GetHashString(content);
        }
    }
}
