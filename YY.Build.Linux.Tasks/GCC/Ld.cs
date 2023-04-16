using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YY.Build.Linux.Tasks.Shared;
using Microsoft.Build.Framework;

namespace YY.Build.Linux.Tasks.GCC
{
    public class Ld : YY.Build.Linux.Tasks.Shared.CommandLineToolTask
    {
        public Ld()
        {
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
                toolSwitch.Separator = " ";
                toolSwitch.Required = true;
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.TaskItemArray = value;
                base.ActiveToolSwitches.Add("Sources", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }


        public string OutputFile
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
                toolSwitch.DisplayName = "Output File";
                toolSwitch.Description = "The option overrides the default name and location of the program that the linker creates. (-o)";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-o ";
                toolSwitch.Name = "OutputFile";
                toolSwitch.Value = value;
                // base.ActiveToolSwitches.Add("OutputFile", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }
    }
}
