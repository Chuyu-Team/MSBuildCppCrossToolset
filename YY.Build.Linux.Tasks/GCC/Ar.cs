using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YY.Build.Linux.Tasks.GCC
{
    public class Ar : TrackedVCToolTask
    {
        private ArrayList switchOrderList;

        protected override ArrayList SwitchOrderList => switchOrderList;

        protected override string ToolName => "ar";

        public Ar()
            : base(Microsoft.Build.CppTasks.Common.Properties.Microsoft_Build_CPPTasks_Strings.ResourceManager)
        {
            switchOrderList = new ArrayList();
            switchOrderList.Add("Command");
            switchOrderList.Add("CreateIndex");
            switchOrderList.Add("CreateThinArchive");
            switchOrderList.Add("NoWarnOnCreate");
            switchOrderList.Add("TruncateTimestamp");
            switchOrderList.Add("SuppressStartupBanner");
            switchOrderList.Add("Verbose");
            switchOrderList.Add("AdditionalOptions");
            switchOrderList.Add("OutputFile");
            switchOrderList.Add("Sources");
        }

        public virtual string Command
        {
            get
            {
                if (IsPropertySet("Command"))
                {
                    return base.ActiveToolSwitches["Command"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("Command");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "Command";
                toolSwitch.Description = "Command for AR.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.ArgumentRelationList.Add(new ArgumentRelation("CreateIndex", "", required: false, ""));
                toolSwitch.ArgumentRelationList.Add(new ArgumentRelation("CreateThinArchive", "", required: false, ""));
                toolSwitch.ArgumentRelationList.Add(new ArgumentRelation("NoWarnOnCreate", "", required: false, ""));
                toolSwitch.ArgumentRelationList.Add(new ArgumentRelation("TruncateTimestamp", "", required: false, ""));
                toolSwitch.ArgumentRelationList.Add(new ArgumentRelation("SuppressStartupBanner", "", required: false, ""));
                toolSwitch.ArgumentRelationList.Add(new ArgumentRelation("Verbose", "", required: false, ""));
                string[][] switchMap = new string[7][]
                {
                    new string[2] { "Delete", "-d" },
                    new string[2] { "Move", "-m" },
                    new string[2] { "Print", "-p" },
                    new string[2] { "Quick", "-q" },
                    new string[2] { "Replacement", "-r" },
                    new string[2] { "Table", "-t" },
                    new string[2] { "Extract", "-x" }
                };
                toolSwitch.SwitchValue = ReadSwitchMap("Command", switchMap, value);
                toolSwitch.Name = "Command";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("Command", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        [Required]
        public virtual string OutputFile
        {
            get
            {
                if (IsPropertySet("OutputFile"))
                {
                    return base.ActiveToolSwitches["OutputFile"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("OutputFile");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.File);
                toolSwitch.Separator = " ";
                toolSwitch.DisplayName = "Output File";
                toolSwitch.Description = "The /OUT option overrides the default name and location of the program that the lib creates.";
                toolSwitch.Required = true;
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.Name = "OutputFile";
                toolSwitch.Value = value;
                base.ActiveToolSwitches.Add("OutputFile", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        [Required]
        public virtual ITaskItem[] Sources
        {
            get
            {
                if (IsPropertySet("Sources"))
                {
                    return base.ActiveToolSwitches["Sources"].TaskItemArray;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("Sources");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.ITaskItemArray);
                toolSwitch.Required = true;
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.TaskItemArray = value;
                base.ActiveToolSwitches.Add("Sources", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool CreateIndex
        {
            get
            {
                if (IsPropertySet("CreateIndex"))
                {
                    return base.ActiveToolSwitches["CreateIndex"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("CreateIndex");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Create an archive index";
                toolSwitch.Description = "Create an archive index (cf. ranlib).  This can speed up linking and reduce dependency within its own library.";
                toolSwitch.Parents.AddLast("Command");
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "s";
                toolSwitch.Name = "CreateIndex";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("CreateIndex", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool CreateThinArchive
        {
            get
            {
                if (IsPropertySet("CreateThinArchive"))
                {
                    return base.ActiveToolSwitches["CreateThinArchive"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("CreateThinArchive");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Create Thin Archive";
                toolSwitch.Description = "Create a thin archive.  A thin archive contains relativepaths to the objects instead of embedding the objects.  Switching between Thin and Normal requires deleting the existing library.";
                toolSwitch.Parents.AddLast("Command");
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "T";
                toolSwitch.Name = "CreateThinArchive";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("CreateThinArchive", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool NoWarnOnCreate
        {
            get
            {
                if (IsPropertySet("NoWarnOnCreate"))
                {
                    return base.ActiveToolSwitches["NoWarnOnCreate"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("NoWarnOnCreate");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "No Warning on Create";
                toolSwitch.Description = "Do not warn if when the library is created.";
                toolSwitch.Parents.AddLast("Command");
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "c";
                toolSwitch.Name = "NoWarnOnCreate";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("NoWarnOnCreate", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool TruncateTimestamp
        {
            get
            {
                if (IsPropertySet("TruncateTimestamp"))
                {
                    return base.ActiveToolSwitches["TruncateTimestamp"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("TruncateTimestamp");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Truncate Timestamp";
                toolSwitch.Description = "Use zero for timestamps and uids/gids.";
                toolSwitch.Parents.AddLast("Command");
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "D";
                toolSwitch.Name = "TruncateTimestamp";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("TruncateTimestamp", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool SuppressStartupBanner
        {
            get
            {
                if (IsPropertySet("SuppressStartupBanner"))
                {
                    return base.ActiveToolSwitches["SuppressStartupBanner"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("SuppressStartupBanner");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Suppress Startup Banner";
                toolSwitch.Description = "Dont show version number.";
                toolSwitch.Parents.AddLast("Command");
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.ReverseSwitchValue = "V";
                toolSwitch.Name = "SuppressStartupBanner";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("SuppressStartupBanner", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool Verbose
        {
            get
            {
                if (IsPropertySet("Verbose"))
                {
                    return base.ActiveToolSwitches["Verbose"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("Verbose");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Verbose";
                toolSwitch.Description = "Verbose";
                toolSwitch.Parents.AddLast("Command");
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "v";
                toolSwitch.Name = "Verbose";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("Verbose", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        protected override ITaskItem[] TrackedInputFiles => Sources;

        protected override bool MaintainCompositeRootingMarkers => true;

        protected override string TrackerIntermediateDirectory
        {
            get
            {
                if (TrackerLogDirectory != null)
                {
                    return TrackerLogDirectory;
                }
                return string.Empty;
            }
        }

        public virtual string TrackerLogDirectory
        {
            get
            {
                if (IsPropertySet("TrackerLogDirectory"))
                {
                    return base.ActiveToolSwitches["TrackerLogDirectory"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("TrackerLogDirectory");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Directory);
                toolSwitch.DisplayName = "Tracker Log Directory";
                toolSwitch.Description = "Tracker log directory.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.Value = VCToolTask.EnsureTrailingSlash(value);
                base.ActiveToolSwitches.Add("TrackerLogDirectory", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        protected override void SaveTracking()
        {
            string SourceKey = "^";

            foreach (ITaskItem taskItem in Sources)
            {
                if (SourceKey.Length > 1)
                    SourceKey += '|';

                SourceKey += FileTracker.FormatRootingMarker(taskItem);
            }

            // 保存Write文件
            {
                string WriteFilePath = TLogWriteFiles[0].GetMetadata("FullPath");
                Directory.CreateDirectory(Path.GetDirectoryName(WriteFilePath));
                using StreamWriter WriteFileWriter = FileUtilities.OpenWrite(WriteFilePath, append: true, Encoding.Unicode);

                WriteFileWriter.WriteLine(SourceKey);
                WriteFileWriter.WriteLine(OutputFile);
            }

            // 保存Read文件
            {
                string ReadFilePath = TLogReadFiles[0].GetMetadata("FullPath");
                Directory.CreateDirectory(Path.GetDirectoryName(ReadFilePath));
                using StreamWriter ReadFileWriter = FileUtilities.OpenWrite(ReadFilePath, append: true, Encoding.Unicode);

                ReadFileWriter.WriteLine(SourceKey);

                foreach (ITaskItem taskItem in Sources)
                {
                    ReadFileWriter.WriteLine(FileTracker.FormatRootingMarker(taskItem));
                }
            }
        }
    }
}
