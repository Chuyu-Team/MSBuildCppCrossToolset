using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using Microsoft.Build.Utilities;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Build.CPPTasks
{
    public abstract class TrackedVCToolTask : VCToolTask
    {
        private bool skippedExecution;

        private CanonicalTrackedInputFiles sourceDependencies;

        private CanonicalTrackedOutputFiles sourceOutputs;

        private bool trackFileAccess;

        private bool trackCommandLines = true;

        private bool minimalRebuildFromTracking;

        private bool deleteOutputBeforeExecute;

        private string rootSource;

        private ITaskItem[] tlogReadFiles;

        private ITaskItem[] tlogWriteFiles;

        private ITaskItem tlogCommandFile;

        private ITaskItem[] sourcesCompiled;

        private ITaskItem[] trackedInputFilesToIgnore;

        private ITaskItem[] trackedOutputFilesToIgnore;

        private ITaskItem[] excludedInputPaths = new TaskItem[0];

        private string pathOverride;

        private static readonly char[] NewlineArray = Environment.NewLine.ToCharArray();

        private static readonly Regex extraNewlineRegex = new Regex("(\\r?\\n)?(\\r?\\n)+");

        protected abstract string TrackerIntermediateDirectory { get; }

        protected abstract ITaskItem[] TrackedInputFiles { get; }

        protected CanonicalTrackedInputFiles SourceDependencies
        {
            get
            {
                return sourceDependencies;
            }
            set
            {
                sourceDependencies = value;
            }
        }

        protected CanonicalTrackedOutputFiles SourceOutputs
        {
            get
            {
                return sourceOutputs;
            }
            set
            {
                sourceOutputs = value;
            }
        }

        [Output]
        public bool SkippedExecution
        {
            get
            {
                return skippedExecution;
            }
            set
            {
                skippedExecution = value;
            }
        }

        public string RootSource
        {
            get
            {
                return rootSource;
            }
            set
            {
                rootSource = value;
            }
        }

        protected virtual bool TrackReplaceFile => false;

        protected virtual string[] ReadTLogNames
        {
            get
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(ToolExe);
                return new string[4]
                {
                fileNameWithoutExtension + ".read.*.tlog",
                fileNameWithoutExtension + ".*.read.*.tlog",
                fileNameWithoutExtension + "-*.read.*.tlog",
                GetType().FullName + ".read.*.tlog"
                };
            }
        }

        protected virtual string[] WriteTLogNames
        {
            get
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(ToolExe);
                return new string[4]
                {
                fileNameWithoutExtension + ".write.*.tlog",
                fileNameWithoutExtension + ".*.write.*.tlog",
                fileNameWithoutExtension + "-*.write.*.tlog",
                GetType().FullName + ".write.*.tlog"
                };
            }
        }

        protected virtual string[] DeleteTLogNames
        {
            get
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(ToolExe);
                return new string[4]
                {
                fileNameWithoutExtension + ".delete.*.tlog",
                fileNameWithoutExtension + ".*.delete.*.tlog",
                fileNameWithoutExtension + "-*.delete.*.tlog",
                GetType().FullName + ".delete.*.tlog"
                };
            }
        }

        protected virtual string CommandTLogName
        {
            get
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(ToolExe);
                return fileNameWithoutExtension + ".command.1.tlog";
            }
        }

        public ITaskItem[] TLogReadFiles
        {
            get
            {
                return tlogReadFiles;
            }
            set
            {
                tlogReadFiles = value;
            }
        }

        public ITaskItem[] TLogWriteFiles
        {
            get
            {
                return tlogWriteFiles;
            }
            set
            {
                tlogWriteFiles = value;
            }
        }

        public ITaskItem[] TLogDeleteFiles { get; set; }

        public ITaskItem TLogCommandFile
        {
            get
            {
                return tlogCommandFile;
            }
            set
            {
                tlogCommandFile = value;
            }
        }

        public bool TrackFileAccess
        {
            get
            {
                return trackFileAccess;
            }
            set
            {
                trackFileAccess = value;
            }
        }

        public bool TrackCommandLines
        {
            get
            {
                return trackCommandLines;
            }
            set
            {
                trackCommandLines = value;
            }
        }

        public bool PostBuildTrackingCleanup { get; set; }

        public bool EnableExecuteTool { get; set; }

        public bool MinimalRebuildFromTracking
        {
            get
            {
                return minimalRebuildFromTracking;
            }
            set
            {
                minimalRebuildFromTracking = value;
            }
        }

        public virtual bool AttributeFileTracking => false;

        [Output]
        public ITaskItem[] SourcesCompiled
        {
            get
            {
                return sourcesCompiled;
            }
            set
            {
                sourcesCompiled = value;
            }
        }

        public ITaskItem[] TrackedOutputFilesToIgnore
        {
            get
            {
                return trackedOutputFilesToIgnore;
            }
            set
            {
                trackedOutputFilesToIgnore = value;
            }
        }

        public ITaskItem[] TrackedInputFilesToIgnore
        {
            get
            {
                return trackedInputFilesToIgnore;
            }
            set
            {
                trackedInputFilesToIgnore = value;
            }
        }

        public bool DeleteOutputOnExecute
        {
            get
            {
                return deleteOutputBeforeExecute;
            }
            set
            {
                deleteOutputBeforeExecute = value;
            }
        }

        public bool DeleteOutputBeforeExecute
        {
            get
            {
                return deleteOutputBeforeExecute;
            }
            set
            {
                deleteOutputBeforeExecute = value;
            }
        }

        protected virtual bool MaintainCompositeRootingMarkers => false;

        protected virtual bool UseMinimalRebuildOptimization => false;

        public virtual string SourcesPropertyName => "Sources";

        // protected virtual ExecutableType? ToolType => null;

        public string ToolArchitecture { get; set; }

        public string TrackerFrameworkPath { get; set; }

        public string TrackerSdkPath { get; set; }

        public ITaskItem[] ExcludedInputPaths
        {
            get
            {
                return excludedInputPaths;
            }
            set
            {
                List<ITaskItem> list = new List<ITaskItem>(value);
                excludedInputPaths = list.ToArray();
            }
        }

        public string PathOverride
        {
            get
            {
                return pathOverride;
            }
            set
            {
                pathOverride = value;
            }
        }

        protected TrackedVCToolTask(System.Resources.ResourceManager taskResources)
            : base(taskResources)
        {
            PostBuildTrackingCleanup = true;
            EnableExecuteTool = true;
        }

        protected virtual void AssignDefaultTLogPaths()
        {
            string trackerIntermediateDirectory = TrackerIntermediateDirectory;
            if (TLogReadFiles == null)
            {
                string[] readTLogNames = ReadTLogNames;
                TLogReadFiles = new ITaskItem[readTLogNames.Length];
                for (int i = 0; i < readTLogNames.Length; i++)
                {
                    TLogReadFiles[i] = new TaskItem(Path.Combine(trackerIntermediateDirectory, readTLogNames[i]));
                }
            }
            if (TLogWriteFiles == null)
            {
                string[] writeTLogNames = WriteTLogNames;
                TLogWriteFiles = new ITaskItem[writeTLogNames.Length];
                for (int j = 0; j < writeTLogNames.Length; j++)
                {
                    TLogWriteFiles[j] = new TaskItem(Path.Combine(trackerIntermediateDirectory, writeTLogNames[j]));
                }
            }
            if (TLogDeleteFiles == null)
            {
                string[] deleteTLogNames = DeleteTLogNames;
                TLogDeleteFiles = new ITaskItem[deleteTLogNames.Length];
                for (int k = 0; k < deleteTLogNames.Length; k++)
                {
                    TLogDeleteFiles[k] = new TaskItem(Path.Combine(trackerIntermediateDirectory, deleteTLogNames[k]));
                }
            }
            if (TLogCommandFile == null)
            {
                TLogCommandFile = new TaskItem(Path.Combine(trackerIntermediateDirectory, CommandTLogName));
            }
        }

        protected override bool SkipTaskExecution()
        {
            return ComputeOutOfDateSources();
        }

        protected internal virtual bool ComputeOutOfDateSources()
        {
            if (MinimalRebuildFromTracking || TrackFileAccess)
            {
                AssignDefaultTLogPaths();
            }
            if (MinimalRebuildFromTracking && !ForcedRebuildRequired())
            {
                sourceOutputs = new CanonicalTrackedOutputFiles(this, TLogWriteFiles);
                sourceDependencies = new CanonicalTrackedInputFiles(this, TLogReadFiles, TrackedInputFiles, ExcludedInputPaths, sourceOutputs, UseMinimalRebuildOptimization, MaintainCompositeRootingMarkers);
                ITaskItem[] sourcesOutOfDateThroughTracking = SourceDependencies.ComputeSourcesNeedingCompilation(searchForSubRootsInCompositeRootingMarkers: false);
                List<ITaskItem> sourcesWithChangedCommandLines = GenerateSourcesOutOfDateDueToCommandLine();
                SourcesCompiled = MergeOutOfDateSourceLists(sourcesOutOfDateThroughTracking, sourcesWithChangedCommandLines);
                if (SourcesCompiled.Length == 0)
                {
                    SkippedExecution = true;
                    return SkippedExecution;
                }
                SourcesCompiled = AssignOutOfDateSources(SourcesCompiled);
                SourceDependencies.RemoveEntriesForSource(SourcesCompiled);
                SourceDependencies.SaveTlog();
                if (DeleteOutputOnExecute)
                {
                    DeleteFiles(sourceOutputs.OutputsForSource(SourcesCompiled, searchForSubRootsInCompositeRootingMarkers: false));
                }
                sourceOutputs.RemoveEntriesForSource(SourcesCompiled);
                sourceOutputs.SaveTlog();
            }
            else
            {
                SourcesCompiled = TrackedInputFiles;
                if (SourcesCompiled == null || SourcesCompiled.Length == 0)
                {
                    SkippedExecution = true;
                    return SkippedExecution;
                }
            }
            if ((TrackFileAccess || TrackCommandLines) && string.IsNullOrEmpty(RootSource))
            {
                RootSource = FileTracker.FormatRootingMarker(SourcesCompiled);
            }
            SkippedExecution = false;
            return SkippedExecution;
        }

        protected virtual ITaskItem[] AssignOutOfDateSources(ITaskItem[] sources)
        {
            return sources;
        }

        protected virtual bool ForcedRebuildRequired()
        {
            string text = null;
            try
            {
                text = TLogCommandFile.GetMetadata("FullPath");
            }
            catch (Exception ex)
            {
                if (!(ex is InvalidOperationException) && !(ex is NullReferenceException))
                {
                    throw;
                }
                base.Log.LogWarningWithCodeFromResources("TrackedVCToolTask.RebuildingDueToInvalidTLog", ex.Message);
                return true;
            }
            if (!File.Exists(text))
            {
                base.Log.LogMessageFromResources(MessageImportance.Low, "TrackedVCToolTask.RebuildingNoCommandTLog", TLogCommandFile.GetMetadata("FullPath"));
                return true;
            }
            return false;
        }

        protected virtual List<ITaskItem> GenerateSourcesOutOfDateDueToCommandLine()
        {
            IDictionary<string, string> dictionary = MapSourcesToCommandLines();
            List<ITaskItem> list = new List<ITaskItem>();
            if (!TrackCommandLines)
            {
                return list;
            }
            if (dictionary.Count == 0)
            {
                ITaskItem[] trackedInputFiles = TrackedInputFiles;
                foreach (ITaskItem item in trackedInputFiles)
                {
                    list.Add(item);
                }
            }
            else if (MaintainCompositeRootingMarkers)
            {
                string text = ApplyPrecompareCommandFilter(GenerateCommandLine(CommandLineFormat.ForTracking));
                string value = null;
                if (dictionary.TryGetValue(FileTracker.FormatRootingMarker(TrackedInputFiles), out value))
                {
                    value = ApplyPrecompareCommandFilter(value);
                    if (value == null || !text.Equals(value, StringComparison.Ordinal))
                    {
                        ITaskItem[] trackedInputFiles2 = TrackedInputFiles;
                        foreach (ITaskItem item2 in trackedInputFiles2)
                        {
                            list.Add(item2);
                        }
                    }
                }
                else
                {
                    ITaskItem[] trackedInputFiles3 = TrackedInputFiles;
                    foreach (ITaskItem item3 in trackedInputFiles3)
                    {
                        list.Add(item3);
                    }
                }
            }
            else
            {
                string text2 = SourcesPropertyName ?? "Sources";
                string text3 = GenerateCommandLineExceptSwitches(new string[1] { text2 }, CommandLineFormat.ForTracking);
                ITaskItem[] trackedInputFiles4 = TrackedInputFiles;
                foreach (ITaskItem taskItem in trackedInputFiles4)
                {
                    string text4 = ApplyPrecompareCommandFilter(text3 + " " + taskItem.GetMetadata("FullPath")/*.ToUpperInvariant()*/);
                    string value2 = null;
                    if (dictionary.TryGetValue(FileTracker.FormatRootingMarker(taskItem), out value2))
                    {
                        value2 = ApplyPrecompareCommandFilter(value2);
                        if (value2 == null || !text4.Equals(value2, StringComparison.Ordinal))
                        {
                            list.Add(taskItem);
                        }
                    }
                    else
                    {
                        list.Add(taskItem);
                    }
                }
            }
            return list;
        }

        protected ITaskItem[] MergeOutOfDateSourceLists(ITaskItem[] sourcesOutOfDateThroughTracking, List<ITaskItem> sourcesWithChangedCommandLines)
        {
            if (sourcesWithChangedCommandLines.Count == 0)
            {
                return sourcesOutOfDateThroughTracking;
            }
            if (sourcesOutOfDateThroughTracking.Length == 0)
            {
                if (sourcesWithChangedCommandLines.Count == TrackedInputFiles.Length)
                {
                    base.Log.LogMessageFromResources(MessageImportance.Low, "TrackedVCToolTask.RebuildingAllSourcesCommandLineChanged");
                }
                else
                {
                    foreach (ITaskItem sourcesWithChangedCommandLine in sourcesWithChangedCommandLines)
                    {
                        base.Log.LogMessageFromResources(MessageImportance.Low, "TrackedVCToolTask.RebuildingSourceCommandLineChanged", sourcesWithChangedCommandLine.GetMetadata("FullPath"));
                    }
                }
                return sourcesWithChangedCommandLines.ToArray();
            }
            if (sourcesOutOfDateThroughTracking.Length == TrackedInputFiles.Length)
            {
                return TrackedInputFiles;
            }
            if (sourcesWithChangedCommandLines.Count == TrackedInputFiles.Length)
            {
                base.Log.LogMessageFromResources(MessageImportance.Low, "TrackedVCToolTask.RebuildingAllSourcesCommandLineChanged");
                return TrackedInputFiles;
            }
            Dictionary<ITaskItem, bool> dictionary = new Dictionary<ITaskItem, bool>();
            foreach (ITaskItem key in sourcesOutOfDateThroughTracking)
            {
                dictionary[key] = false;
            }
            foreach (ITaskItem sourcesWithChangedCommandLine2 in sourcesWithChangedCommandLines)
            {
                if (!dictionary.ContainsKey(sourcesWithChangedCommandLine2))
                {
                    dictionary.Add(sourcesWithChangedCommandLine2, value: true);
                }
            }
            List<ITaskItem> list = new List<ITaskItem>();
            ITaskItem[] trackedInputFiles = TrackedInputFiles;
            foreach (ITaskItem taskItem in trackedInputFiles)
            {
                bool value = false;
                if (dictionary.TryGetValue(taskItem, out value))
                {
                    list.Add(taskItem);
                    if (value)
                    {
                        base.Log.LogMessageFromResources(MessageImportance.Low, "TrackedVCToolTask.RebuildingSourceCommandLineChanged", taskItem.GetMetadata("FullPath"));
                    }
                }
            }
            return list.ToArray();
        }

        protected IDictionary<string, string> MapSourcesToCommandLines()
        {
            IDictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string metadata = TLogCommandFile.GetMetadata("FullPath");
            if (File.Exists(metadata))
            {
                using (StreamReader streamReader = File.OpenText(metadata))
                {
                    bool flag = false;
                    string text = string.Empty;
                    for (string text2 = streamReader.ReadLine(); text2 != null; text2 = streamReader.ReadLine())
                    {
                        if (text2.Length == 0)
                        {
                            flag = true;
                            break;
                        }
                        if (text2[0] == '^')
                        {
                            if (text2.Length == 1)
                            {
                                flag = true;
                                break;
                            }
                            text = text2.Substring(1);
                        }
                        else
                        {
                            string value = null;
                            if (!dictionary.TryGetValue(text, out value))
                            {
                                dictionary[text] = text2;
                            }
                            else
                            {
                                IDictionary<string, string> dictionary2 = dictionary;
                                string key = text;
                                dictionary2[key] = dictionary2[key] + "\r\n" + text2;
                            }
                        }
                    }
                    if (flag)
                    {
                        base.Log.LogWarningWithCodeFromResources("TrackedVCToolTask.RebuildingDueToInvalidTLogContents", metadata);
                        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }
                    return dictionary;
                }
            }
            return dictionary;
        }

        protected void WriteSourcesToCommandLinesTable(IDictionary<string, string> sourcesToCommandLines)
        {
            string metadata = TLogCommandFile.GetMetadata("FullPath");
            Directory.CreateDirectory(Path.GetDirectoryName(metadata));
            using StreamWriter streamWriter = new StreamWriter(metadata, append: false, Encoding.Unicode);
            foreach (KeyValuePair<string, string> sourcesToCommandLine in sourcesToCommandLines)
            {
                streamWriter.WriteLine("^" + sourcesToCommandLine.Key);
                streamWriter.WriteLine(ApplyPrecompareCommandFilter(sourcesToCommandLine.Value));
            }
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            int num = 0;
            if (EnableExecuteTool)
            {
                try
                {
                    num = TrackerExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
                }
                finally
                {
                    PrintMessage(ParseLine(null), base.StandardOutputImportanceToUse);
                    if (PostBuildTrackingCleanup)
                    {
                        num = PostExecuteTool(num);
                    }
                }
            }
            return num;
        }

        protected virtual int PostExecuteTool(int exitCode)
        {
            if (MinimalRebuildFromTracking || TrackFileAccess)
            {
                SourceOutputs = new CanonicalTrackedOutputFiles(TLogWriteFiles);
                SourceDependencies = new CanonicalTrackedInputFiles(TLogReadFiles, TrackedInputFiles, ExcludedInputPaths, SourceOutputs, useMinimalRebuildOptimization: false, MaintainCompositeRootingMarkers);
                string[] array = null;
                IDictionary<string, string> dictionary = MapSourcesToCommandLines();
                if (exitCode != 0)
                {
                    SourceOutputs.RemoveEntriesForSource(SourcesCompiled);
                    SourceOutputs.SaveTlog();
                    SourceDependencies.RemoveEntriesForSource(SourcesCompiled);
                    SourceDependencies.SaveTlog();
                    if (TrackCommandLines)
                    {
                        if (MaintainCompositeRootingMarkers)
                        {
                            dictionary.Remove(RootSource);
                        }
                        else
                        {
                            ITaskItem[] array2 = SourcesCompiled;
                            foreach (ITaskItem source in array2)
                            {
                                dictionary.Remove(FileTracker.FormatRootingMarker(source));
                            }
                        }
                        WriteSourcesToCommandLinesTable(dictionary);
                    }
                }
                else
                {
                    AddTaskSpecificOutputs(SourcesCompiled, SourceOutputs);
                    RemoveTaskSpecificOutputs(SourceOutputs);
                    SourceOutputs.RemoveDependenciesFromEntryIfMissing(SourcesCompiled);
                    if (MaintainCompositeRootingMarkers)
                    {
                        array = SourceOutputs.RemoveRootsWithSharedOutputs(SourcesCompiled);
                        string[] array3 = array;
                        foreach (string rootingMarker in array3)
                        {
                            SourceDependencies.RemoveEntryForSourceRoot(rootingMarker);
                        }
                    }
                    if (TrackedOutputFilesToIgnore != null && TrackedOutputFilesToIgnore.Length != 0)
                    {
                        Dictionary<string, ITaskItem> trackedOutputFilesToRemove = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
                        ITaskItem[] array4 = TrackedOutputFilesToIgnore;
                        foreach (ITaskItem taskItem in array4)
                        {
                            string key = taskItem.GetMetadata("FullPath")/*.ToUpperInvariant()*/;
                            if (!trackedOutputFilesToRemove.ContainsKey(key))
                            {
                                trackedOutputFilesToRemove.Add(key, taskItem);
                            }
                        }
                        SourceOutputs.SaveTlog((string fullTrackedPath) => (!trackedOutputFilesToRemove.ContainsKey(fullTrackedPath/*.ToUpperInvariant()*/)) ? true : false);
                    }
                    else
                    {
                        SourceOutputs.SaveTlog();
                    }
                    DeleteEmptyFile(TLogWriteFiles);
                    RemoveTaskSpecificInputs(SourceDependencies);
                    SourceDependencies.RemoveDependenciesFromEntryIfMissing(SourcesCompiled);
                    if (TrackedInputFilesToIgnore != null && TrackedInputFilesToIgnore.Length != 0)
                    {
                        Dictionary<string, ITaskItem> trackedInputFilesToRemove = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
                        ITaskItem[] array5 = TrackedInputFilesToIgnore;
                        foreach (ITaskItem taskItem2 in array5)
                        {
                            string key2 = taskItem2.GetMetadata("FullPath")/*.ToUpperInvariant()*/;
                            if (!trackedInputFilesToRemove.ContainsKey(key2))
                            {
                                trackedInputFilesToRemove.Add(key2, taskItem2);
                            }
                        }
                        SourceDependencies.SaveTlog((string fullTrackedPath) => (!trackedInputFilesToRemove.ContainsKey(fullTrackedPath)) ? true : false);
                    }
                    else
                    {
                        SourceDependencies.SaveTlog();
                    }
                    DeleteEmptyFile(TLogReadFiles);
                    DeleteFiles(TLogDeleteFiles);
                    if (TrackCommandLines)
                    {
                        if (MaintainCompositeRootingMarkers)
                        {
                            string value = GenerateCommandLine(CommandLineFormat.ForTracking);
                            dictionary[RootSource] = value;
                            if (array != null)
                            {
                                string[] array6 = array;
                                foreach (string key3 in array6)
                                {
                                    dictionary.Remove(key3);
                                }
                            }
                        }
                        else
                        {
                            string text = SourcesPropertyName ?? "Sources";
                            string text2 = GenerateCommandLineExceptSwitches(new string[1] { text }, CommandLineFormat.ForTracking);
                            ITaskItem[] array7 = SourcesCompiled;
                            foreach (ITaskItem taskItem3 in array7)
                            {
                                dictionary[FileTracker.FormatRootingMarker(taskItem3)] = text2 + " " + taskItem3.GetMetadata("FullPath")/*.ToUpperInvariant()*/;
                            }
                        }
                        WriteSourcesToCommandLinesTable(dictionary);
                    }
                }
            }
            return exitCode;
        }

        protected virtual void RemoveTaskSpecificOutputs(CanonicalTrackedOutputFiles compactOutputs)
        {
        }

        protected virtual void RemoveTaskSpecificInputs(CanonicalTrackedInputFiles compactInputs)
        {
        }

        protected virtual void AddTaskSpecificOutputs(ITaskItem[] sources, CanonicalTrackedOutputFiles compactOutputs)
        {
        }

        protected override void LogPathToTool(string toolName, string pathToTool)
        {
            base.LogPathToTool(toolName, base.ResolvedPathToTool);
        }

        protected virtual void SaveTracking()
        {
            // 微软没有此函数，自己重写的版本，保存跟踪文件，增量编译使用。
        }

        protected int TrackerExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            string dllName = null;
            string text = null;
            bool flag = TrackFileAccess;
            string text2 = Environment.ExpandEnvironmentVariables(pathToTool);
            string text3 = Environment.ExpandEnvironmentVariables(commandLineCommands);

            // 微软的方案严重不适合Linux，因为tracker什么的再Linux等非Windows 平台都是没有的。
            // 因此这方面重写。

            var ErrorCode = base.ExecuteTool(text2, responseFileCommands, text3);

            if(ErrorCode == 0 && (MinimalRebuildFromTracking || TrackFileAccess))
            {
                // 将数据生成数据会回写到Write文件。
                SaveTracking();
            }

            return ErrorCode;
