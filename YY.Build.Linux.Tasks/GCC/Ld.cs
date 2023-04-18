using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;
// using Microsoft.Build.Linux.Shared;
// using Microsoft.Build.Linux.Tasks;
using Microsoft.Build.Utilities;
using System.Text.RegularExpressions;

namespace YY.Build.Linux.Tasks.GCC
{
    public class Ld : Microsoft.Build.CPPTasks.VCToolTask
    {
        public Ld()
        {
            switchOrderList = new ArrayList();
            switchOrderList.Add("OutputFile");
            switchOrderList.Add("ShowProgress");
            switchOrderList.Add("Version");
            switchOrderList.Add("VerboseOutput");
            switchOrderList.Add("Trace");
            switchOrderList.Add("TraceSymbols");
            switchOrderList.Add("PrintMap");
            switchOrderList.Add("UnresolvedSymbolReferences");
            switchOrderList.Add("OptimizeforMemory");
            switchOrderList.Add("SharedLibrarySearchPath");
            switchOrderList.Add("AdditionalLibraryDirectories");
            switchOrderList.Add("IgnoreSpecificDefaultLibraries");
            switchOrderList.Add("IgnoreDefaultLibraries");
            switchOrderList.Add("ForceUndefineSymbolReferences");
            switchOrderList.Add("DebuggerSymbolInformation");
            switchOrderList.Add("GenerateMapFile");
            switchOrderList.Add("Relocation");
            switchOrderList.Add("FunctionBinding");
            switchOrderList.Add("NoExecStackRequired");
            switchOrderList.Add("LinkDll");
            switchOrderList.Add("WholeArchiveBegin");
            switchOrderList.Add("AdditionalOptions");
            switchOrderList.Add("Sources");
            switchOrderList.Add("AdditionalDependencies");
            switchOrderList.Add("WholeArchiveEnd");
            switchOrderList.Add("LibraryDependencies");
            switchOrderList.Add("BuildingInIde");
            switchOrderList.Add("EnableASAN");
            switchOrderList.Add("UseOfStl");
            errorListRegexList.Add(_fileLineTextExpression);
        }

        private ArrayList switchOrderList;

        protected override ArrayList SwitchOrderList => switchOrderList;

        private static Regex _fileLineTextExpression = new Regex("^\\s*(?<FILENAME>[^:]*):(((?<LINE>\\d*):)?)(\\s*(?<CATEGORY>(fatal error|error|warning|note)):)?\\s*(?<TEXT>.*)$", RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100.0));

