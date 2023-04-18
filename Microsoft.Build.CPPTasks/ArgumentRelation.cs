using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.CPPTasks
{
    public class ArgumentRelation : PropertyRelation
    {
        public string Separator { get; set; }

        public ArgumentRelation(string argument, string value, bool required, string separator)
            : base(argument, value, required)
        {
            Separator = separator;
        }
    }
}
