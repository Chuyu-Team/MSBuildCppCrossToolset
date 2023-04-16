using Microsoft.Build.Framework;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Microsoft.Build.CPPTasks.Common
{
    // https://learn.microsoft.com/zh-cn/visualstudio/msbuild/setenv-task?view=vs-2022
    public class SetEnv : Microsoft.Build.Utilities.Task
    {
        private string val = string.Empty;
        private string? outputEnvironmentVariable;

        [Required]
        public string Name { get; set; }


        public string Value 
        {
            get
            {
                return val;
            }
            set
            {
                val = value;
            }
        }

        [Required]
        public bool Prefix { get; set; }

        public string Target { get; set; }
        public string? Verbosity { get; set; }

        [Output]
        public string? OutputEnvironmentVariable => outputEnvironmentVariable;

        public SetEnv()
        {
            Target = "Process";
        }

        public override bool Execute()
        {
            EnvironmentVariableTarget environmentVariableTarget = EnvironmentVariableTarget.Process;
            if (string.Compare(Target, "User", StringComparison.OrdinalIgnoreCase) == 0)
            {
                environmentVariableTarget = EnvironmentVariableTarget.User;
            }
            else if (string.Compare(Target, "Machine", StringComparison.OrdinalIgnoreCase) == 0)
            {
                environmentVariableTarget = EnvironmentVariableTarget.Machine;
            }
            if (Prefix)
            {
                string environmentVariable = Environment.GetEnvironmentVariable(Name, environmentVariableTarget);
                outputEnvironmentVariable = Environment.ExpandEnvironmentVariables(Value + environmentVariable);
            }
            else
            {
                outputEnvironmentVariable = Environment.ExpandEnvironmentVariables(Value);
            }
            Environment.SetEnvironmentVariable(Name, outputEnvironmentVariable, environmentVariableTarget);

            MessageImportance importance = MessageImportance.Low;
            if (!string.IsNullOrEmpty(Verbosity))
            {
                try
                {
                    importance = (MessageImportance)Enum.Parse(typeof(MessageImportance), Verbosity, ignoreCase: true);
                }
                catch(ArgumentException)
                {
                    // 什么也不做，反正只是个输出
                }
            }


            Log.LogMessage(importance, Name + "=" + outputEnvironmentVariable);

            return true;
        }
    }
}