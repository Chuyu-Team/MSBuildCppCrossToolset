using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Build.CppTasks.Common
{
    // todo
    public class GetOutOfDateItems : Microsoft.Build.Utilities.Task
    {
        public ITaskItem[] Sources { get; set; }

        [Required]
        public string OutputsMetadataName { get; set; }

        public string DependenciesMetadataName { get; set; }

        public string CommandMetadataName { get; set; }

        [Required]
        public string TLogDirectory { get; set; }

        [Required]
        public string TLogNamePrefix { get; set; }

        public bool CheckForInterdependencies { get; set; }

        public bool TrackFileAccess { get; set; }

        [Output]
        public ITaskItem[] OutOfDateSources { get; set; }

        [Output]
        public bool HasInterdependencies { get; set; }

        public GetOutOfDateItems()
        {
            CheckForInterdependencies = false;
            HasInterdependencies = false;
            DependenciesMetadataName = null;
            CommandMetadataName = null;
            TrackFileAccess = true;
        }
        public override bool Execute()
        {
            return true;
        }
    }
}
