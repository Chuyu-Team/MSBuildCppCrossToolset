using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;
// using Microsoft.Build.Linux.Shared;
// using Microsoft.Build.Linux.Tasks;
using Microsoft.Build.Utilities;
using System.Resources;
using Microsoft.Build.Shared;
using System.Text;
using System;

namespace YY.Build.Cross.Tasks.OSX
{
    public class Compile : YY.Build.Cross.Tasks.Cross.Compile
    {
        public Compile()
            : base()
        {
            var Index = SwitchOrderList.IndexOf("AdditionalOptions");
            SwitchOrderList.Insert(Index++, "ObjCAutomaticRefCounting");
            SwitchOrderList.Insert(Index++, "ObjCAutomaticRefCountingExceptionHandlingSafe");
            SwitchOrderList.Insert(Index++, "ObjCExceptionHandling");
        }

        // OSX 添加ObjectC 以及ObjectC++支持
        public virtual string CompileAs
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
                string[][] switchMap = new string[][]
                {
                    new string[2] { "Default", "" },
                    new string[2] { "CompileAsC", "-x c" },
                    new string[2] { "CompileAsCpp", "-x c++" },
                    new string[2] { "CompileAsObjC", "-x objective-c" },
                    new string[2] { "CompileAsObjCpp", "-x objective-c++" },
                    // assembler这2个是隐藏功能，给AS特意开的洞。
                    new string[2] { "CompileAsAsm", "-x assembler" },
                    new string[2] { "CompileAsAsmWithCpp", "-x assembler-with-cpp" },
                };
                toolSwitch.SwitchValue = ReadSwitchMap("CompileAs", switchMap, value);
                toolSwitch.Name = "CompileAs";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("CompileAs", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool ObjCAutomaticRefCounting
        {
            get
            {
                if (IsPropertySet("ObjCAutomaticRefCounting"))
                {
                    return base.ActiveToolSwitches["ObjCAutomaticRefCounting"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ObjCAutomaticRefCounting");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Object-C Automatic Reference Counting";
                toolSwitch.Description = "Synthesize retain and release calls for Objective-C pointers";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-fobjc-arc";
                toolSwitch.Name = "ObjCAutomaticRefCounting";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("ObjCAutomaticRefCounting", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool ObjCAutomaticRefCountingExceptionHandlingSafe
        {
            get
            {
                if (IsPropertySet("ObjCAutomaticRefCountingExceptionHandlingSafe"))
                {
                    return base.ActiveToolSwitches["ObjCAutomaticRefCountingExceptionHandlingSafe"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ObjCAutomaticRefCountingExceptionHandlingSafe");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Object-C Automatic Reference Counting Except Handle Safe";
                toolSwitch.Description = "Use EH-safe code when synthesizing retains and releases in -fobjc-arc.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-fobjc-arc-exceptions";
                toolSwitch.Name = "ObjCAutomaticRefCountingExceptionHandlingSafe";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("ObjCAutomaticRefCountingExceptionHandlingSafe", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string ObjCExceptionHandling
        {
            get
            {
                if (IsPropertySet("ObjCExceptionHandling"))
                {
                    return base.ActiveToolSwitches["ObjCExceptionHandling"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ObjCExceptionHandling");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "ObjC Exception Handling";
                toolSwitch.Description = "Enable Objective-C exceptions.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                string[][] switchMap = new string[][]
                {
                    new string[2] { "Disabled", "" },
                    new string[2] { "Enabled", "-fobjc-exceptions" }
                };
                toolSwitch.SwitchValue = ReadSwitchMap("ObjCExceptionHandling", switchMap, value);
                toolSwitch.Name = "ObjCExceptionHandling";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("ObjCExceptionHandling", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        protected override void ValidateRelations()
        {
            if(!ObjCAutomaticRefCounting)
            {
                base.ActiveToolSwitches.Remove("ObjCAutomaticRefCountingExceptionHandlingSafe");
            }
        }
    }
}