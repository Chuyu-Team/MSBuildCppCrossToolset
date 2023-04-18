using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.CPPTasks
{
    public class PropertyRelation
    {
        public string Argument { get; set; }

        public string Value { get; set; }

        public bool Required { get; set; }

        public PropertyRelation()
        {
        }

        public PropertyRelation(string argument, string value, bool required)
        {
            Argument = argument;
            Value = value;
            Required = required;
        }
    }
}
