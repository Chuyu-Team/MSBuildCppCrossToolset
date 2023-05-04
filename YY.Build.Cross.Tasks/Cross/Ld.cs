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
using Microsoft.Build.Shared;
using System.IO;

namespace YY.Build.Cross.Tasks.Cross
{
    public class Ld : TrackedVCToolTask
    {
        public Ld()
            : base(Microsoft.Build.CppTasks.Common.Properties.Microsoft_Build_CPPTasks_Strings.ResourceManager)
        {
            switchOrderList = new ArrayList();
            switchOrderList.Add("OutputFile");
            switchOrderList.Add("LinkStatus");
            switchOrderList.Add("Version");
            switchOrderList.Add("ShowProgress");
            switchOrderList.Add("Trace");
            switchOrderList.Add("TraceSymbols");
            switchOrderList.Add("GenerateMapFile");
            switchOrderList.Add("UnresolvedSymbolReferences");
            switchOrderList.Add("OptimizeforMemory");
            switchOrderList.Add("SharedLibrarySearchPath");
            switchOrderList.Add("AdditionalLibraryDirectories");
            switchOrderList.Add("IgnoreSpecificDefaultLibraries");
            switchOrderList.Add("IgnoreDefaultLibraries");
            switchOrderList.Add("ForceUndefineSymbolReferences");
            switchOrderList.Add("DebuggerSymbolInformation");
            switchOrderList.Add("MapFileName");
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

        protected override ITaskItem[] TrackedInputFiles => Sources;

        protected override bool MaintainCompositeRootingMarkers => true;

        protected override string CommandTLogName
        {
            get
            {
                return "ld." + base.CommandTLogName;
            }
        }

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

        public bool LinkStatus
        {
            get
            {
                if (IsPropertySet("LinkStatus"))
                {
                    return base.ActiveToolSwitches["LinkStatus"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("LinkStatus");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "LinkStatus";
                toolSwitch.Description = "Prints Linker Progress Messages.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--stats";
                toolSwitch.Name = "LinkStatus";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("LinkStatus", toolSwitch);
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

        public string ShowProgress
        {
            get
            {
                if (IsPropertySet("ShowProgress"))
                {
                    return base.ActiveToolSwitches["ShowProgress"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ShowProgress");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "Show Progress";
                toolSwitch.Description = "Prints Linker Progress Messages.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                // toolSwitch.SwitchValue = "-Wl,--stats";
                string[][] switchMap = new string[][]
                {
                    new string[2] { "NotSet", "" },
                    new string[2] { "LinkVerbose", "-Wl,--verbose" },
                    new string[2] { "LinkVerboseLib", "" },
                    new string[2] { "LinkVerboseICF", "" }, // 仅兼容MSVC需要
                    new string[2] { "LinkVerboseREF", "" }, // 仅兼容MSVC需要
                    new string[2] { "LinkVerboseSAFESEH", "" }, // 仅兼容MSVC需要
                    new string[2] { "LinkVerboseCLR", "" }, // 仅兼容MSVC需要

                    // Linux工具集兼容
                    new string[2] { "true", "-Wl,--stats" },
                    new string[2] { "false", "" },
                };

                toolSwitch.SwitchValue = ReadSwitchMap("ShowProgress", switchMap, value);
                toolSwitch.Name = "ShowProgress";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("ShowProgress", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        // 仅兼容Linux工具集
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
                // XCode的ld不支持 `--trace`，但是支持 `-t`
                // Linux下的ld能同时支持 `--trace` 与 `-t`
                // 所以我们最终改成 -t
                // toolSwitch.SwitchValue = "-Wl,--trace";
                toolSwitch.SwitchValue = "-Wl,-t";
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

        public bool GenerateMapFile
        {
            get
            {
                if (IsPropertySet("GenerateMapFile"))
                {
                    return base.ActiveToolSwitches["GenerateMapFile"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("GenerateMapFile");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Print Map";
                toolSwitch.Description = "The --print-map option tells the linker to output a link map.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Wl,--print-map";
                toolSwitch.Name = "GenerateMapFile";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("GenerateMapFile", toolSwitch);
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

        public virtual string MapFileName
        {
            get
            {
                if (IsPropertySet("MapFileName"))
                {
                    return base.ActiveToolSwitches["MapFileName"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("MapFileName");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "Map File Name";
                toolSwitch.Description = "The Map option tells the linker to create a map file with the user specified name.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.Name = "MapFileName";
                toolSwitch.Value = value;
                toolSwitch.SwitchValue = "-Wl,-Map=";
                base.ActiveToolSwitches.Add("MapFileName", toolSwitch);
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

        protected override string TrackerIntermediateDirectory
        {
            get
            {
                if (TrackerLogDirectory != null)
                {
                    return TrackerLogDirectory;
                }
                return string.Empty;
            }
        }

        public virtual string TrackerLogDirectory
        {
            get
            {
                if (IsPropertySet("TrackerLogDirectory"))
                {
                    return base.ActiveToolSwitches["TrackerLogDirectory"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("TrackerLogDirectory");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Directory);
                toolSwitch.DisplayName = "Tracker Log Directory";
                toolSwitch.Description = "Tracker log directory.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.Value = VCToolTask.EnsureTrailingSlash(value);
                base.ActiveToolSwitches.Add("TrackerLogDirectory", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        private HashSet<string> ReadObjectFiles;
        private bool? PrintTrace;

        protected override void ValidateRelations()
        {
            if(PrintTrace == null)
            {
                PrintTrace = Trace;

                if (GenerateMapFile)
                    MinimalRebuildFromTracking = false;

                if (MinimalRebuildFromTracking && IsPropertySet("ShowProgress"))
                {
                    if(base.ActiveToolSwitches["ShowProgress"].SwitchValue.Length !=0)
                    {
                        MinimalRebuildFromTracking = false;
                    }
                }

                // 因为最小构建必须要求开启 Trace，否则无法跟踪依赖的使用情况。
                if (MinimalRebuildFromTracking && Trace == false)
                    Trace = true;
            }
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            ReadObjectFiles = null;

            if (MinimalRebuildFromTracking == true)
            {
                ReadObjectFiles = new HashSet<string>();
            }

            return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            do
            {
                if (StandardOutputImportanceToUse != messageImportance)
                    break;

                if (ReadObjectFiles == null)
                    break;

                if (singleLine.Length == 0)
                    break;

                if(singleLine[0] != '/' || singleLine.Contains(": "))
                    break;

                try
                {
                    var ObjPath = FileUtilities.NormalizePath(singleLine);
                    ReadObjectFiles.Add(ObjPath);
                    // Trace如果本身没有开启，那么不向其输出搜索内容，避免内容太多形成干扰。
                    if (PrintTrace != null && PrintTrace == false)
                        return;
                } catch
                {
                }
                
            } while (false);
            
            base.LogEventsFromTextOutput(singleLine, messageImportance);
        }

        protected override void SaveTracking()
        {
            string SourceKey = "^";

            foreach (ITaskItem taskItem in Sources)
            {
                if (SourceKey.Length != 1)
                    SourceKey += '|';

                SourceKey += FileTracker.FormatRootingMarker(taskItem);
            }

            // 保存Write文件
            {
                string WriteFilePath = TLogWriteFiles[0].GetMetadata("FullPath");
                Directory.CreateDirectory(Path.GetDirectoryName(WriteFilePath));
                using StreamWriter WriteFileWriter = FileUtilities.OpenWrite(WriteFilePath, append: true, Encoding.Unicode);

                WriteFileWriter.WriteLine(SourceKey);
                WriteFileWriter.WriteLine(OutputFile);
            }

            // 保存Read文件
            {
                string ReadFilePath = TLogReadFiles[0].GetMetadata("FullPath");
                Directory.CreateDirectory(Path.GetDirectoryName(ReadFilePath));
                using StreamWriter ReadFileWriter = FileUtilities.OpenWrite(ReadFilePath, append: true, Encoding.Unicode);

                ReadFileWriter.WriteLine(SourceKey);

                foreach (var FilePath in ReadObjectFiles)
                {
                    ReadFileWriter.WriteLine(FilePath);
                }
            }
        }
    }
}