        protected override string ToolName => "ld";

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
                base.ActiveToolSwitches.Add("OutputFile", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool ShowProgress
        {
            get
            {
                if (IsPropertySet("ShowProgress"))
                {
                    return base.ActiveToolSwitches["ShowProgress"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ShowProgress");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Show Progress";
                toolSwitch.Description = "Prints Linker Progress Messages.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--stats";
                toolSwitch.Name = "ShowProgress";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("ShowProgress", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool Version
        {
            get
            {
                if (IsPropertySet("Version"))
                {
                    return base.ActiveToolSwitches["Version"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("Version");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Version";
                toolSwitch.Description = "The -version option tells the linker to put a version number in the header of the executable.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--version";
                toolSwitch.Name = "Version";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("Version", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool VerboseOutput
        {
            get
            {
                if (IsPropertySet("VerboseOutput"))
                {
                    return base.ActiveToolSwitches["VerboseOutput"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("VerboseOutput");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Enable Verbose Output";
                toolSwitch.Description = "The -verbose option tells the linker to output verbose messages for debugging.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--verbose";
                toolSwitch.Name = "VerboseOutput";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("VerboseOutput", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool Trace
        {
            get
            {
                if (IsPropertySet("Trace"))
                {
                    return base.ActiveToolSwitches["Trace"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("Trace");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Trace";
                toolSwitch.Description = "The --trace option tells the linker to output the input files as are processed.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--trace";
                toolSwitch.Name = "Trace";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("Trace", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string[] TraceSymbols
        {
            get
            {
                if (IsPropertySet("TraceSymbols"))
                {
                    return base.ActiveToolSwitches["TraceSymbols"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("TraceSymbols");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringArray);
                toolSwitch.DisplayName = "Trace Symbols";
                toolSwitch.Description = "Print the list of files in which a symbol appears. (--trace-symbol=symbol)";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--trace-symbol=";
                toolSwitch.Name = "TraceSymbols";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("TraceSymbols", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool PrintMap
        {
            get
            {
                if (IsPropertySet("PrintMap"))
                {
                    return base.ActiveToolSwitches["PrintMap"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("PrintMap");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Print Map";
                toolSwitch.Description = "The --print-map option tells the linker to output a link map.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--print-map";
                toolSwitch.Name = "PrintMap";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("PrintMap", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool UnresolvedSymbolReferences
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
                toolSwitch.SwitchValue = "-Wl,--no-undefined";
                toolSwitch.Name = "UnresolvedSymbolReferences";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("UnresolvedSymbolReferences", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool OptimizeforMemory
        {
            get
            {
                if (IsPropertySet("OptimizeforMemory"))
                {
                    return base.ActiveToolSwitches["OptimizeforMemory"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("OptimizeforMemory");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Optimize For Memory Usage";
                toolSwitch.Description = "Optimize for memory usage, by rereading the symbol tables as necessary.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--no-keep-memory";
                toolSwitch.Name = "OptimizeforMemory";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("OptimizeforMemory", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string[] SharedLibrarySearchPath
        {
            get
            {
                if (IsPropertySet("SharedLibrarySearchPath"))
                {
                    return base.ActiveToolSwitches["SharedLibrarySearchPath"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("SharedLibrarySearchPath");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringPathArray);
                toolSwitch.DisplayName = "Shared Library Search Path";
                toolSwitch.Description = "Allows the user to populate the shared library search path.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,-rpath-link=";
                toolSwitch.Name = "SharedLibrarySearchPath";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("SharedLibrarySearchPath", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string[] AdditionalLibraryDirectories
        {
            get
            {
                if (IsPropertySet("AdditionalLibraryDirectories"))
                {
                    return base.ActiveToolSwitches["AdditionalLibraryDirectories"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("AdditionalLibraryDirectories");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringPathArray);
                toolSwitch.DisplayName = "Additional Library Directories";
                toolSwitch.Description = "Allows the user to override the environmental library path. (-L folder).";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,-L";
                toolSwitch.Name = "AdditionalLibraryDirectories";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("AdditionalLibraryDirectories", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string[] IgnoreSpecificDefaultLibraries
        {
            get
            {
                if (IsPropertySet("IgnoreSpecificDefaultLibraries"))
                {
                    return base.ActiveToolSwitches["IgnoreSpecificDefaultLibraries"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("IgnoreSpecificDefaultLibraries");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringArray);
                toolSwitch.DisplayName = "Ignore Specific Default Libraries";
                toolSwitch.Description = "Specifies one or more names of default libraries to ignore.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--exclude-libs=";
                toolSwitch.Name = "IgnoreSpecificDefaultLibraries";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("IgnoreSpecificDefaultLibraries", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool IgnoreDefaultLibraries
        {
            get
            {
                if (IsPropertySet("IgnoreDefaultLibraries"))
                {
                    return base.ActiveToolSwitches["IgnoreDefaultLibraries"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("IgnoreDefaultLibraries");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Ignore Default Libraries";
                toolSwitch.Description = "Ignore default libraries and only search libraries explicitely specified.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "--Wl,-nostdlib";
                toolSwitch.Name = "IgnoreDefaultLibraries";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("IgnoreDefaultLibraries", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string[] ForceUndefineSymbolReferences
        {
            get
            {
                if (IsPropertySet("ForceUndefineSymbolReferences"))
                {
                    return base.ActiveToolSwitches["ForceUndefineSymbolReferences"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ForceUndefineSymbolReferences");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringArray);
                toolSwitch.DisplayName = "Force Symbol References";
                toolSwitch.Description = "Force symbol to be entered in the output file as an undefined symbol.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,-u--undefined=";
                toolSwitch.Name = "ForceUndefineSymbolReferences";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("ForceUndefineSymbolReferences", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string DebuggerSymbolInformation
        {
            get
            {
                if (IsPropertySet("DebuggerSymbolInformation"))
                {
                    return base.ActiveToolSwitches["DebuggerSymbolInformation"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("DebuggerSymbolInformation");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "Debugger Symbol Information";
                toolSwitch.Description = "Debugger symbol information from the output file.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                string[][] switchMap = new string[5][]
                {
                    new string[2] { "true", "" },
                    new string[2] { "false", "" },
                    new string[2] { "IncludeAll", "" },
                    new string[2] { "OmitDebuggerSymbolInformation", "-Wl,--strip-debug" },
                    new string[2] { "OmitAllSymbolInformation", "-Wl,--strip-all" }
                };
                toolSwitch.SwitchValue = ReadSwitchMap("DebuggerSymbolInformation", switchMap, value);
                toolSwitch.Name = "DebuggerSymbolInformation";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("DebuggerSymbolInformation", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string GenerateMapFile
        {
            get
            {
                if (IsPropertySet("GenerateMapFile"))
                {
                    return base.ActiveToolSwitches["GenerateMapFile"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("GenerateMapFile");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "Map File Name";
                toolSwitch.Description = "The Map option tells the linker to create a map file with the user specified name.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.Name = "GenerateMapFile";
                toolSwitch.Value = value;
                toolSwitch.SwitchValue = "-Wl,-Map=";
                base.ActiveToolSwitches.Add("GenerateMapFile", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool Relocation
        {
            get
            {
                if (IsPropertySet("Relocation"))
                {
                    return base.ActiveToolSwitches["Relocation"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("Relocation");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Mark Variables ReadOnly After Relocation";
                toolSwitch.Description = "This option marks variables read-only after relocation.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,-z,relro";
                toolSwitch.ReverseSwitchValue = "-Wl,-z,norelro";
                toolSwitch.Name = "Relocation";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("Relocation", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool FunctionBinding
        {
            get
            {
                if (IsPropertySet("FunctionBinding"))
                {
                    return base.ActiveToolSwitches["FunctionBinding"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("FunctionBinding");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Enable Immediate Function Binding";
                toolSwitch.Description = "This option marks object for immediate function binding.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,-z,now";
                toolSwitch.Name = "FunctionBinding";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("FunctionBinding", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool NoExecStackRequired
        {
            get
            {
                if (IsPropertySet("NoExecStackRequired"))
                {
                    return base.ActiveToolSwitches["NoExecStackRequired"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("NoExecStackRequired");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Executable Stack Not Required";
                toolSwitch.Description = "This option marks output as not requiring executable stack.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,-z,noexecstack";
                toolSwitch.Name = "NoExecStackRequired";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("NoExecStackRequired", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool LinkDll
        {
            get
            {
                if (IsPropertySet("LinkDll"))
                {
                    return base.ActiveToolSwitches["LinkDll"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("LinkDll");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-shared";
                toolSwitch.Name = "LinkDll";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("LinkDll", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool WholeArchiveBegin
        {
            get
            {
                if (IsPropertySet("WholeArchiveBegin"))
                {
                    return base.ActiveToolSwitches["WholeArchiveBegin"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("WholeArchiveBegin");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Whole Archive";
                toolSwitch.Description = "Whole Archive uses all code from Sources and Additional Dependencies.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--whole-archive";
                toolSwitch.Name = "WholeArchiveBegin";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("WholeArchiveBegin", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
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

        public virtual string[] AdditionalDependencies
        {
            get
            {
                if (IsPropertySet("AdditionalDependencies"))
                {
                    return base.ActiveToolSwitches["AdditionalDependencies"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("AdditionalDependencies");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringArray);
                toolSwitch.DisplayName = "Additional Dependencies";
                toolSwitch.Description = "Specifies additional items to add to the link command line.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.Name = "AdditionalDependencies";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("AdditionalDependencies", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool WholeArchiveEnd
        {
            get
            {
                if (IsPropertySet("WholeArchiveEnd"))
                {
                    return base.ActiveToolSwitches["WholeArchiveEnd"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("WholeArchiveEnd");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--no-whole-archive";
                toolSwitch.Name = "WholeArchiveEnd";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("WholeArchiveEnd", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string[] LibraryDependencies
        {
            get
            {
                if (IsPropertySet("LibraryDependencies"))
                {
                    return base.ActiveToolSwitches["LibraryDependencies"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("LibraryDependencies");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringArray);
                toolSwitch.DisplayName = "Library Dependencies";
                toolSwitch.Description = "This option allows specifying additional libraries to be  added to the linker command line. The additional library will be added to the end of the linker command line  prefixed with 'lib' and end with the '.a' extension.  (-lNAME)";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-l";
                toolSwitch.Name = "LibraryDependencies";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("LibraryDependencies", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool BuildingInIde
        {
            get
            {
                if (IsPropertySet("BuildingInIde"))
                {
                    return base.ActiveToolSwitches["BuildingInIde"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("BuildingInIde");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.Name = "BuildingInIde";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("BuildingInIde", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual bool EnableASAN
        {
            get
            {
                if (IsPropertySet("EnableASAN"))
                {
                    return base.ActiveToolSwitches["EnableASAN"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("EnableASAN");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Enable Address Sanitizer";
                toolSwitch.Description = "Link program with AddressSanitizer. Must also compiler with AddressSanitizer enabled. Must run with debugger to view diagnostic results.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-fsanitize=address";
                toolSwitch.Name = "EnableASAN";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("EnableASAN", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string UseOfStl
        {
            get
            {
                if (IsPropertySet("UseOfStl"))
                {
                    return base.ActiveToolSwitches["UseOfStl"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("UseOfStl");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.ArgumentRelationList = new ArrayList();
                string[][] switchMap = new string[2][]
                {
                    new string[2] { "libstdc++_shared", "" },
                    new string[2] { "libstdc++_static", "-static-libstdc++" }
                };
                toolSwitch.SwitchValue = ReadSwitchMap("UseOfStl", switchMap, value);
                toolSwitch.Name = "UseOfStl";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("UseOfStl", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }
    }
}
