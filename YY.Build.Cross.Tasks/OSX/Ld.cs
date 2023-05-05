using Microsoft.Build.CPPTasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace YY.Build.Cross.Tasks.OSX
{
    public class Ld : YY.Build.Cross.Tasks.Cross.Ld
    {
        public Ld()
            : base()
        {
            // 苹果自带的ld不支持这几个选项
            SwitchOrderList.Remove("Relocation");
            SwitchOrderList.Remove("FunctionBinding");
            SwitchOrderList.Remove("NoExecStackRequired");
            SwitchOrderList.Remove("ShowProgress");
            SwitchOrderList.Remove("LinkStatus");

            var Index = SwitchOrderList.IndexOf("AdditionalOptions");
            SwitchOrderList.Insert(Index++, "FlatNamespace");
            SwitchOrderList.Insert(Index++, "Frameworks");
        }

        public override bool UnresolvedSymbolReferences
        {
            get
            {
                if (IsPropertySet("UnresolvedSymbolReferences"))
                {
                    return base.ActiveToolSwitches["UnresolvedSymbolReferences"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("UnresolvedSymbolReferences");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Report Unresolved Symbol References";
                toolSwitch.Description = "This option when enabled will report unresolved symbol references.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                // toolSwitch.SwitchValue = "-Wl,--no-undefined";
                toolSwitch.SwitchValue = "-Wl,-undefined,error";
                toolSwitch.Name = "UnresolvedSymbolReferences";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("UnresolvedSymbolReferences", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool FlatNamespace
        {
            get
            {
                if (IsPropertySet("FlatNamespace"))
                {
                    return base.ActiveToolSwitches["FlatNamespace"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("FlatNamespace");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "启用一级命名";
                toolSwitch.Description = "如果关闭则保存默认，默认值为二级命名（two_levelnamespace）。";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,-flat_namespace";
                toolSwitch.Name = "FlatNamespace";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("FlatNamespace", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string[] Frameworks
        {
            get
            {
                if (IsPropertySet("Frameworks"))
                {
                    return base.ActiveToolSwitches["Frameworks"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("Frameworks");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringArray);
                toolSwitch.DisplayName = "依赖的Framework";
                toolSwitch.Description = "Apple特有的Framework引用（-framework）。";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-framework ";
                toolSwitch.Name = "Frameworks";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("Frameworks", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }
    }
}
