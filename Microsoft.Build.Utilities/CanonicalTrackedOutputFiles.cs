using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.Utilities
{
    public class CanonicalTrackedOutputFiles
    {
        private ITaskItem[] _tlogFiles;

        private TaskLoggingHelper _log;

        private bool _tlogAvailable;

        public Dictionary<string, Dictionary<string, DateTime>> DependencyTable { get; private set; }

        public CanonicalTrackedOutputFiles(ITaskItem[] tlogFiles)
        {
            InternalConstruct(null, tlogFiles, constructOutputsFromTLogs: true);
        }

        public CanonicalTrackedOutputFiles(ITask ownerTask, ITaskItem[] tlogFiles)
        {
            InternalConstruct(ownerTask, tlogFiles, constructOutputsFromTLogs: true);
        }

        public CanonicalTrackedOutputFiles(ITask ownerTask, ITaskItem[] tlogFiles, bool constructOutputsFromTLogs)
        {
            InternalConstruct(ownerTask, tlogFiles, constructOutputsFromTLogs);
        }

        private void InternalConstruct(ITask ownerTask, ITaskItem[] tlogFiles, bool constructOutputsFromTLogs)
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
            DependencyTable = new Dictionary<string, Dictionary<string, DateTime>>(StringComparer.OrdinalIgnoreCase);
            if (_tlogFiles != null && constructOutputsFromTLogs)
            {
                ConstructOutputTable();
            }
        }

        private void ConstructOutputTable()
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
            if (!_tlogAvailable)
            {
                FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_TrackingLogNotAvailable");
                lock (DependencyTableCache.DependencyTable)
                {
                    DependencyTableCache.DependencyTable.Remove(text);
                    return;
                }
            }
            DependencyTableCacheEntry dependencyTableCacheEntry = null;
            lock (DependencyTableCache.DependencyTable)
            {
                dependencyTableCacheEntry = DependencyTableCache.GetCachedEntry(text);
            }
            ITaskItem[] tlogFiles;
            if (dependencyTableCacheEntry != null)
            {
                DependencyTable = (Dictionary<string, Dictionary<string, DateTime>>)dependencyTableCacheEntry.DependencyTable;
                FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_WriteTrackingCached");
                tlogFiles = dependencyTableCacheEntry.TlogFiles;
                foreach (ITaskItem taskItem in tlogFiles)
                {
                    FileTracker.LogMessage(_log, MessageImportance.Low, "\t{0}", taskItem.ItemSpec);
                }
                return;
            }
            FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_WriteTrackingLogs");
            bool flag = false;
            string text2 = null;
            tlogFiles = _tlogFiles;
            foreach (ITaskItem taskItem2 in tlogFiles)
            {
                FileTracker.LogMessage(_log, MessageImportance.Low, "\t{0}", taskItem2.ItemSpec);
                try
                {
                    using StreamReader streamReader = File.OpenText(taskItem2.ItemSpec);
                    string text3 = streamReader.ReadLine();
                    while (text3 != null)
                    {
                        if (text3.Length == 0)
                        {
                            flag = true;
                            text2 = taskItem2.ItemSpec;
                            break;
                        }
                        if (text3[0] == '^')
                        {
                            text3 = text3.Substring(1);
                            if (text3.Length == 0)
                            {
                                flag = true;
                                text2 = taskItem2.ItemSpec;
                                break;
                            }
                            if (!DependencyTable.TryGetValue(text3, out var value))
                            {
                                value = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
                                DependencyTable.Add(text3, value);
                            }
                            do
                            {
                                text3 = streamReader.ReadLine();
                                if (text3 != null)
                                {
                                    if (text3.Length == 0)
                                    {
                                        flag = true;
                                        text2 = taskItem2.ItemSpec;
                                        break;
                                    }
                                    if (text3[0] != '^' && text3[0] != '#' && !value.ContainsKey(text3) && (FileTracker.FileIsUnderPath(text3, path) || !FileTracker.FileIsExcludedFromDependencies(text3)))
                                    {
                                        DateTime lastWriteFileUtcTime = NativeMethods.GetLastWriteFileUtcTime(text3);
                                        value.Add(text3, lastWriteFileUtcTime);
                                    }
                                }
                            }
                            while (text3 != null && text3[0] != '^');
                            if (flag)
                            {
                                break;
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
                if (flag)
                {
                    DependencyTableCache.DependencyTable.Remove(text);
                    DependencyTable = new Dictionary<string, Dictionary<string, DateTime>>(StringComparer.OrdinalIgnoreCase);
                }
                else
                {
                    DependencyTableCache.DependencyTable[text] = new DependencyTableCacheEntry(_tlogFiles, DependencyTable);
                }
            }
        }

        public string[] RemoveRootsWithSharedOutputs(ITaskItem[] sources)
        {
            ErrorUtilities.VerifyThrowArgumentNull(sources, "sources");
            List<string> list = new List<string>();
            string text = FileTracker.FormatRootingMarker(sources);
            if (DependencyTable.TryGetValue(text, out var value))
            {
                foreach (KeyValuePair<string, Dictionary<string, DateTime>> item in DependencyTable)
                {
                    if (text.Equals(item.Key, StringComparison.Ordinal))
                    {
                        continue;
                    }
                    foreach (string key in value.Keys)
                    {
                        if (item.Value.ContainsKey(key))
                        {
                            list.Add(item.Key);
                            break;
                        }
                    }
                }
                foreach (string item2 in list)
                {
                    DependencyTable.Remove(item2);
                }
            }
            return list.ToArray();
        }

        public bool RemoveOutputForSourceRoot(string sourceRoot, string outputPathToRemove)
        {
            if (DependencyTable.TryGetValue(sourceRoot, out var value))
            {
                bool result = value.Remove(outputPathToRemove);
                if (DependencyTable[sourceRoot].Count == 0)
                {
                    DependencyTable.Remove(sourceRoot);
                }
                return result;
            }
            return true;
        }

        public ITaskItem[] OutputsForNonCompositeSource(params ITaskItem[] sources)
        {
            Dictionary<string, ITaskItem> dictionary = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
            List<ITaskItem> list = new List<ITaskItem>();
            string text = FileTracker.FormatRootingMarker(sources);
            for (int i = 0; i < sources.Length; i++)
            {
                string sourceKey = FileUtilities.NormalizePath(sources[i].ItemSpec);
                OutputsForSourceRoot(dictionary, sourceKey);
            }
            if (dictionary.Count == 0)
            {
                FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_OutputForRootNotFound", text);
            }
            else
            {
                list.AddRange(dictionary.Values);
                if (dictionary.Count > 100)
                {
                    FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_OutputsNotShown", dictionary.Count);
                }
                else
                {
                    FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_OutputsFor", text);
                    foreach (ITaskItem item in list)
                    {
                        FileTracker.LogMessage(_log, MessageImportance.Low, "\t" + item);
                    }
                }
            }
            return list.ToArray();
        }

        public ITaskItem[] OutputsForSource(params ITaskItem[] sources)
        {
            return OutputsForSource(sources, searchForSubRootsInCompositeRootingMarkers: true);
        }

        public ITaskItem[] OutputsForSource(ITaskItem[] sources, bool searchForSubRootsInCompositeRootingMarkers)
        {
            if (!_tlogAvailable)
            {
                return null;
            }
            Dictionary<string, ITaskItem> dictionary = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
            string text = FileTracker.FormatRootingMarker(sources);
            List<ITaskItem> list = new List<ITaskItem>();
            foreach (string key in DependencyTable.Keys)
            {
                string text2 = key/*.ToUpperInvariant()*/;
                if (searchForSubRootsInCompositeRootingMarkers && (text.Contains(text2) || text2.Contains(text) || CanonicalTrackedFilesHelper.RootContainsAllSubRootComponents(text, text2)))
                {
                    OutputsForSourceRoot(dictionary, text2);
                }
                else if (!searchForSubRootsInCompositeRootingMarkers && text2.Equals(text, StringComparison.Ordinal))
                {
                    OutputsForSourceRoot(dictionary, text2);
                }
            }
            if (dictionary.Count == 0)
            {
                FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_OutputForRootNotFound", text);
            }
            else
            {
                list.AddRange(dictionary.Values);
                if (dictionary.Count > 100)
                {
                    FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_OutputsNotShown", dictionary.Count);
                }
                else
                {
                    FileTracker.LogMessageFromResources(_log, MessageImportance.Low, "Tracking_OutputsFor", text);
                    foreach (ITaskItem item in list)
                    {
                        FileTracker.LogMessage(_log, MessageImportance.Low, "\t" + item);
                    }
                }
            }
            return list.ToArray();
        }

        private void OutputsForSourceRoot(Dictionary<string, ITaskItem> outputs, string sourceKey)
        {
            if (!DependencyTable.TryGetValue(sourceKey, out var value))
            {
                return;
            }
            foreach (string key in value.Keys)
            {
                if (!outputs.ContainsKey(key))
                {
                    outputs.Add(key, new TaskItem(key));
                }
            }
        }

        public void AddComputedOutputForSourceRoot(string sourceKey, string computedOutput)
        {
            AddOutput(GetSourceKeyOutputs(sourceKey), computedOutput);
        }

        public void AddComputedOutputsForSourceRoot(string sourceKey, string[] computedOutputs)
        {
            Dictionary<string, DateTime> sourceKeyOutputs = GetSourceKeyOutputs(sourceKey);
            foreach (string computedOutput in computedOutputs)
            {
                AddOutput(sourceKeyOutputs, computedOutput);
            }
        }

        public void AddComputedOutputsForSourceRoot(string sourceKey, ITaskItem[] computedOutputs)
        {
            Dictionary<string, DateTime> sourceKeyOutputs = GetSourceKeyOutputs(sourceKey);
            foreach (ITaskItem taskItem in computedOutputs)
            {
                AddOutput(sourceKeyOutputs, FileUtilities.NormalizePath(taskItem.ItemSpec));
            }
        }

        private Dictionary<string, DateTime> GetSourceKeyOutputs(string sourceKey)
        {
            if (!DependencyTable.TryGetValue(sourceKey, out var value))
            {
                value = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
                DependencyTable.Add(sourceKey, value);
            }
            return value;
        }

        private static void AddOutput(Dictionary<string, DateTime> dependencies, string computedOutput)
        {
            var Output = FileUtilities.NormalizePath(computedOutput);

            string text = Output/*.ToUpperInvariant()*/;
            if (!dependencies.ContainsKey(text))
            {
                DateTime value = (FileUtilities.FileExistsNoThrow(Output) ? NativeMethods.GetLastWriteFileUtcTime(Output) : DateTime.MinValue);
                dependencies.Add(text, value);
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
            foreach (string key2 in DependencyTable.Keys)
            {
                Dictionary<string, DateTime> dictionary = DependencyTable[key2];
                streamWriter.WriteLine("^" + key2);
                foreach (string key3 in dictionary.Keys)
                {
                    if (includeInTLog == null || includeInTLog(key3))
                    {
                        streamWriter.WriteLine(key3);
                    }
                }
            }

            _tlogAvailable = true;
        }

        public void RemoveEntriesForSource(ITaskItem source)
        {
            RemoveEntriesForSource(new ITaskItem[1] { source }, null);
        }

        public void RemoveEntriesForSource(ITaskItem source, ITaskItem correspondingOutput)
        {
            RemoveEntriesForSource(new ITaskItem[1] { source }, new ITaskItem[1] { correspondingOutput });
        }

        public void RemoveEntriesForSource(ITaskItem[] source)
        {
            RemoveEntriesForSource(source, null);
        }

        public void RemoveEntriesForSource(ITaskItem[] source, ITaskItem[] correspondingOutputs)
        {
            string key = FileTracker.FormatRootingMarker(source, correspondingOutputs);
            DependencyTable.Remove(key);
            foreach (ITaskItem taskItem in source)
            {
                DependencyTable.Remove(FileUtilities.NormalizePath(taskItem.ItemSpec));
            }
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
            FileTracker.LogMessageFromResources(_log, MessageImportance.Normal, "Tracking_WriteLogEntryNotFound", rootingMarker);
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
            Dictionary<string, DateTime> dictionary = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
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
                    dictionary.Add(key, DateTime.Now);
                }
            }
            DependencyTable[rootingMarker] = dictionary;
        }
    }
}
