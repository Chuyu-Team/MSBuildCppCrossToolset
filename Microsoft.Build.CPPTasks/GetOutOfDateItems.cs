using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.CPPTasks
{
    public class GetOutOfDateItems : Microsoft.Build.Utilities.Task
    {
        private const string DependsOnAnotherItemOutputsMetadataName = "DependsOnAnotherItemOutput";

        private const string InitializingValue = "Initializing";

        public ITaskItem[] Sources { get; set; }

        [Required]
        public string OutputsMetadataName { get; set; }

        public string DependenciesMetadataName { get; set; }

        public string CommandMetadataName { get; set; }

        [Required]
        public string TLogDirectory { get; set; }

        [Required]
        public string TLogNamePrefix { get; set; }

        public bool CheckForInterdependencies { get; set; }

        public bool TrackFileAccess { get; set; }

        [Output]
        public ITaskItem[] OutOfDateSources { get; set; }

        [Output]
        public bool HasInterdependencies { get; set; }

        public GetOutOfDateItems()
            : base(new ResourceManager("Microsoft.Build.CPPTasks.Strings", Assembly.GetExecutingAssembly()))
        {
            CheckForInterdependencies = false;
            HasInterdependencies = false;
            DependenciesMetadataName = null;
            CommandMetadataName = null;
            TrackFileAccess = true;
        }

        public override bool Execute()
        {
            //IL_00fd: Unknown result type (might be due to invalid IL or missing references)
            //IL_0104: Expected O, but got Unknown
            //IL_011f: Unknown result type (might be due to invalid IL or missing references)
            //IL_0126: Expected O, but got Unknown
            if (!TrackFileAccess)
            {
                if (Sources != null)
                {
                    List<ITaskItem> list = new List<ITaskItem>();
                    ITaskItem[] sources = Sources;
                    foreach (ITaskItem sourceItem in sources)
                    {
                        list.Add(new TaskItem(sourceItem));
                    }
                    OutOfDateSources = list.ToArray();
                }
                return true;
            }
            TaskItem taskItem = new TaskItem(Path.Combine(TLogDirectory, TLogNamePrefix + ".read.1.tlog"));
            TaskItem taskItem2 = new TaskItem(Path.Combine(TLogDirectory, TLogNamePrefix + ".write.1.tlog"));
            TaskItem taskItem3 = new TaskItem(Path.Combine(TLogDirectory, TLogNamePrefix + ".command.1.tlog"));
            string metadata = taskItem.GetMetadata("FullPath");
            string metadata2 = taskItem2.GetMetadata("FullPath");
            string metadata3 = taskItem3.GetMetadata("FullPath");
            List<ITaskItem> list2 = new List<ITaskItem>();
            if (Sources != null)
            {
                CanonicalTrackedOutputFiles val = new CanonicalTrackedOutputFiles((ITask)this, new ITaskItem[1] { taskItem2 }, true);
                CanonicalTrackedInputFiles val2 = new CanonicalTrackedInputFiles((ITask)this, new ITaskItem[1] { taskItem }, Sources, new ITaskItem[0], val, true, false);
                ITaskItem[] collection = val2.ComputeSourcesNeedingCompilation(false);
                HashSet<ITaskItem> hashSet = new HashSet<ITaskItem>(collection);
                StringBuilder stringBuilder = new StringBuilder();
                StringBuilder stringBuilder2 = new StringBuilder();
                StringBuilder stringBuilder3 = new StringBuilder();
                Dictionary<string, object> dictionary = ReadCommandLines(metadata3);
                Dictionary<ITaskItem, HashSet<string>> dictionary2 = new Dictionary<ITaskItem, HashSet<string>>();
                Dictionary<ITaskItem, HashSet<string>> dictionary3 = new Dictionary<ITaskItem, HashSet<string>>();
                ITaskItem[] sources2 = Sources;
                foreach (ITaskItem taskItem4 in sources2)
                {
                    string metadata4 = taskItem4.GetMetadata("FullPath");
                    string text = metadata4/*.ToUpperInvariant()*/;
                    string text2 = (string.IsNullOrEmpty(CommandMetadataName) ? string.Empty : taskItem4.GetMetadata(CommandMetadataName).Trim());
                    string[] collection2 = (string.IsNullOrEmpty(DependenciesMetadataName) ? new string[0] : MsbuildTaskUtilities.GetWildcardExpandedFileListFromMetadata(base.BuildEngine, taskItem4, DependenciesMetadataName, base.Log));
                    HashSet<string> hashSet2 = new HashSet<string>(collection2, StringComparer.OrdinalIgnoreCase);
                    hashSet2.Remove(metadata4);
                    string[] wildcardExpandedFileListFromMetadata = MsbuildTaskUtilities.GetWildcardExpandedFileListFromMetadata(base.BuildEngine, taskItem4, OutputsMetadataName, base.Log);
                    HashSet<string> hashSet3 = new HashSet<string>(wildcardExpandedFileListFromMetadata, StringComparer.OrdinalIgnoreCase);
                    dictionary2[taskItem4] = hashSet2;
                    dictionary3[taskItem4] = hashSet3;
                    if (!hashSet.Contains(taskItem4))
                    {
                        bool flag = false;
                        if (!string.IsNullOrEmpty(CommandMetadataName))
                        {
                            flag = true;
                            if (dictionary.TryGetValue(metadata4, out var value))
                            {
                                if (value is List<string> list3)
                                {
                                    foreach (string item in list3)
                                    {
                                        if (text2.Equals(item))
                                        {
                                            flag = false;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    string value2 = (string)value;
                                    if (text2.Equals(value2))
                                    {
                                        flag = false;
                                    }
                                }
                                if (flag)
                                {
                                    hashSet.Add(taskItem4);
                                    base.Log.LogMessageFromResources(MessageImportance.Low, "TrackedVCToolTask.RebuildingSourceCommandLineChanged", metadata4);
                                }
                            }
                            else
                            {
                                hashSet.Add(taskItem4);
                                base.Log.LogMessageFromResources(MessageImportance.Low, "TrackedVCToolTask.RebuildingSourceCommandLineChanged", metadata4);
                            }
                        }
                        if (!flag && !string.IsNullOrEmpty(DependenciesMetadataName))
                        {
                            if (!IsTheSameFileSet(val2.DependencyTable, metadata4, hashSet2.ToArray(), dependencyTableIncludesSourceFile: true))
                            {
                                hashSet.Add(taskItem4);
                                base.Log.LogMessageFromResources(MessageImportance.Low, "GetOutOfDateItems.RebuildingSourceDependenciesChanged", metadata4);
                            }
                            if (!IsTheSameFileSet(val.DependencyTable, metadata4, hashSet3.ToArray(), dependencyTableIncludesSourceFile: false))
                            {
                                hashSet.Add(taskItem4);
                                base.Log.LogMessageFromResources(MessageImportance.Low, "GetOutOfDateItems.RebuildingSourceOutputsChanged", metadata4);
                            }
                        }
                    }
                    stringBuilder.Append("^");
                    stringBuilder.AppendLine(text);
                    stringBuilder.AppendLine(text2);
                    stringBuilder2.Append("^");
                    stringBuilder2.AppendLine(text);
                    foreach (string item2 in hashSet2)
                    {
                        if (!string.Equals(item2, text, StringComparison.OrdinalIgnoreCase))
                        {
                            stringBuilder2.AppendLine(item2);
                        }
                    }
                    stringBuilder3.Append("^");
                    stringBuilder3.AppendLine(text);
                    foreach (string item3 in hashSet3)
                    {
                        stringBuilder3.AppendLine(item3);
                    }
                }
                if (hashSet.Count > 0)
                {
                    Dictionary<string, ITaskItem> dictionary4 = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
                    if (CheckForInterdependencies)
                    {
                        ITaskItem[] sources3 = Sources;
                        foreach (ITaskItem taskItem5 in sources3)
                        {
                            foreach (string item4 in dictionary3[taskItem5])
                            {
                                dictionary4[item4] = taskItem5;
                            }
                        }
                    }
                    ITaskItem[] sources4 = Sources;
                    foreach (ITaskItem taskItem6 in sources4)
                    {
                        CheckIfItemDependsOnOtherItemOutputs(taskItem6, dictionary2, dictionary4, hashSet);
                        if (hashSet.Contains(taskItem6))
                        {
                            list2.Add(new TaskItem(taskItem6));
                        }
                    }
                    Directory.CreateDirectory(Path.GetDirectoryName(metadata));
                    File.WriteAllText(metadata3, stringBuilder.ToString());
                    File.WriteAllText(metadata, stringBuilder2.ToString());
                    File.WriteAllText(metadata2, stringBuilder3.ToString());
                }
            }
            else
            {
                try
                {
                    if (File.Exists(metadata))
                    {
                        File.Delete(metadata);
                    }
                    if (File.Exists(metadata2))
                    {
                        File.Delete(metadata2);
                    }
                    if (File.Exists(metadata3))
                    {
                        File.Delete(metadata3);
                    }
                }
                catch (Exception ex)
                {
                    base.Log.LogWarningWithCodeFromResources("CannotDeleteTlogs", TLogNamePrefix, ex.Message);
                }
            }
            OutOfDateSources = list2.ToArray();
            return true;
        }

        private bool CheckIfItemDependsOnOtherItemOutputs(ITaskItem item, Dictionary<ITaskItem, HashSet<string>> itemDependencies, Dictionary<string, ITaskItem> allOutputs, HashSet<ITaskItem> outOfDateItemHash)
        {
            if (allOutputs.Count > 0 && (!outOfDateItemHash.Contains(item) || string.IsNullOrEmpty(item.GetMetadata("DependsOnAnotherItemOutput"))))
            {
                item.SetMetadata("DependsOnAnotherItemOutput", "Initializing");
                foreach (string item2 in itemDependencies[item])
                {
                    if (!allOutputs.TryGetValue(item2, out var value))
                    {
                        continue;
                    }
                    string metadata = value.GetMetadata("DependsOnAnotherItemOutput");
                    if (!string.Equals(metadata, "Initializing"))
                    {
                        if (outOfDateItemHash.Contains(value))
                        {
                            base.Log.LogMessageFromResources(MessageImportance.Normal, "GetOutOfDateItems.ItemDependsOnAnotherItemOutput", item.GetMetadata("FullPath"), item2, value.GetMetadata("FullPath"));
                            item.SetMetadata("DependsOnAnotherItemOutput", "true");
                            outOfDateItemHash.Add(item);
                            HasInterdependencies = true;
                            return true;
                        }
                        if (CheckIfItemDependsOnOtherItemOutputs(value, itemDependencies, allOutputs, outOfDateItemHash))
                        {
                            return true;
                        }
                    }
                }
                item.SetMetadata("DependsOnAnotherItemOutput", "false");
            }
            return false;
        }

        private Dictionary<string, object> ReadCommandLines(string tlogCommandFile)
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            if (File.Exists(tlogCommandFile))
            {
                string[] array = File.ReadAllLines(tlogCommandFile);
                string text = null;
                StringBuilder stringBuilder = new StringBuilder();
                string[] array2 = array;
                foreach (string text2 in array2)
                {
                    if (string.IsNullOrEmpty(text2) || text2.StartsWith("#"))
                    {
                        continue;
                    }
                    if (text2.StartsWith("^"))
                    {
                        AddCommandLine(dictionary, text, stringBuilder.ToString());
                        stringBuilder.Clear();
                        text = text2.Substring(1);
                    }
                    else if (!string.IsNullOrEmpty(text))
                    {
                        if (stringBuilder.Length != 0)
                        {
                            stringBuilder.Append("\r\n");
                        }
                        stringBuilder.Append(text2);
                    }
                }
                AddCommandLine(dictionary, text, stringBuilder.ToString());
            }
            return dictionary;
        }

        private void AddCommandLine(Dictionary<string, object> commandLines, string sourceFile, string command)
        {
            if (string.IsNullOrEmpty(sourceFile))
            {
                return;
            }
            if (commandLines.TryGetValue(sourceFile, out var value))
            {
                List<string> list = value as List<string>;
                if (list == null)
                {
                    list = new List<string>();
                    list.Add((string)value);
                    commandLines[sourceFile] = list;
                }
                list.Add(command);
            }
            else
            {
                commandLines[sourceFile] = command;
            }
        }

        private bool IsTheSameFileSet<T>(Dictionary<string, Dictionary<string, T>> dependencyTable, string primaryFile, string[] newFileSet, bool dependencyTableIncludesSourceFile)
        {
            if (!dependencyTable.TryGetValue(primaryFile, out var value))
            {
                return false;
            }
            if (value.Count != newFileSet.GetLength(0) + (dependencyTableIncludesSourceFile ? 1 : 0))
            {
                return false;
            }
            foreach (string key in newFileSet)
            {
                if (!value.ContainsKey(key))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
