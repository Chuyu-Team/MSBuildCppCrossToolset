using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.CppTasks.Common
{
    public class VCMessage : Microsoft.Build.Utilities.Task
    {
        private string code;

        private string type;

        private string _importance;

        private string arguments;

        [Required]
        public string Code
        {
            get
            {
                return code;
            }
            set
            {
                code = value;
            }
        }

        public string Type
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

        public string Importance
        {
            get
            {
                return _importance;
            }
            set
            {
                _importance = value;
            }
        }

        public string Arguments
        {
            get
            {
                return arguments;
            }
            set
            {
                arguments = value;
            }
        }

        private static object[] ParseArguments(string arguments)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                return null;
            }
            List<string> list = new List<string>();
            bool flag = false;
            int i = 0;
            int num = 0;
            for (; i < arguments.Length; i++)
            {
                if (arguments[i] == ';' && !flag)
                {
                    list.Add(arguments.Substring(num, i - num).Replace("\\;", ";"));
                    num = i + 1;
                }
                else if (i == arguments.Length - 1)
                {
                    list.Add(arguments.Substring(num, i - num + 1).Replace("\\;", ";"));
                }
                flag = arguments[i] == '\\';
            }
            return list.ToArray();
        }

        public VCMessage()
            : base(Microsoft.Build.CppTasks.Common.Properties.Microsoft_Build_CPPTasks_Strings.ResourceManager)
        {
            
        }

        public override bool Execute()
        {
            if (string.IsNullOrEmpty(Type))
            {
                Type = "Warning";
            }
            try
            {
                if (string.Equals(Type, "Warning", StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogWarningWithCodeFromResources("VCMessage." + Code, ParseArguments(Arguments));
                    return true;
                }
                if (string.Equals(Type, "Error", StringComparison.OrdinalIgnoreCase))
                {
                    Log.LogErrorWithCodeFromResources("VCMessage." + Code, ParseArguments(Arguments));
                    return false;
                }
                if (string.Equals(Type, "Message", StringComparison.OrdinalIgnoreCase))
                {
                    MessageImportance val = MessageImportance.Normal;
                    try
                    {
                        val = (MessageImportance)Enum.Parse(typeof(MessageImportance), Importance, ignoreCase: true);
                    }
                    catch (ArgumentException)
                    {
                        Log.LogErrorWithCodeFromResources("Message.InvalidImportance", new object[1] { Importance });
                        return false;
                    }
                    Log.LogMessageFromResources(val, "VCMessage." + Code, ParseArguments(Arguments));
                    return true;
                }
                Log.LogErrorWithCodeFromResources("VCMessage.InvalidType", new object[1] { Type });
                return false;
            }
            catch (ArgumentException)
            {
                Log.LogErrorWithCodeFromResources("VCMessage.InvalidCode", new object[1] { Code });
                return false;
            }
        }
    }
}