#if __
            try
            {
                string text4;
                if (flag)
                {
                    ExecutableType result = ExecutableType.SameAsCurrentProcess;
                    if (!string.IsNullOrEmpty(ToolArchitecture))
                    {
                        if (!Enum.TryParse<ExecutableType>(ToolArchitecture, out result))
                        {
                            base.Log.LogErrorWithCodeFromResources("General.InvalidValue", "ToolArchitecture", GetType().Name);
                            return -1;
                        }
                    }
                    else if (ToolType.HasValue)
                    {
                        result = ToolType.Value;
                    }
                    if ((result == ExecutableType.Native32Bit || result == ExecutableType.Native64Bit) && Microsoft.Build.Shared.NativeMethodsShared.Is64bitApplication(text2, out var is64bit))
                    {
                        result = (is64bit ? ExecutableType.Native64Bit : ExecutableType.Native32Bit);
                    }
                    try
                    {
                        text4 = FileTracker.GetTrackerPath(result, TrackerSdkPath);
                        if (text4 == null)
                        {
                            base.Log.LogErrorFromResources("Error.MissingFile", "tracker.exe");
                        }
                    }
                    catch (Exception e)
                    {
                        if (Microsoft.Build.Shared.ExceptionHandling.NotExpectedException(e))
                        {
                            throw;
                        }
                        base.Log.LogErrorWithCodeFromResources("General.InvalidValue", "TrackerSdkPath", GetType().Name);
                        return -1;
                    }
                    try
                    {
                        dllName = FileTracker.GetFileTrackerPath(result, TrackerFrameworkPath);
                    }
                    catch (Exception e2)
                    {
                        if (Microsoft.Build.Shared.ExceptionHandling.NotExpectedException(e2))
                        {
                            throw;
                        }
                        base.Log.LogErrorWithCodeFromResources("General.InvalidValue", "TrackerFrameworkPath", GetType().Name);
                        return -1;
                    }
                }
                else
                {
                    text4 = text2;
                }
                if (!string.IsNullOrEmpty(text4))
                {
                    Microsoft.Build.Shared.ErrorUtilities.VerifyThrowInternalRooted(text4);
                    string commandLineCommands2;
                    if (flag)
                    {
                        string text5 = FileTracker.TrackerArguments(text2, text3, dllName, TrackerIntermediateDirectory, RootSource, base.CancelEventName);
                        base.Log.LogMessageFromResources(MessageImportance.Low, "Native_TrackingCommandMessage");
                        string message = text4 + (AttributeFileTracking ? " /a " : " ") + (TrackReplaceFile ? "/f " : "") + text5 + " " + responseFileCommands;
                        base.Log.LogMessage(MessageImportance.Low, message);
                        text = Microsoft.Build.Shared.FileUtilities.GetTemporaryFile();
                        using (StreamWriter streamWriter = new StreamWriter(text, append: false, Encoding.Unicode))
                        {
                            streamWriter.Write(FileTracker.TrackerResponseFileArguments(dllName, TrackerIntermediateDirectory, RootSource, base.CancelEventName));
                        }
                        commandLineCommands2 = (AttributeFileTracking ? "/a @\"" : "@\"") + text + "\"" + (TrackReplaceFile ? " /f " : "") + FileTracker.TrackerCommandArguments(text2, text3);
                    }
                    else
                    {
                        commandLineCommands2 = text3;
                    }
                    return base.ExecuteTool(text4, responseFileCommands, commandLineCommands2);
                }
                return -1;
            }
            finally
            {
                if (text != null)
                {
                    DeleteTempFile(text);
                }
            }
