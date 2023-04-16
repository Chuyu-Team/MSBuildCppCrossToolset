using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections;
using YY.Build.Linux.Tasks.Shared;

namespace YY.Build.Linux.Tasks.GCC
{
    public class Compile : YY.Build.Linux.Tasks.Shared.CommandLineToolTask
    {
        public Compile()
        {
        }
        protected override string ToolName => "g++";

        protected override string AlwaysAppend 
        {
            get
            {
                string _Tmp = "-c";

                foreach (var Item in Sources)
                {
                    _Tmp += ' ';

                    _Tmp += '\"';
                    _Tmp += Item.GetMetadata("FullPath");
                    _Tmp += '\"';
                }

                return _Tmp;
            }
        }

        [Required]
        public ITaskItem[] Sources { get; set; }

        public string ObjectFileName
        {
            get
            {
                if (IsPropertySet("ObjectFileName"))
                {
                    return base.ActiveToolSwitches["ObjectFileName"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ObjectFileName");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.File);
                toolSwitch.DisplayName = "Object File Name";
                toolSwitch.Description = "Specifies a name to override the default object file name; can be file or directory name. (/Fo[name]).";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-o ";
                toolSwitch.Name = "ObjectFileName";
                toolSwitch.Value = value;
                //base.ActiveToolSwitches.Add("ObjectFileName", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string CompileAs
        {
	        get
	        {
		        if (IsPropertySet("CompileAs"))
		        {
			        return base.ActiveToolSwitches["CompileAs"].Value;
		        }
		        return null;
	        }
	        set
	        {
		        base.ActiveToolSwitches.Remove("CompileAs");
		        ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
		        toolSwitch.DisplayName = "Compile As";
		        toolSwitch.Description = "Select compile language option for .c and .cpp files.  'Default' will detect based on .c or .cpp extention. (-x c, -x c++)";
		        toolSwitch.ArgumentRelationList = new ArrayList();
		        string[][] switchMap = new string[3][]
		        {
			        new string[2] { "Default", "" },
			        new string[2] { "CompileAsC", "-x c" },
			        new string[2] { "CompileAsCpp", "-x c++" }
		        };
		        toolSwitch.SwitchValue = ReadSwitchMap("CompileAs", switchMap, value);
		        toolSwitch.Name = "CompileAs";
		        toolSwitch.Value = value;
		        toolSwitch.MultipleValues = true;
		        //base.ActiveToolSwitches.Add("CompileAs", toolSwitch);
		        AddActiveSwitchToolValue(toolSwitch);
	        }
        }

        public override bool Execute()
        {
            foreach (var Item in Sources)
            {
                Log.LogMessage(MessageImportance.High, Item.ItemSpec);
            }

            return base.Execute();
        }
    }
}