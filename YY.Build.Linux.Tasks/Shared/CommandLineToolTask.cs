using Microsoft.Build.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YY.Build.Linux.Tasks.Shared
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
                ErrorUtilities.VerifyThrow(type == ToolSwitchType.Boolean, "InvalidType", "ToolSwitchType.Boolean");
                return booleanValue;
            }
            set
            {
                ErrorUtilities.VerifyThrow(type == ToolSwitchType.Boolean, "InvalidType", "ToolSwitchType.Boolean");
                booleanValue = value;
            }
        }

        public int Number
        {
            get
            {
                ErrorUtilities.VerifyThrow(type == ToolSwitchType.Integer, "InvalidType", "ToolSwitchType.Integer");
                return number;
            }
            set
            {
                ErrorUtilities.VerifyThrow(type == ToolSwitchType.Integer, "InvalidType", "ToolSwitchType.Integer");
                number = value;
            }
        }

        public string[] StringList
        {
            get
            {
                ErrorUtilities.VerifyThrow(type == ToolSwitchType.StringArray || type == ToolSwitchType.StringPathArray, "InvalidType", "ToolSwitchType.StringArray or ToolSwitchType.StringPathArray");
                return stringList;
            }
            set
            {
                ErrorUtilities.VerifyThrow(type == ToolSwitchType.StringArray || type == ToolSwitchType.StringPathArray, "InvalidType", "ToolSwitchType.StringArray or ToolSwitchType.StringPathArray");
                stringList = value;
            }
        }

        public ITaskItem TaskItem
        {
            get
            {
                ErrorUtilities.VerifyThrow(type == ToolSwitchType.ITaskItem, "InvalidType", "ToolSwitchType.ITaskItem");
                return taskItem;
            }
            set
            {
                ErrorUtilities.VerifyThrow(type == ToolSwitchType.ITaskItem, "InvalidType", "ToolSwitchType.ITaskItem");
                taskItem = value;
            }
        }

        public ITaskItem[] TaskItemArray
        {
            get
            {
                ErrorUtilities.VerifyThrow(type == ToolSwitchType.ITaskItemArray, "InvalidType", "ToolSwitchType.ITaskItemArray");
                return taskItemArray;
            }
            set
            {
                ErrorUtilities.VerifyThrow(type == ToolSwitchType.ITaskItemArray, "InvalidType", "ToolSwitchType.ITaskItemArray");
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
            type = ToolSwitchType.Boolean;
        }

        public ToolSwitch(ToolSwitchType toolType)
        {
            type = toolType;
        }
    }

    public class CommandLineToolTask : Microsoft.Build.Utilities.ToolTask
    {
        protected Dictionary<string, ToolSwitch> ActiveToolSwitches = new Dictionary<string, ToolSwitch>(StringComparer.OrdinalIgnoreCase);

        protected virtual string AlwaysAppend
        {
            get
            {
                return string.Empty;
            }
            set
            {
            }
        }

        protected override string ToolName
        {
            get
            {
                return string.Empty;
            }
        }

        protected override string GenerateFullPathToTool()
        {
            // Todo Linux上只考虑 sh
            return "sh";
        }

        public bool IsPropertySet(string propertyName)
        {
            return ActiveToolSwitches.ContainsKey(propertyName);
        }

        protected void AddActiveSwitchToolValue(ToolSwitch switchToAdd)
        {
            if (switchToAdd.Type != 0 || switchToAdd.BooleanValue)
            {
                if (switchToAdd.SwitchValue != string.Empty)
                {
                    ActiveToolSwitches.Add(switchToAdd.SwitchValue, switchToAdd);
                }
            }
            else if (switchToAdd.ReverseSwitchValue != string.Empty)
            {
                ActiveToolSwitches.Add(switchToAdd.ReverseSwitchValue, switchToAdd);
            }
        }

        protected string ReadSwitchMap(string propertyName, string[][] switchMap, string value)
        {
            if (switchMap != null)
            {
                for (int i = 0; i < switchMap.Length; i++)
                {
                    if (string.Equals(switchMap[i][0], value, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return switchMap[i][1];
                    }
                }
                /*
                if (!IgnoreUnknownSwitchValues)
                {
                    logPrivate.LogErrorFromResources("ArgumentOutOfRange", new object[2] { propertyName, value });
                }
                */
            }
            return string.Empty;
        }

        protected override string GenerateCommandLineCommands()
        {
            return GetCommandLine();
        }

        public string GetCommandLine()
        {
            var CommendLine = "";

            foreach (var Switch in ActiveToolSwitches)
            {
                if (Switch.Value.Type == ToolSwitchType.Boolean)
                {
                    if (!Switch.Value.BooleanValue)
                        continue;
                }

                if (Switch.Value.SwitchValue.Length != 0)
                {
                    if (CommendLine.Length != 0)
                        CommendLine += ' ';

                    CommendLine += Switch.Value.SwitchValue;
                }


                if (Switch.Value.Type == ToolSwitchType.File)
                {
                    if (Switch.Value.Value.Length != 0)
                    {
                        CommendLine += ' ';
                        CommendLine += '\"';
                        CommendLine += Switch.Value.Value;
                        CommendLine += '\"';
                    }
                }
                else if (Switch.Value.Type == ToolSwitchType.ITaskItemArray)
                {
                    foreach(var Item in Switch.Value.TaskItemArray)
                    {
                        CommendLine += ' ';
                        CommendLine += '\"';
                        CommendLine += Item.ItemSpec;
                        CommendLine += '\"';
                    }
                }
            }

            var _AlwaysAppend = AlwaysAppend;

            if (CommendLine.Length != 0 && _AlwaysAppend.Length != 0)
            {
                CommendLine += ' ';
                CommendLine += _AlwaysAppend;
            }

            return CommendLine;
        }
    }
}