#endif
        }

        protected override void ProcessStarted()
        {
        }

        public virtual string ApplyPrecompareCommandFilter(string value)
        {
            return extraNewlineRegex.Replace(value, "$2");
        }

        public static string RemoveSwitchFromCommandLine(string removalWord, string cmdString, bool removeMultiple = false)
        {
            int num = 0;
            while ((num = cmdString.IndexOf(removalWord, num, StringComparison.Ordinal)) >= 0)
            {
                if (num == 0 || cmdString[num - 1] == ' ')
                {
                    int num2 = cmdString.IndexOf(' ', num);
                    if (num2 >= 0)
                    {
                        num2++;
                    }
                    else
                    {
                        num2 = cmdString.Length;
                        num--;
                    }
                    cmdString = cmdString.Remove(num, num2 - num);
                    if (!removeMultiple)
                    {
                        break;
                    }
                }
                num++;
                if (num >= cmdString.Length)
                {
                    break;
                }
            }
            return cmdString;
        }

        protected static int DeleteFiles(ITaskItem[] filesToDelete)
        {
            if (filesToDelete == null)
            {
                return 0;
            }
            ITaskItem[] array = TrackedDependencies.ExpandWildcards(filesToDelete);
            if (array.Length == 0)
            {
                return 0;
            }
            int num = 0;
            ITaskItem[] array2 = array;
            foreach (ITaskItem taskItem in array2)
            {
                try
                {
                    FileInfo fileInfo = new FileInfo(taskItem.ItemSpec);
                    if (fileInfo.Exists)
                    {
                        fileInfo.Delete();
                        num++;
                    }
                }
                catch (Exception ex)
                {
                    if (ex is SecurityException || ex is ArgumentException || ex is UnauthorizedAccessException || ex is PathTooLongException || ex is NotSupportedException)
                    {
                        continue;
                    }
                    throw;
                }
            }
            return num;
        }

        protected static int DeleteEmptyFile(ITaskItem[] filesToDelete)
        {
            if (filesToDelete == null)
            {
                return 0;
            }
            ITaskItem[] array = TrackedDependencies.ExpandWildcards(filesToDelete);
            if (array.Length == 0)
            {
                return 0;
            }
            int num = 0;
            ITaskItem[] array2 = array;
            foreach (ITaskItem taskItem in array2)
            {
                bool flag = false;
                try
                {
                    FileInfo fileInfo = new FileInfo(taskItem.ItemSpec);
                    if (fileInfo.Exists)
                    {
                        if (fileInfo.Length <= 4)
                        {
                            flag = true;
                        }
                        if (flag)
                        {
                            fileInfo.Delete();
                            num++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (ex is SecurityException || ex is ArgumentException || ex is UnauthorizedAccessException || ex is PathTooLongException || ex is NotSupportedException)
                    {
                        continue;
                    }
                    throw;
                }
            }
            return num;
        }
    }

}
