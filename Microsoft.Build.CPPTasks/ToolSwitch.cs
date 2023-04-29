using System.Collections;
using System.Collections.Generic;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;

namespace Microsoft.Build.CPPTasks
{
    public enum ToolSwitchType
    {
        Boolean,
        Integer,
        String,
        StringArray,
        File,
        Directory,
        ITaskItem,
        ITaskItemArray,
        AlwaysAppend,
        StringPathArray
    }

    public class ToolSwitch
    {
        private string name = string.Empty;

        private ToolSwitchType type;

        private string falseSuffix = string.Empty;

        private string trueSuffix = string.Empty;

        private string separator = string.Empty;

        private string argumentParameter = string.Empty;

        private string fallback = string.Empty;

        private bool argumentRequired;

        private bool required;

        private LinkedList<string> parents = new LinkedList<string>();

        private LinkedList<KeyValuePair<string, string>> overrides = new LinkedList<KeyValuePair<string, string>>();

        private ArrayList argumentRelationList;

        private bool isValid;

        private bool reversible;

        private bool booleanValue = true;

        private int number;

        private string[] stringList;

        private ITaskItem taskItem;

        private ITaskItem[] taskItemArray;

#if __REMOVE
#else
        // GCC工具链没有完整路径选择，为了方便出错时查看代码路径，所以添加了完整路径输出能力。
        public bool TaskItemFullPath = false;
#endif
        private string value = string.Empty;

        private string switchValue = string.Empty;

        private string reverseSwitchValue = string.Empty;

        private string description = string.Empty;

        private string displayName = string.Empty;

        private const string typeBoolean = "ToolSwitchType.Boolean";

        private const string typeInteger = "ToolSwitchType.Integer";

        private const string typeITaskItem = "ToolSwitchType.ITaskItem";

        private const string typeITaskItemArray = "ToolSwitchType.ITaskItemArray";

        private const string typeStringArray = "ToolSwitchType.StringArray or ToolSwitchType.StringPathArray";

        public string Name
        {
            get
            {
                return name;
            }
            set
            {
                name = value;
            }
        }

        public string Value
        {
            get
            {
                return value;
            }
            set
            {
                this.value = value;
            }
        }

        public bool IsValid
        {
            get
            {
                return isValid;
            }
            set
            {
                isValid = value;
            }
        }

        public string SwitchValue
        {
            get
            {
                return switchValue;
            }
            set
            {
                switchValue = value;
            }
        }

        public string ReverseSwitchValue
        {
            get
            {
                return reverseSwitchValue;
            }
            set
            {
                reverseSwitchValue = value;
            }
        }

        public string DisplayName
        {
            get
            {
                return displayName;
            }
            set
            {
                displayName = value;
            }
        }

        public string Description
        {
            get
            {
                return description;
            }
            set
            {
                description = value;
            }
        }

        public ToolSwitchType Type
        {
            get
            {
                return type;
            }
            set
            {
                type = value;
            }
        }

        public bool Reversible
        {
            get
            {
                return reversible;
            }
            set
            {
                reversible = value;
            }
        }

        public bool MultipleValues { get; set; }

        public string FalseSuffix
        {
            get
            {
                return falseSuffix;
            }
            set
            {
                falseSuffix = value;
            }
        }

        public string TrueSuffix
        {
            get
            {
                return trueSuffix;
            }
            set
            {
                trueSuffix = value;
            }
        }

        public string Separator
        {
            get
            {
                return separator;
            }
            set
            {
                separator = value;
            }
        }

        public string FallbackArgumentParameter
        {
            get
            {
                return fallback;
            }
            set
            {
                fallback = value;
            }
        }

        public bool ArgumentRequired
        {
            get
            {
                return argumentRequired;
            }
            set
            {
                argumentRequired = value;
            }
        }

        public bool Required
        {
            get
            {
                return required;
            }
            set
            {
                required = value;
            }
        }

        public LinkedList<string> Parents => parents;

        public LinkedList<KeyValuePair<string, string>> Overrides => overrides;

        public ArrayList ArgumentRelationList
        {
            get
            {
                return argumentRelationList;
            }
            set
            {
                argumentRelationList = value;
            }
        }

        public bool BooleanValue
        {
            get
            {
                Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(type == ToolSwitchType.Boolean, "InvalidType", "ToolSwitchType.Boolean");
                return booleanValue;
            }
            set
            {
                Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(type == ToolSwitchType.Boolean, "InvalidType", "ToolSwitchType.Boolean");
                booleanValue = value;
            }
        }

        public int Number
        {
            get
            {
                Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(type == ToolSwitchType.Integer, "InvalidType", "ToolSwitchType.Integer");
                return number;
            }
            set
            {
                Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(type == ToolSwitchType.Integer, "InvalidType", "ToolSwitchType.Integer");
                number = value;
            }
        }

        public string[] StringList
        {
            get
            {
                Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(type == ToolSwitchType.StringArray || type == ToolSwitchType.StringPathArray, "InvalidType", "ToolSwitchType.StringArray or ToolSwitchType.StringPathArray");
                return stringList;
            }
            set
            {
                Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(type == ToolSwitchType.StringArray || type == ToolSwitchType.StringPathArray, "InvalidType", "ToolSwitchType.StringArray or ToolSwitchType.StringPathArray");
                stringList = value;
            }
        }

        public ITaskItem TaskItem
        {
            get
            {
                Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(type == ToolSwitchType.ITaskItem, "InvalidType", "ToolSwitchType.ITaskItem");
                return taskItem;
            }
            set
            {
                Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(type == ToolSwitchType.ITaskItem, "InvalidType", "ToolSwitchType.ITaskItem");
                taskItem = value;
            }
        }

        public ITaskItem[] TaskItemArray
        {
            get
            {
                Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(type == ToolSwitchType.ITaskItemArray, "InvalidType", "ToolSwitchType.ITaskItemArray");
                return taskItemArray;
            }
            set
            {
                Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(type == ToolSwitchType.ITaskItemArray, "InvalidType", "ToolSwitchType.ITaskItemArray");
                taskItemArray = value;
            }
        }

        public string ValueAsString
        {
            get
            {
                string result = string.Empty;
                switch (Type)
                {
                    case ToolSwitchType.Boolean:
                        result = booleanValue.ToString();
                        break;
                    case ToolSwitchType.String:
                    case ToolSwitchType.File:
                    case ToolSwitchType.Directory:
                        result = value;
                        break;
                    case ToolSwitchType.StringArray:
                    case ToolSwitchType.StringPathArray:
                        result = string.Join(";", StringList);
                        break;
                    case ToolSwitchType.Integer:
                        result = number.ToString();
                        break;
                }
                return result;
            }
        }

        public ToolSwitch()
        {
        }

        public ToolSwitch(ToolSwitchType toolType)
        {
            type = toolType;
        }
    }

}
