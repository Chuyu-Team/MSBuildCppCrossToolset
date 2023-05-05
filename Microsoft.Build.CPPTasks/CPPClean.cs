using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Resources;

namespace Microsoft.Build.CPPTasks
{
    public class CPPClean : Microsoft.Build.Utilities.Task
    {
        private ITaskItem[] _deletedFiles;

        private string _foldersToClean;

        private string _filePatternsToDeleteOnClean;

        private string _filesExcludedFromClean;

        private bool _doDelete = true;

        private HashSet<string> _filesToDeleteSet = new HashSet<string>();

        [Required]
        public string FoldersToClean
        {
            get
            {
                return _foldersToClean;
            }
            set
            {
                _foldersToClean = value;
            }
        }

        public string FilesExcludedFromClean
        {
            get
            {
                return _filesExcludedFromClean;
            }
            set
            {
                _filesExcludedFromClean = value;
            }
        }

        public bool DoDelete
        {
            get
            {
                return _doDelete;
            }
            set
            {
                _doDelete = value;
            }
        }

        [Required]
        public string FilePatternsToDeleteOnClean
        {
            get
            {
                return _filePatternsToDeleteOnClean;
            }
            set
            {
                _filePatternsToDeleteOnClean = value;
            }
        }

        [Output]
        public ITaskItem[] DeletedFiles
        {
            get
            {
                return _deletedFiles;
            }
            set
            {
                _deletedFiles = value;
            }
        }

        public CPPClean()
            : base(Microsoft.Build.CppTasks.Common.Properties.Microsoft_Build_CPPTasks_Strings.ResourceManager)
        {
        }

        public override bool Execute()
        {
            try
            {
                if (!ComputeFilesToDelete())
                {
                    return true;
                }
                if (DoDelete)
                {
                    DeleteFiles();
                }
                return true;
            }
            catch (Exception ex)
            {
                base.Log.LogErrorFromException(ex);
                if (Microsoft.Build.Shared.ExceptionHandling.IsCriticalException(ex))
                {
                    throw;
                }
                return false;
            }
        }

        private bool ComputeFilesToDelete()
        {
            if (FoldersToClean == null || FilePatternsToDeleteOnClean == null)
            {
                return false;
            }
            string[] array = FilePatternsToDeleteOnClean.Split(';');
            string[] array2 = FoldersToClean.Split(';');
            string[] array3 = ((FilesExcludedFromClean != null) ? FilesExcludedFromClean.Split(';') : null);
            List<string> list = new List<string>();
            string[] array4 = array2;
            foreach (string text in array4)
            {
                if (!string.IsNullOrEmpty(text) && Directory.Exists(text))
                {
                    list.Add(text);
                }
            }
            if (list.Count == 0)
            {
                return false;
            }
            List<string> filesFromFoldersByFilters = GetFilesFromFoldersByFilters(list, "*.write.*.tlog");
            GetFilesFromTLogs(filesFromFoldersByFilters, _filesToDeleteSet);
            foreach (string item in list)
            {
                DirectoryInfo dirInfo = new DirectoryInfo(item);
                string[] array5 = array;
                foreach (string filePattern in array5)
                {
                    List<string> matchingFilesInFolder = GetMatchingFilesInFolder(dirInfo, filePattern);
                    foreach (string item2 in matchingFilesInFolder)
                    {
                        _filesToDeleteSet.Add(item2/*.ToLowerInvariant()*/);
                    }
                }
                if (array3 == null)
                {
                    continue;
                }
                string[] array6 = array3;
                foreach (string filePattern2 in array6)
                {
                    List<string> matchingFilesInFolder2 = GetMatchingFilesInFolder(dirInfo, filePattern2);
                    foreach (string item3 in matchingFilesInFolder2)
                    {
                        _filesToDeleteSet.Remove(item3/*.ToLowerInvariant()*/);
                    }
                }
            }
            if (_filesToDeleteSet.Count == 0)
            {
                return false;
            }
            DeletedFiles = new ITaskItem[_filesToDeleteSet.Count];
            int num = 0;
            foreach (string item4 in _filesToDeleteSet)
            {
                DeletedFiles[num] = new TaskItem(item4);
                num++;
            }
            return true;
        }

        private static List<string> GetFilesFromFoldersByFilters(List<string> foldersList, string filter)
        {
            if (foldersList == null)
            {
                return null;
            }
            List<string> list = new List<string>();
            foreach (string folders in foldersList)
            {
                list.AddRange(Directory.GetFiles(folders, filter));
            }
            return list;
        }

        private static List<string> GetMatchingFilesInFolder(DirectoryInfo dirInfo, string filePattern)
        {
            List<string> list = new List<string>();
            if (!string.IsNullOrEmpty(filePattern))
            {
                if (filePattern.IndexOf("*", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    FileInfo[] files = dirInfo.GetFiles(filePattern);
                    FileInfo[] array = files;
                    foreach (FileInfo fileInfo in array)
                    {
                        list.Add(fileInfo.FullName);
                    }
                }
                else
                {
                    string text = (Path.IsPathRooted(filePattern) ? filePattern : Path.Combine(dirInfo.FullName, filePattern));
                    if (File.Exists(text))
                    {
                        list.Add(text);
                    }
                }
            }
            return list;
        }

        private static void GetFilesFromTLogs(List<string> TLogs, HashSet<string> filesToDelete)
        {
            foreach (string TLog in TLogs)
            {
                if (TLog.Length <= 0)
                {
                    continue;
                }
                using TextReader textReader = File.OpenText(TLog);
                while (textReader.Peek() > -1)
                {
                    string text = textReader.ReadLine();
                    if (text.Length > 0 && text[0] != '^' && text[0] != '#')
                    {
                        filesToDelete.Add(text/*.ToLowerInvariant()*/);
                    }
                }
            }
        }

        private bool DeleteFiles()
        {
            foreach (string item in _filesToDeleteSet)
            {
                if (!File.Exists(item))
                {
                    continue;
                }
                try
                {
                    File.Delete(item);
                }
                catch (Exception ex)
                {
                    if (Microsoft.Build.Shared.ExceptionHandling.IsCriticalException(ex))
                    {
                        throw;
                    }
                    base.Log.LogWarningFromException(ex);
                }
            }
            return true;
        }
    }
}
