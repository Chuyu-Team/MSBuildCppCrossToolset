using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.Utilities
{
    public class CanonicalTrackedInputFiles
    {
        private DateTime _outputNewestTime = DateTime.MinValue;

        private ITaskItem[] _tlogFiles;

        private ITaskItem[] _sourceFiles;

        private TaskLoggingHelper _log;

        private CanonicalTrackedOutputFiles _outputs;

        private ITaskItem[] _outputFileGroup;

        private ITaskItem[] _outputFiles;

        private bool _useMinimalRebuildOptimization;

        private bool _tlogAvailable;

        private bool _maintainCompositeRootingMarkers;

        private readonly HashSet<string> _excludedInputPaths = new HashSet<string>(StringComparer.Ordinal);

        private readonly ConcurrentDictionary<string, DateTime> _lastWriteTimeCache = new ConcurrentDictionary<string, DateTime>(StringComparer.Ordinal);

        internal ITaskItem[] SourcesNeedingCompilation { get; set; }

        public Dictionary<string, Dictionary<string, string>> DependencyTable { get; private set; }

        public CanonicalTrackedInputFiles(ITaskItem[] tlogFiles, ITaskItem[] sourceFiles, CanonicalTrackedOutputFiles outputs, bool useMinimalRebuildOptimization, bool maintainCompositeRootingMarkers)
        {
            InternalConstruct(null, tlogFiles, sourceFiles, null, null, outputs, useMinimalRebuildOptimization, maintainCompositeRootingMarkers);
        }

        public CanonicalTrackedInputFiles(ITaskItem[] tlogFiles, ITaskItem[] sourceFiles, ITaskItem[] excludedInputPaths, CanonicalTrackedOutputFiles outputs, bool useMinimalRebuildOptimization, bool maintainCompositeRootingMarkers)
        {
            InternalConstruct(null, tlogFiles, sourceFiles, null, excludedInputPaths, outputs, useMinimalRebuildOptimization, maintainCompositeRootingMarkers);
        }

        public CanonicalTrackedInputFiles(ITask ownerTask, ITaskItem[] tlogFiles, ITaskItem[] sourceFiles, ITaskItem[] excludedInputPaths, CanonicalTrackedOutputFiles outputs, bool useMinimalRebuildOptimization, bool maintainCompositeRootingMarkers)
        {
            InternalConstruct(ownerTask, tlogFiles, sourceFiles, null, excludedInputPaths, outputs, useMinimalRebuildOptimization, maintainCompositeRootingMarkers);
        }

        public CanonicalTrackedInputFiles(ITask ownerTask, ITaskItem[] tlogFiles, ITaskItem[] sourceFiles, ITaskItem[] excludedInputPaths, ITaskItem[] outputs, bool useMinimalRebuildOptimization, bool maintainCompositeRootingMarkers)
        {
            InternalConstruct(ownerTask, tlogFiles, sourceFiles, outputs, excludedInputPaths, null, useMinimalRebuildOptimization, maintainCompositeRootingMarkers);
        }

        public CanonicalTrackedInputFiles(ITask ownerTask, ITaskItem[] tlogFiles, ITaskItem sourceFile, ITaskItem[] excludedInputPaths, CanonicalTrackedOutputFiles outputs, bool useMinimalRebuildOptimization, bool maintainCompositeRootingMarkers)
        {
            InternalConstruct(ownerTask, tlogFiles, new ITaskItem[1] { sourceFile }, null, excludedInputPaths, outputs, useMinimalRebuildOptimization, maintainCompositeRootingMarkers);
        }

        private void InternalConstruct(ITask ownerTask, ITaskItem[] tlogFiles, ITaskItem[] sourceFiles, ITaskItem[] outputFiles, ITaskItem[] excludedInputPaths, CanonicalTrackedOutputFiles outputs, bool useMinimalRebuildOptimization, bool maintainCompositeRootingMarkers)
        {
            if (ownerTask != null)
            {
                _log = new TaskLoggingHelper(ownerTask)
                {
                    TaskResources = Microsoft.Build.CppTasks.Common.Properties.Microsoft_Build_CPPTasks_Strings.ResourceManager,
                    HelpKeywordPrefix = "MSBuild."
                };
            }
            _tlogFiles = TrackedDependencies.ExpandWildcards(tlogFiles);
            _tlogAvailable = TrackedDependencies.ItemsExist(_tlogFiles);
            _sourceFiles = sourceFiles;
            _outputs = outputs;
            _outputFiles = outputFiles;
            _useMinimalRebuildOptimization = useMinimalRebuildOptimization;
            _maintainCompositeRootingMarkers = maintainCompositeRootingMarkers;
            if (excludedInputPaths != null)
            {
                for (int i = 0; i < excludedInputPaths.Length; i++)
                {
                    string item = FileUtilities.EnsureNoTrailingSlash(FileUtilities.NormalizePath(excludedInputPaths[i].ItemSpec))/*.ToUpperInvariant()*/;
                    _excludedInputPaths.Add(item);
                }
            }
            DependencyTable = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
            if (_tlogFiles != null)
            {
                ConstructDependencyTable();
            }
        }

        public ITaskItem[] ComputeSourcesNeedingCompilation()
        {
            return ComputeSourcesNeedingCompilation(searchForSubRootsInCompositeRootingMarkers: true);
        }

        public ITaskItem[] ComputeSourcesNeedingCompilation(bool searchForSubRootsInCompositeRootingMarkers)
        {
            if (_outputFiles != null)
            {
                _outputFileGroup = _outputFiles;
            }
            else if (_sourceFiles != null && _outputs != null && _maintainCompositeRootingMarkers)
            {
                _outputFileGroup = _outputs.OutputsForSource(_sourceFiles, searchForSubRootsInCompositeRootingMarkers);
            }
            else if (_sourceFiles != null && _outputs != null)
            {
                _outputFileGroup = _outputs.OutputsForNonCompositeSource(_sourceFiles);
            }
            if (!_maintainCompositeRootingMarkers)
            {
                return ComputeSourcesNeedingCompilationFromPrimaryFiles();
            }
            return ComputeSourcesNeedingCompilationFromCompositeRootingMarker(searchForSubRootsInCompositeRootingMarkers);
        }

        private ITaskItem[] ComputeSourcesNeedingCompilationFromPrimaryFiles()
        {
            if (SourcesNeedingCompilation == null)
            {
                ConcurrentQueue<ITaskItem> sourcesNeedingCompilationList = new ConcurrentQueue<ITaskItem>();
                bool allOutputFilesExist = false;
                if (_tlogAvailable && !_useMinimalRebuildOptimization)
                {
                    allOutputFilesExist = FilesExistAndRecordNewestWriteTime(_outputFileGroup);
                }
                Parallel.For(0, _sourceFiles.Length, delegate (int index)
                {
                    CheckIfSourceNeedsCompilation(sourcesNeedingCompilationList, allOutputFilesExist, _sourceFiles[index]);
                });
                SourcesNeedingCompilation = sourcesNeedingCompilationList.ToArray();
            }
            if (SourcesNeedingCompilation.Length == 0)
            {
                FileTracker.LogMessageFromResources(_log, MessageImportance.Normal, "Tracking_AllOutputsAreUpToDate");
                SourcesNeedingCompilation = Array.Empty<ITaskItem>();
            }
            else
            {
                Array.Sort(SourcesNeedingCompilation, CompareTaskItems);
                ITaskItem[] sourcesNeedingCompilation = SourcesNeedingCompilation;
                foreach (ITaskItem taskItem in sourcesNeedingCompilation)
                {
                    string metadata = taskItem.GetMetadata("_trackerModifiedPath");
                    string metadata2 = taskItem.GetMetadata("_trackerModifiedTime");
                    string metadata3 = taskItem.GetMetadata("_trackerOutputFile");
                    string metadata4 = taskItem.GetMetadata("_trackerCompileReason");
                    if (string.Equals(metadata4, "Tracking_SourceWillBeCompiledDependencyWasModifiedAt", StringComparison.Ordinal))
                    {
                        FileTracker.LogMessageFromResources(_log, MessageImportance.Low, metadata4, taskItem.ItemSpec, metadata, metadata2);
                    }
                    else if (string.Equals(metadata4, "Tracking_SourceWillBeCompiledMissingDependency", StringComparison.Ordinal))
                    {
                        FileTracker.LogMessageFromResources(_log, MessageImportance.Low, metadata4, taskItem.ItemSpec, metadata);
                    }
                    else if (string.Equals(metadata4, "Tracking_SourceWillBeCompiledOutputDoesNotExist", StringComparison.Ordinal))
                    {
                        FileTracker.LogMessageFromResources(_log, MessageImportance.Low, metadata4, taskItem.ItemSpec, metadata3);
                    }
                    else
                    {
                        FileTracker.LogMessageFromResources(_log, MessageImportance.Low, metadata4, taskItem.ItemSpec);
                    }
                    taskItem.RemoveMetadata("_trackerModifiedPath");
                    taskItem.RemoveMetadata("_trackerModifiedTime");
                    taskItem.RemoveMetadata("_trackerOutputFile");
                    taskItem.RemoveMetadata("_trackerCompileReason");
                }
            }
            return SourcesNeedingCompilation;
        }

        private void CheckIfSourceNeedsCompilation(ConcurrentQueue<ITaskItem> sourcesNeedingCompilationList, bool allOutputFilesExist, ITaskItem source)
        {
            if (!_tlogAvailable || _outputFileGroup == null)
            {
                source.SetMetadata("_trackerCompileReason", "Tracking_SourceWillBeCompiledAsNoTrackingLog");
                sourcesNeedingCompilationList.Enqueue(source);
            }
            else if (!_useMinimalRebuildOptimization && !allOutputFilesExist)
            {
                source.SetMetadata("_trackerCompileReason", "Tracking_SourceOutputsNotAvailable");
                sourcesNeedingCompilationList.Enqueue(source);
            }
            else if (!IsUpToDate(source))
            {
                if (string.IsNullOrEmpty(source.GetMetadata("_trackerCompileReason")))
                {
                    source.SetMetadata("_trackerCompileReason", "Tracking_SourceWillBeCompiled");
                }
                sourcesNeedingCompilationList.Enqueue(source);
            }
            else if (!_useMinimalRebuildOptimization && _outputNewestTime == DateTime.MinValue)
            {
                source.SetMetadata("_trackerCompileReason", "Tracking_SourceNotInTrackingLog");
                sourcesNeedingCompilationList.Enqueue(source);
            }
        }

        private static int CompareTaskItems(ITaskItem left, ITaskItem right)
        {
            return string.Compare(left.ItemSpec, right.ItemSpec, StringComparison.Ordinal);
        }

        private ITaskItem[] ComputeSourcesNeedingCompilationFromCompositeRootingMarker(bool searchForSubRootsInCompositeRootingMarkers)
        {
            if (!_tlogAvailable)
            {
                return _sourceFiles;
            }
            Dictionary<string, ITaskItem> dictionary = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
            string text = FileTracker.FormatRootingMarker(_sourceFiles);
            List<ITaskItem> list = new List<ITaskItem>();
            foreach (string key in DependencyTable.Keys)
            {
                string text2 = key/*.ToUpperInvariant()*/;
                if (searchForSubRootsInCompositeRootingMarkers)
                {
                    if (text2.Contains(text) || CanonicalTrackedFilesHelper.RootContainsAllSubRootComponents(text, text2))
                    {
                        SourceDependenciesForOutputRoot(dictionary, text2, _outputFileGroup);
                    }
                }
                else if (text2.Equals(text, StringComparison.Ordinal))
                {
                    SourceDependenciesForOutputRoot(dictionary, text2, _outputFileGroup);
                }
            }
            if (dictionary.Count == 0)
            {
                FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_DependenciesForRootNotFound", text);
                return _sourceFiles;
            }
            list.AddRange(dictionary.Values);
            string outputOldestFilename = string.Empty;
            if (CanonicalTrackedFilesHelper.FilesExistAndRecordNewestWriteTime(list, _log, out var outputNewestTime, out var outputNewestFilename) && CanonicalTrackedFilesHelper.FilesExistAndRecordOldestWriteTime(_outputFileGroup, _log, out var outputOldestTime, out outputOldestFilename) && outputNewestTime <= outputOldestTime)
            {
                FileTracker.LogMessageFromResources(_log, MessageImportance.Normal, "Tracking_AllOutputsAreUpToDate");
                return Array.Empty<ITaskItem>();
            }
            if (dictionary.Count > 100)
            {
                FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_InputsNotShown", dictionary.Count);
            }
            else
            {
                FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_InputsFor", text);
                foreach (ITaskItem item in list)
                {
                    FileTracker.LogMessage(_log, MessageImportance.Low, "\t" + item);
                }
            }
            FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_InputNewerThanOutput", outputNewestFilename, outputOldestFilename);
            return _sourceFiles;
        }

        private void SourceDependenciesForOutputRoot(Dictionary<string, ITaskItem> sourceDependencies, string sourceKey, ITaskItem[] filesToIgnore)
        {
            bool flag = filesToIgnore != null && filesToIgnore.Length != 0;
            if (!DependencyTable.TryGetValue(sourceKey, out var value))
            {
                return;
            }
            foreach (string key in value.Keys)
            {
                bool flag2 = false;
                if (flag)
                {
                    foreach (ITaskItem taskItem in filesToIgnore)
                    {
                        if (string.Equals(key, taskItem.ItemSpec, StringComparison.OrdinalIgnoreCase))
                        {
                            flag2 = true;
                            break;
                        }
                    }
                }
                if (!flag2 && !sourceDependencies.TryGetValue(key, out var _))
                {
                    sourceDependencies.Add(key, new TaskItem(key));
                }
            }
        }

        private bool IsUpToDate(ITaskItem sourceFile)
        {
            string text = FileUtilities.NormalizePath(sourceFile.ItemSpec);
            Dictionary<string, string> value;
            bool flag = DependencyTable.TryGetValue(text, out value);
            DateTime dateTime = _outputNewestTime;
            if (_useMinimalRebuildOptimization && _outputs != null && flag)
            {
                dateTime = DateTime.MinValue;
                if (!_outputs.DependencyTable.TryGetValue(text, out var value2))
                {
                    sourceFile.SetMetadata("_trackerCompileReason", "Tracking_SourceOutputsNotAvailable");
                    return false;
                }
                DateTime lastWriteFileUtcTime = NativeMethods.GetLastWriteFileUtcTime(text);
                foreach (string key in value2.Keys)
                {
                    DateTime lastWriteFileUtcTime2 = NativeMethods.GetLastWriteFileUtcTime(key);
                    if (lastWriteFileUtcTime2 > DateTime.MinValue)
                    {
                        if (lastWriteFileUtcTime2 < lastWriteFileUtcTime)
                        {
                            sourceFile.SetMetadata("_trackerCompileReason", "Tracking_SourceWillBeCompiledDependencyWasModifiedAt");
                            sourceFile.SetMetadata("_trackerModifiedPath", text);
                            sourceFile.SetMetadata("_trackerModifiedTime", lastWriteFileUtcTime.ToLocalTime().ToString());
                            return false;
                        }
                        if (lastWriteFileUtcTime2 > dateTime)
                        {
                            dateTime = lastWriteFileUtcTime2;
                        }
                        continue;
                    }
                    sourceFile.SetMetadata("_trackerCompileReason", "Tracking_SourceWillBeCompiledOutputDoesNotExist");
                    sourceFile.SetMetadata("_trackerOutputFile", key);
                    return false;
                }
            }
            if (flag)
            {
                foreach (string key2 in value.Keys)
                {
                    if (!FileIsExcludedFromDependencyCheck(key2))
                    {
                        if (!_lastWriteTimeCache.TryGetValue(key2, out var value3))
                        {
                            value3 = NativeMethods.GetLastWriteFileUtcTime(key2);
                            _lastWriteTimeCache[key2] = value3;
                        }
                        if (!(value3 > DateTime.MinValue))
                        {
                            sourceFile.SetMetadata("_trackerCompileReason", "Tracking_SourceWillBeCompiledMissingDependency");
                            sourceFile.SetMetadata("_trackerModifiedPath", key2);
                            return false;
                        }
                        if (value3 > dateTime)
                        {
                            sourceFile.SetMetadata("_trackerCompileReason", "Tracking_SourceWillBeCompiledDependencyWasModifiedAt");
                            sourceFile.SetMetadata("_trackerModifiedPath", key2);
                            sourceFile.SetMetadata("_trackerModifiedTime", value3.ToLocalTime().ToString());
                            return false;
                        }
                    }
                }
                return true;
            }
            sourceFile.SetMetadata("_trackerCompileReason", "Tracking_SourceNotInTrackingLog");
            return false;
        }

        public bool FileIsExcludedFromDependencyCheck(string fileName)
        {
            string directoryNameOfFullPath = FileUtilities.GetDirectoryNameOfFullPath(fileName);
            return _excludedInputPaths.Contains(directoryNameOfFullPath);
        }

        private bool FilesExistAndRecordNewestWriteTime(ITaskItem[] files)
        {
            string outputNewestFilename;
            return CanonicalTrackedFilesHelper.FilesExistAndRecordNewestWriteTime(files, _log, out _outputNewestTime, out outputNewestFilename);
        }

        private void ConstructDependencyTable()
        {
            string text;
            try
            {
                text = DependencyTableCache.FormatNormalizedTlogRootingMarker(_tlogFiles);
            }
            catch (ArgumentException ex)
            {
                FileTracker.LogWarningWithCodeFromResources(_log, "Tracking_RebuildingDueToInvalidTLog", ex.Message);
                return;
            }
            string path = FileUtilities.EnsureTrailingSlash(Directory.GetCurrentDirectory());
            ITaskItem[] tlogFiles;
            if (!_tlogAvailable)
            {
                tlogFiles = _tlogFiles;
                foreach (ITaskItem taskItem in tlogFiles)
                {
                    if (!FileUtilities.FileExistsNoThrow(taskItem.ItemSpec))
                    {
                        FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_SingleLogFileNotAvailable", taskItem.ItemSpec);
                    }
                }
                lock (DependencyTableCache.DependencyTable)
                {
                    DependencyTableCache.DependencyTable.Remove(text);
                    return;
                }
            }
            DependencyTableCacheEntry cachedEntry;
            lock (DependencyTableCache.DependencyTable)
            {
                cachedEntry = DependencyTableCache.GetCachedEntry(text);
            }
            if (cachedEntry != null)
            {
                DependencyTable = (Dictionary<string, Dictionary<string, string>>)cachedEntry.DependencyTable;
                FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_ReadTrackingCached");
                tlogFiles = cachedEntry.TlogFiles;
                foreach (ITaskItem taskItem2 in tlogFiles)
                {
                    FileTracker.LogMessage(_log, MessageImportance.Low, "\t{0}", taskItem2.ItemSpec);
                }
                return;
            }
            bool flag = false;
            bool flag2 = false;
            string text2 = null;
            FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_ReadTrackingLogs");
            tlogFiles = _tlogFiles;
            foreach (ITaskItem taskItem3 in tlogFiles)
            {
                try
                {
                    FileTracker.LogMessage(_log, MessageImportance.Low, "\t{0}", taskItem3.ItemSpec);
                    using StreamReader streamReader = File.OpenText(taskItem3.ItemSpec);
                    string text3 = streamReader.ReadLine();
                    while (text3 != null)
                    {
                        if (text3.Length == 0)
                        {
                            flag = true;
                            text2 = taskItem3.ItemSpec;
                            break;
                        }
                        if (text3[0] != '#')
                        {
                            bool flag3 = false;
                            if (text3[0] == '^')
                            {
                                text3 = text3.Substring(1);
                                if (text3.Length == 0)
                                {
                                    flag = true;
                                    text2 = taskItem3.ItemSpec;
                                    break;
                                }
                                flag3 = true;
                            }
                            if (flag3)
                            {
                                Dictionary<string, string> dictionary;
                                if (!_maintainCompositeRootingMarkers)
                                {
                                    dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                    if (text3.Contains("|"))
                                    {
                                        ITaskItem[] sourceFiles = _sourceFiles;
                                        foreach (ITaskItem taskItem4 in sourceFiles)
                                        {
                                            if (!dictionary.ContainsKey(FileUtilities.NormalizePath(taskItem4.ItemSpec)))
                                            {
                                                dictionary.Add(FileUtilities.NormalizePath(taskItem4.ItemSpec), null);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        dictionary.Add(text3, null);
                                    }
                                }
                                else
                                {
                                    dictionary = null;
                                }
                                if (!DependencyTable.TryGetValue(text3, out var value))
                                {
                                    value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                                    if (!_maintainCompositeRootingMarkers)
                                    {
                                        value.Add(text3, null);
                                    }
                                    DependencyTable.Add(text3, value);
                                }
                                text3 = streamReader.ReadLine();
                                if (_maintainCompositeRootingMarkers)
                                {
                                    while (text3 != null)
                                    {
                                        if (text3.Length == 0)
                                        {
                                            flag = true;
                                            text2 = taskItem3.ItemSpec;
                                            break;
                                        }
                                        if (text3[0] == '#' || text3[0] == '^')
                                        {
                                            break;
                                        }
                                        if (!value.ContainsKey(text3) && (FileTracker.FileIsUnderPath(text3, path) || !FileTracker.FileIsExcludedFromDependencies(text3)))
                                        {
                                            value.Add(text3, null);
                                        }
                                        text3 = streamReader.ReadLine();
                                    }
                                    continue;
                                }
                                while (text3 != null)
                                {
                                    if (text3.Length == 0)
                                    {
                                        flag = true;
                                        text2 = taskItem3.ItemSpec;
                                        break;
                                    }
                                    if (text3[0] == '#' || text3[0] == '^')
                                    {
                                        break;
                                    }
                                    if (dictionary.ContainsKey(text3))
                                    {
                                        if (!DependencyTable.TryGetValue(text3, out value))
                                        {
                                            value = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { { text3, null } };
                                            DependencyTable.Add(text3, value);
                                        }
                                    }
                                    else if (!value.ContainsKey(text3) && (FileTracker.FileIsUnderPath(text3, path) || !FileTracker.FileIsExcludedFromDependencies(text3)))
                                    {
                                        value.Add(text3, null);
                                    }
                                    text3 = streamReader.ReadLine();
                                }
                            }
                            else
                            {
                                text3 = streamReader.ReadLine();
                            }
                        }
                        else
                        {
                            text3 = streamReader.ReadLine();
                        }
                    }
                }
                catch (Exception ex2) when (ExceptionHandling.IsIoRelatedException(ex2))
                {
                    FileTracker.LogWarningWithCodeFromResources(_log, "Tracking_RebuildingDueToInvalidTLog", ex2.Message);
                    break;
                }
                if (flag)
                {
                    FileTracker.LogWarningWithCodeFromResources(_log, "Tracking_RebuildingDueToInvalidTLogContents", text2);
                    break;
                }
            }
            lock (DependencyTableCache.DependencyTable)
            {
                if (flag || flag2)
                {
                    DependencyTableCache.DependencyTable.Remove(text);
                    DependencyTable = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    DependencyTableCache.DependencyTable[text] = new DependencyTableCacheEntry(_tlogFiles, DependencyTable);
                }
            }
        }

        public void SaveTlog()
        {
            SaveTlog(null);
        }

        public void SaveTlog(DependencyFilter includeInTLog)
        {
            ITaskItem[] tlogFiles = _tlogFiles;
            if (tlogFiles == null || tlogFiles.Length == 0)
            {
                return;
            }
            string key = DependencyTableCache.FormatNormalizedTlogRootingMarker(_tlogFiles);
            lock (DependencyTableCache.DependencyTable)
            {
                DependencyTableCache.DependencyTable.Remove(key);
            }
            string itemSpec = _tlogFiles[0].ItemSpec;
            ITaskItem[] tlogFiles2 = _tlogFiles;
            for (int i = 0; i < tlogFiles2.Length; i++)
            {
                File.WriteAllText(tlogFiles2[i].ItemSpec, "", Encoding.Unicode);
            }
            using StreamWriter streamWriter = FileUtilities.OpenWrite(itemSpec, append: false, Encoding.Unicode);
            if (!_maintainCompositeRootingMarkers)
            {
                foreach (string key2 in DependencyTable.Keys)
                {
                    if (!key2.Contains("|"))
                    {
                        Dictionary<string, string> dictionary = DependencyTable[key2];
                        streamWriter.WriteLine("^" + key2);
                        foreach (string key3 in dictionary.Keys)
                        {
                            if (key3 != key2 && (includeInTLog == null || includeInTLog(key3)))
                            {
                                streamWriter.WriteLine(key3);
                            }
                        }
                    }
                }
                return;
            }
            foreach (string key4 in DependencyTable.Keys)
            {
                Dictionary<string, string> dictionary2 = DependencyTable[key4];
                streamWriter.WriteLine("^" + key4);
                foreach (string key5 in dictionary2.Keys)
                {
                    if (includeInTLog == null || includeInTLog(key5))
                    {
                        streamWriter.WriteLine(key5);
                    }
                }
            }
        }

        public void RemoveEntriesForSource(ITaskItem source)
        {
            RemoveEntriesForSource(new ITaskItem[1] { source });
        }

        public void RemoveEntriesForSource(ITaskItem[] source)
        {
            string key = FileTracker.FormatRootingMarker(source);
            DependencyTable.Remove(key);
            foreach (ITaskItem taskItem in source)
            {
                DependencyTable.Remove(FileUtilities.NormalizePath(taskItem.ItemSpec));
            }
        }

        public void RemoveEntryForSourceRoot(string rootingMarker)
        {
            DependencyTable.Remove(rootingMarker);
        }

        public void RemoveDependencyFromEntry(ITaskItem[] sources, ITaskItem dependencyToRemove)
        {
            string rootingMarker = FileTracker.FormatRootingMarker(sources);
            RemoveDependencyFromEntry(rootingMarker, dependencyToRemove);
        }

        public void RemoveDependencyFromEntry(ITaskItem source, ITaskItem dependencyToRemove)
        {
            string rootingMarker = FileTracker.FormatRootingMarker(source);
            RemoveDependencyFromEntry(rootingMarker, dependencyToRemove);
        }

        private void RemoveDependencyFromEntry(string rootingMarker, ITaskItem dependencyToRemove)
        {
            if (DependencyTable.TryGetValue(rootingMarker, out var value))
            {
                value.Remove(FileUtilities.NormalizePath(dependencyToRemove.ItemSpec));
                return;
            }
            FileTracker.LogMessageFromResources(_log, MessageImportance.Normal, "Tracking_ReadLogEntryNotFound", rootingMarker);
        }

        public void RemoveDependenciesFromEntryIfMissing(ITaskItem source)
        {
            RemoveDependenciesFromEntryIfMissing(new ITaskItem[1] { source }, null);
        }

        public void RemoveDependenciesFromEntryIfMissing(ITaskItem source, ITaskItem correspondingOutput)
        {
            RemoveDependenciesFromEntryIfMissing(new ITaskItem[1] { source }, new ITaskItem[1] { correspondingOutput });
        }

        public void RemoveDependenciesFromEntryIfMissing(ITaskItem[] source)
        {
            RemoveDependenciesFromEntryIfMissing(source, null);
        }

        public void RemoveDependenciesFromEntryIfMissing(ITaskItem[] source, ITaskItem[] correspondingOutputs)
        {
            Dictionary<string, bool> fileCache = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            if (correspondingOutputs != null)
            {
                ErrorUtilities.VerifyThrowArgument(source.Length == correspondingOutputs.Length, "Tracking_SourcesAndCorrespondingOutputMismatch");
            }
            string rootingMarker = FileTracker.FormatRootingMarker(source, correspondingOutputs);
            RemoveDependenciesFromEntryIfMissing(rootingMarker, fileCache);
            for (int i = 0; i < source.Length; i++)
            {
                rootingMarker = ((correspondingOutputs != null) ? FileTracker.FormatRootingMarker(source[i], correspondingOutputs[i]) : FileTracker.FormatRootingMarker(source[i]));
                RemoveDependenciesFromEntryIfMissing(rootingMarker, fileCache);
            }
        }

        private void RemoveDependenciesFromEntryIfMissing(string rootingMarker, Dictionary<string, bool> fileCache)
        {
            if (!DependencyTable.TryGetValue(rootingMarker, out var value))
            {
                return;
            }
            Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            int num = 0;
            foreach (string key in value.Keys)
            {
                if (num++ > 0)
                {
                    if (!fileCache.TryGetValue(key, out var value2))
                    {
                        value2 = FileUtilities.FileExistsNoThrow(key);
                        fileCache.Add(key, value2);
                    }
                    if (value2)
                    {
                        dictionary.Add(key, value[key]);
                    }
                }
                else
                {
                    dictionary.Add(key, key);
                }
            }
            DependencyTable[rootingMarker] = dictionary;
        }
    }
}
