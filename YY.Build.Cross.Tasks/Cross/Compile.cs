using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Collections;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;
// using Microsoft.Build.Linux.Shared;
// using Microsoft.Build.Linux.Tasks;
using Microsoft.Build.Utilities;
using System.Resources;
using Microsoft.Build.Shared;
using System.Text;
using System;

namespace YY.Build.Cross.Tasks.Cross
{
    public class Compile : TrackedVCToolTask
    {
        public Compile()
            : base(Microsoft.Build.CppTasks.Common.Properties.Microsoft_Build_CPPTasks_Strings.ResourceManager)
        {
            UseCommandProcessor = false;

            switchOrderList = new ArrayList();
            switchOrderList.Add("AlwaysAppend");
            switchOrderList.Add("CompileAs");
            switchOrderList.Add("Sources");
            switchOrderList.Add("BuildingInIde");
            switchOrderList.Add("AdditionalIncludeDirectories");
            switchOrderList.Add("DebugInformationFormat");
            switchOrderList.Add("ObjectFileName");
            switchOrderList.Add("WarningLevel");
            switchOrderList.Add("TreatWarningAsError");
            switchOrderList.Add("AdditionalWarning");
            switchOrderList.Add("Verbose");
            switchOrderList.Add("Optimization");
            switchOrderList.Add("StrictAliasing");
            switchOrderList.Add("UnrollLoops");
            switchOrderList.Add("WholeProgramOptimization");
            switchOrderList.Add("OmitFramePointers");
            switchOrderList.Add("NoCommonBlocks");
            switchOrderList.Add("PreprocessorDefinitions");
            switchOrderList.Add("UndefinePreprocessorDefinitions");
            switchOrderList.Add("UndefineAllPreprocessorDefinitions");
            switchOrderList.Add("ShowIncludes");
            switchOrderList.Add("PositionIndependentCode");
            switchOrderList.Add("ThreadSafeStatics");
            switchOrderList.Add("FloatingPointModel");
            switchOrderList.Add("HideInlineMethods");
            switchOrderList.Add("SymbolsHiddenByDefault");
            switchOrderList.Add("ExceptionHandling");
            switchOrderList.Add("RuntimeTypeInfo");
            switchOrderList.Add("LanguageStandard_C");
            switchOrderList.Add("LanguageStandard");
            switchOrderList.Add("ForcedIncludeFiles");
            switchOrderList.Add("EnableASAN");
            switchOrderList.Add("AdditionalOptions");
            switchOrderList.Add("Sysroot");
            switchOrderList.Add("DependenceFile");

            base.IgnoreUnknownSwitchValues = true;
        }

        private bool PreprocessToFile = false;
        private bool MinimalRebuild = false;

        private ArrayList switchOrderList;

        private Dictionary<string, ITaskItem> trackedInputFilesToRemove;

        private Dictionary<string, ITaskItem> trackedOutputFilesToRemove;

        protected override ArrayList SwitchOrderList => switchOrderList;

        protected override string ToolName => "g++";

        protected override string AlwaysAppend => "-c";

        protected override ITaskItem[] TrackedInputFiles => Sources;

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

        private string[] objectFiles;

        [Output]
        public string[] ObjectFiles
        {
            get
            {
                return objectFiles;
            }
            set
            {
                objectFiles = value;
            }
        }

        [Required]
        public ITaskItem[] Sources
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
                toolSwitch.TaskItemFullPath = true;
                base.ActiveToolSwitches.Add("Sources", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string[] AdditionalIncludeDirectories
        {
            get
            {
                if (IsPropertySet("AdditionalIncludeDirectories"))
                {
                    return base.ActiveToolSwitches["AdditionalIncludeDirectories"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("AdditionalIncludeDirectories");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringPathArray);
                toolSwitch.DisplayName = "Additional Include Directories";
                toolSwitch.Description = "Specifies one or more directories to add to the include path; separate with semi-colons if more than one. (-I[path]).";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-I ";
                toolSwitch.Name = "AdditionalIncludeDirectories";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("AdditionalIncludeDirectories", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string DebugInformationFormat
        {
            get
            {
                if (IsPropertySet("DebugInformationFormat"))
                {
                    return base.ActiveToolSwitches["DebugInformationFormat"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("DebugInformationFormat");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "Debug Information Format";
                toolSwitch.Description = "Specifies the type of debugging information generated by the compiler.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                string[][] switchMap = new string[][]
                {
                    new string[2] { "None", "-g0" },
                    new string[2] { "OldStyle", "-g2 -gdwarf-2" },
                    new string[2] { "ProgramDatabase", "-g2 -gdwarf-2" },
                    new string[2] { "EditAndContinue", "-g2 -gdwarf-2" },

                    // Linux工具集兼容
                    new string[2] { "Minimal", "-g1" },
                    new string[2] { "FullDebug", "-g2 -gdwarf-2" }
                };
                toolSwitch.SwitchValue = ReadSwitchMap("DebugInformationFormat", switchMap, value);
                toolSwitch.Name = "DebugInformationFormat";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("DebugInformationFormat", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string ObjectFileName
        {
            get
            {
                if (IsPropertySet("ObjectFileName"))
                {
                    return base.ActiveToolSwitches["ObjectFileName"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ObjectFileName");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.File);
                toolSwitch.DisplayName = "Object File Name";
                toolSwitch.Description = "Specifies a name to override the default object file name; can be file or directory name. (/Fo[name]).";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-o ";
                toolSwitch.Name = "ObjectFileName";
                toolSwitch.Value = value;
                base.ActiveToolSwitches.Add("ObjectFileName", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string WarningLevel
        {
            get
            {
                if (IsPropertySet("WarningLevel"))
                {
                    return base.ActiveToolSwitches["WarningLevel"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("WarningLevel");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "Warning Level";
                toolSwitch.Description = "Select how strict you want the compiler to be about code errors.  Other flags should be added directly to Additional Options. (/w, /Weverything).";
                toolSwitch.ArgumentRelationList = new ArrayList();
                string[][] switchMap = new string[][]
                {
                    new string[2] { "TurnOffAllWarnings", "-w" },
                    new string[2] { "Level1", "-Wall" },
                    new string[2] { "Level2", "-Wall" },
                    new string[2] { "Level3", "-Wall" },
                    new string[2] { "Level4", "-Wall - Wextra" },

                    // 微软Linux工作集附带，顺道兼容一下。
                    new string[2] { "EnableAllWarnings", "-Wall" },
                };
                toolSwitch.SwitchValue = ReadSwitchMap("WarningLevel", switchMap, value);
                toolSwitch.Name = "WarningLevel";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("WarningLevel", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool TreatWarningAsError
        {
            get
            {
                if (IsPropertySet("TreatWarningAsError"))
                {
                    return base.ActiveToolSwitches["TreatWarningAsError"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("TreatWarningAsError");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Treat Warnings As Errors";
                toolSwitch.Description = "Treats all compiler warnings as errors. For a new project, it may be best to use /WX in all compilations; resolving all warnings will ensure the fewest possible hard-to-find code defects.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-Werror";
                toolSwitch.Name = "TreatWarningAsError";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("TreatWarningAsError", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string[] AdditionalWarning
        {
            get
            {
                if (IsPropertySet("AdditionalWarning"))
                {
                    return base.ActiveToolSwitches["AdditionalWarning"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("AdditionalWarning");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringArray);
                toolSwitch.DisplayName = "Additional Warnings";
                toolSwitch.Description = "Defines a set of additional warning messages.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-W";
                toolSwitch.Name = "AdditionalWarning";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("AdditionalWarning", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool Verbose
        {
            get
            {
                if (IsPropertySet("Verbose"))
                {
                    return base.ActiveToolSwitches["Verbose"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("Verbose");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Enable Verbose mode";
                toolSwitch.Description = "When Verbose mode is enabled, this tool would print out more information that for diagnosing the build.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-v";
                toolSwitch.Name = "Verbose";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("Verbose", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string Optimization
        {
            get
            {
                if (IsPropertySet("Optimization"))
                {
                    return base.ActiveToolSwitches["Optimization"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("Optimization");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "Optimization";
                toolSwitch.Description = "Specifies the optimization level for the application.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                string[][] switchMap = new string[5][]
                {
                    new string[2] { "Custom", "" },
                    new string[2] { "Disabled", "-O0" },
                    new string[2] { "MinSize", "-Os" },
                    new string[2] { "MaxSpeed", "-O2" },
                    new string[2] { "Full", "-O3" }
                };
                toolSwitch.SwitchValue = ReadSwitchMap("Optimization", switchMap, value);
                toolSwitch.Name = "Optimization";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("Optimization", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool StrictAliasing
        {
            get
            {
                if (IsPropertySet("StrictAliasing"))
                {
                    return base.ActiveToolSwitches["StrictAliasing"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("StrictAliasing");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Strict Aliasing";
                toolSwitch.Description = "Assume the strictest aliasing rules.  An object of one type will never be assumed to reside at the same address as an object of a different type.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-fstrict-aliasing";
                toolSwitch.ReverseSwitchValue = "-fno-strict-aliasing";
                toolSwitch.Name = "StrictAliasing";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("StrictAliasing", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool UnrollLoops
        {
            get
            {
                if (IsPropertySet("UnrollLoops"))
                {
                    return base.ActiveToolSwitches["UnrollLoops"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("UnrollLoops");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Unroll Loops";
                toolSwitch.Description = "Unroll loops to make application faster by reducing number of branches executed at the cost of larger code size.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-funroll-all-loops";
                toolSwitch.Name = "UnrollLoops";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("UnrollLoops", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool WholeProgramOptimization
        {
            get
            {
                if (IsPropertySet("WholeProgramOptimization"))
                {
                    return base.ActiveToolSwitches["WholeProgramOptimization"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("WholeProgramOptimization");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Link Time Optimization";
                toolSwitch.Description = "Enable Inter-Procedural optimizations by allowing the optimizer to look across object files in your application.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-flto";
                toolSwitch.Name = "WholeProgramOptimization";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("WholeProgramOptimization", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool OmitFramePointers
        {
            get
            {
                if (IsPropertySet("OmitFramePointers"))
                {
                    return base.ActiveToolSwitches["OmitFramePointers"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("OmitFramePointers");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Omit Frame Pointer";
                toolSwitch.Description = "Suppresses creation of frame pointers on the call stack.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-fomit-frame-pointer";
                toolSwitch.ReverseSwitchValue = "-fno-omit-frame-pointer";
                toolSwitch.Name = "OmitFramePointers";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("OmitFramePointers", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool NoCommonBlocks
        {
            get
            {
                if (IsPropertySet("NoCommonBlocks"))
                {
                    return base.ActiveToolSwitches["NoCommonBlocks"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("NoCommonBlocks");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "No Common Blocks";
                toolSwitch.Description = "Allocate even unintialized global variables in the data section of the object file, rather then generating them as common blocks";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-fno-common";
                toolSwitch.Name = "NoCommonBlocks";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("NoCommonBlocks", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string[] PreprocessorDefinitions
        {
            get
            {
                if (IsPropertySet("PreprocessorDefinitions"))
                {
                    return base.ActiveToolSwitches["PreprocessorDefinitions"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("PreprocessorDefinitions");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringArray);
                toolSwitch.DisplayName = "Preprocessor Definitions";
                toolSwitch.Description = "Defines a preprocessing symbols for your source file. (-D)";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-D";
                toolSwitch.Name = "PreprocessorDefinitions";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("PreprocessorDefinitions", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string[] UndefinePreprocessorDefinitions
        {
            get
            {
                if (IsPropertySet("UndefinePreprocessorDefinitions"))
                {
                    return base.ActiveToolSwitches["UndefinePreprocessorDefinitions"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("UndefinePreprocessorDefinitions");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringArray);
                toolSwitch.DisplayName = "Undefine Preprocessor Definitions";
                toolSwitch.Description = "Specifies one or more preprocessor undefines.  (-U [macro])";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-U";
                toolSwitch.Name = "UndefinePreprocessorDefinitions";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("UndefinePreprocessorDefinitions", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool UndefineAllPreprocessorDefinitions
        {
            get
            {
                if (IsPropertySet("UndefineAllPreprocessorDefinitions"))
                {
                    return base.ActiveToolSwitches["UndefineAllPreprocessorDefinitions"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("UndefineAllPreprocessorDefinitions");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Undefine All Preprocessor Definitions";
                toolSwitch.Description = "Undefine all previously defined preprocessor values.  (-undef)";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-undef";
                toolSwitch.Name = "UndefineAllPreprocessorDefinitions";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("UndefineAllPreprocessorDefinitions", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool ShowIncludes
        {
            get
            {
                if (IsPropertySet("ShowIncludes"))
                {
                    return base.ActiveToolSwitches["ShowIncludes"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ShowIncludes");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Show Includes";
                toolSwitch.Description = "Generates a list of include files with compiler output.  (-H)";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-H";
                toolSwitch.Name = "ShowIncludes";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("ShowIncludes", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool PositionIndependentCode
        {
            get
            {
                if (IsPropertySet("PositionIndependentCode"))
                {
                    return base.ActiveToolSwitches["PositionIndependentCode"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("PositionIndependentCode");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Position Independent Code";
                toolSwitch.Description = "Generate Position Independent Code (PIC) for use in a shared library.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-fpic";
                toolSwitch.Name = "PositionIndependentCode";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("PositionIndependentCode", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool ThreadSafeStatics
        {
            get
            {
                if (IsPropertySet("ThreadSafeStatics"))
                {
                    return base.ActiveToolSwitches["ThreadSafeStatics"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ThreadSafeStatics");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Statics are thread safe";
                toolSwitch.Description = "Emit Extra code to use routines specified in C++ ABI for thread safe initilization of local statics.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-fthreadsafe-statics";
                toolSwitch.ReverseSwitchValue = "-fno-threadsafe-statics";
                toolSwitch.Name = "ThreadSafeStatics";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("ThreadSafeStatics", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        // 类似于微软Linux工具集的RelaxIEEE。
        public string FloatingPointModel
        {
            get
            {
                if (IsPropertySet("FloatingPointModel"))
                {
                    return base.ActiveToolSwitches["FloatingPointModel"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("FloatingPointModel");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "设置浮点模型";
                toolSwitch.Description = "设置浮点模型。";
                toolSwitch.ArgumentRelationList = new ArrayList();
                string[][] switchMap = new string[][]
                {
                    new string[2] { "Precise", "" },
                    new string[2] { "Strict", "" },
                    new string[2] { "Fast", "-ffast-math" },
                };
                toolSwitch.SwitchValue = ReadSwitchMap("FloatingPointModel", switchMap, value);
                toolSwitch.Name = "FloatingPointModel";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("FloatingPointModel", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool HideInlineMethods
        {
            get
            {
                if (IsPropertySet("HideInlineMethods"))
                {
                    return base.ActiveToolSwitches["HideInlineMethods"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("HideInlineMethods");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Inline Methods Hidden";
                toolSwitch.Description = "When enabled, out-of-line copies of inline methods are declared 'private extern'.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-fvisibility-inlines-hidden";
                toolSwitch.Name = "HideInlineMethods";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("HideInlineMethods", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool SymbolsHiddenByDefault
        {
            get
            {
                if (IsPropertySet("SymbolsHiddenByDefault"))
                {
                    return base.ActiveToolSwitches["SymbolsHiddenByDefault"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("SymbolsHiddenByDefault");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Symbol Hiddens By Default";
                toolSwitch.Description = "All symbols are declared 'private extern' unless explicitly marked to be exported using the '__attribute' macro.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-fvisibility=hidden";
                toolSwitch.Name = "SymbolsHiddenByDefault";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("SymbolsHiddenByDefault", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string ExceptionHandling
        {
            get
            {
                if (IsPropertySet("ExceptionHandling"))
                {
                    return base.ActiveToolSwitches["ExceptionHandling"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ExceptionHandling");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "Enable C++ Exceptions";
                toolSwitch.Description = "Specifies the model of exception handling to be used by the compiler.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                string[][] switchMap = new string[][]
                {
                    // 特意兼容微软
                    new string[2] { "false", "-fno-exceptions" },
                    new string[2] { "Async", "-fexceptions" },
                    new string[2] { "Sync", "-fexceptions" },
                    new string[2] { "SyncCThrow", "-fexceptions" },

                    // 微软Linux工具集附带，顺道也兼容一下。
                    new string[2] { "Disabled", "-fno-exceptions" },
                    new string[2] { "Enabled", "-fexceptions" }
                };
                toolSwitch.SwitchValue = ReadSwitchMap("ExceptionHandling", switchMap, value);
                toolSwitch.Name = "ExceptionHandling";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("ExceptionHandling", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool RuntimeTypeInfo
        {
            get
            {
                if (IsPropertySet("RuntimeTypeInfo"))
                {
                    return base.ActiveToolSwitches["RuntimeTypeInfo"].BooleanValue;
                }
                return false;
            }
            set
            {
                base.ActiveToolSwitches.Remove("RuntimeTypeInfo");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.Boolean);
                toolSwitch.DisplayName = "Enable Run-Time Type Information";
                toolSwitch.Description = "Adds code for checking C++ object types at run time (runtime type information).     (frtti, fno-rtti)";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-frtti";
                toolSwitch.ReverseSwitchValue = "-fno-rtti";
                toolSwitch.Name = "RuntimeTypeInfo";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("RuntimeTypeInfo", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string LanguageStandard_C
        {
            get
            {
                if (IsPropertySet("LanguageStandard_C"))
                {
                    return base.ActiveToolSwitches["LanguageStandard_C"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("LanguageStandard_C");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "C Language Standard";
                toolSwitch.Description = "Determines the C language standard.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                string[][] switchMap = new string[][]
                {
                    new string[2] { "Default", "" },
                    new string[2] { "stdc11", "-std=c11" },
                    new string[2] { "stdc17", "-std=c17" },

                    // Linux工具集兼容
                    new string[2] { "c89", "-std=c89" },
                    new string[2] { "gnu90", "-std=gnu90" }, // 附加
                    new string[2] { "iso9899:199409", "-std=iso9899:199409" },
                    new string[2] { "c99", "-std=c99" },
                    new string[2] { "c11", "-std=c11" },
                    new string[2] { "c2x", "-std=c2x" }, // 附加
                    new string[2] { "gnu89", "-std=gnu89" },
                    new string[2] { "gnu99", "-std=gnu99" },
                    new string[2] { "gnu11", "-std=gnu11" },
                    new string[2] { "gnu17", "-std=gnu17" }, // 附加
                };
                toolSwitch.SwitchValue = ReadSwitchMap("LanguageStandard_C", switchMap, value);
                toolSwitch.Name = "LanguageStandard_C";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("LanguageStandard_C", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string LanguageStandard
        {
            get
            {
                if (IsPropertySet("LanguageStandard"))
                {
                    return base.ActiveToolSwitches["LanguageStandard"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("LanguageStandard");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "C++ Language Standard";
                toolSwitch.Description = "Determines the C++ language standard.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                string[][] switchMap = new string[][]
                {
                    new string[2] { "Default", "" },
                    new string[2] { "stdcpp14", "-std=c++14" },
                    new string[2] { "stdcpp17", "-std=c++17" },
                    new string[2] { "stdcpp20", "-std=c++20" },
                    new string[2] { "stdcpplatest", "-std=c++2b" },

                    // Linux工具集兼容
                    new string[2] { "c++98", "-std=c++98" },
                    new string[2] { "c++03", "-std=c++03" },
                    new string[2] { "c++11", "-std=c++11" },
                    new string[2] { "c++1y", "-std=c++14" },
                    new string[2] { "c++14", "-std=c++14" },
                    new string[2] { "c++17", "-std=c++17" },
                    new string[2] { "c++2a", "-std=c++2a" },
                    new string[2] { "c++20", "-std=c++20" },
                    new string[2] { "c++2b", "-std=c++2b" }, // 附加
                    new string[2] { "gnu++98", "-std=gnu++98" },
                    new string[2] { "gnu++03", "-std=gnu++03" },
                    new string[2] { "gnu++11", "-std=gnu++11" },
                    new string[2] { "gnu++1y", "-std=gnu++1y" },
                    new string[2] { "gnu++14", "-std=gnu++14" },
                    new string[2] { "gnu++1z", "-std=gnu++1z" },
                    new string[2] { "gnu++17", "-std=gnu++17" },
                    new string[2] { "gnu++20", "-std=gnu++20" },
                    new string[2] { "gnu++2b", "-std=gnu++2b" }, // 附加
                };
                toolSwitch.SwitchValue = ReadSwitchMap("LanguageStandard", switchMap, value);
                toolSwitch.Name = "LanguageStandard";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("LanguageStandard", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string CompileAs
        {
            get
            {
                if (IsPropertySet("CompileAs"))
                {
                    return base.ActiveToolSwitches["CompileAs"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("CompileAs");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "Compile As";
                toolSwitch.Description = "Select compile language option for .c and .cpp files.  'Default' will detect based on .c or .cpp extention. (-x c, -x c++)";
                toolSwitch.ArgumentRelationList = new ArrayList();
                string[][] switchMap = new string[3][]
                {
                    new string[2] { "Default", "" },
                    new string[2] { "CompileAsC", "-x c" },
                    new string[2] { "CompileAsCpp", "-x c++" }
                };
                toolSwitch.SwitchValue = ReadSwitchMap("CompileAs", switchMap, value);
                toolSwitch.Name = "CompileAs";
                toolSwitch.Value = value;
                toolSwitch.MultipleValues = true;
                base.ActiveToolSwitches.Add("CompileAs", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public string[] ForcedIncludeFiles
        {
            get
            {
                if (IsPropertySet("ForcedIncludeFiles"))
                {
                    return base.ActiveToolSwitches["ForcedIncludeFiles"].StringList;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("ForcedIncludeFiles");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.StringPathArray);
                toolSwitch.DisplayName = "Forced Include Files";
                toolSwitch.Description = "one or more forced include files.     (-include [name])";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-include ";
                toolSwitch.Name = "ForcedIncludeFiles";
                toolSwitch.StringList = value;
                base.ActiveToolSwitches.Add("ForcedIncludeFiles", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public bool EnableASAN
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
                toolSwitch.Description = "Compiles program with AddressSanitizer. Compile with -fno-omit-frame-pointer and compiler optimization level -Os or -O0 for best results. Must link with Address Sanitizer option too. Must run with debugger to view diagnostic results.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-fsanitize=address";
                toolSwitch.Name = "EnableASAN";
                toolSwitch.BooleanValue = value;
                base.ActiveToolSwitches.Add("EnableASAN", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        public virtual string Sysroot
        {
            get
            {
                if (IsPropertySet("Sysroot"))
                {
                    return base.ActiveToolSwitches["Sysroot"].Value;
                }
                return null;
            }
            set
            {
                base.ActiveToolSwitches.Remove("Sysroot");
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.String);
                toolSwitch.DisplayName = "Sysroot";
                toolSwitch.Description = "Folder path to the root directory for headers and libraries.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.Name = "Sysroot";
                toolSwitch.Value = value;
                toolSwitch.SwitchValue = "--sysroot=";
                base.ActiveToolSwitches.Add("Sysroot", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        protected override bool GenerateCostomCommandsAccordingToType(CommandLineBuilder builder, string switchName, bool dummyForBackwardCompatibility, CommandLineFormat format = CommandLineFormat.ForBuildLog, EscapeFormat escapeFormat = EscapeFormat.Default)
        {
            if (string.Equals(switchName, "DependenceFile", StringComparison.OrdinalIgnoreCase))
            {
                ToolSwitch toolSwitch = new ToolSwitch(ToolSwitchType.File);
                toolSwitch.DisplayName = "DependenceFile";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.SwitchValue = "-MD -MF ";
                toolSwitch.Name = "DependenceFile";

                if (IsPropertySet("ObjectFileName"))
                {
                    toolSwitch.Value = base.ActiveToolSwitches["ObjectFileName"].Value + ".d";
                }
                else if(Sources.Length !=0)
                {
                    toolSwitch.Value = Environment.ExpandEnvironmentVariables(Sources[0].ItemSpec) + ".d";
                }
                else
                {
                    return true;
                }

                GenerateCommandsAccordingToType(builder, toolSwitch, format, escapeFormat);
                return true;
            }
            return false;
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            foreach (var Item in Sources)
            {
                Log.LogMessage(MessageImportance.High, Item.ItemSpec);
            }

            return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
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
                toolSwitch.Description = "Tracker Log Directory.";
                toolSwitch.ArgumentRelationList = new ArrayList();
                toolSwitch.Value = VCToolTask.EnsureTrailingSlash(value);
                base.ActiveToolSwitches.Add("TrackerLogDirectory", toolSwitch);
                AddActiveSwitchToolValue(toolSwitch);
            }
        }

        protected override string CommandTLogName
        {
            get
            {
                return "compile." + base.CommandTLogName;
            }
        }

        protected bool InputDependencyFilter(string fullInputPath)
        {
            if (fullInputPath.EndsWith(".PDB", StringComparison.OrdinalIgnoreCase) || fullInputPath.EndsWith(".IDB", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return !trackedInputFilesToRemove.ContainsKey(fullInputPath);
        }

        protected bool OutputDependencyFilter(string fullOutputPath)
        {
            if (fullOutputPath.EndsWith(".TLH", StringComparison.OrdinalIgnoreCase) || fullOutputPath.EndsWith(".TLI", StringComparison.OrdinalIgnoreCase) || fullOutputPath.EndsWith(".DLL", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return !trackedOutputFilesToRemove.ContainsKey(fullOutputPath);
        }

        protected override int PostExecuteTool(int exitCode)
        {
            if (base.MinimalRebuildFromTracking || base.TrackFileAccess)
            {
                base.SourceOutputs = new CanonicalTrackedOutputFiles(base.TLogWriteFiles);
                base.SourceDependencies = new CanonicalTrackedInputFiles(base.TLogReadFiles, Sources, base.ExcludedInputPaths, base.SourceOutputs, UseMinimalRebuildOptimization, MaintainCompositeRootingMarkers);
                DependencyFilter includeInTLog = OutputDependencyFilter;
                DependencyFilter dependencyFilter = InputDependencyFilter;
                trackedInputFilesToRemove = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
                if (base.TrackedInputFilesToIgnore != null)
                {
                    ITaskItem[] array = base.TrackedInputFilesToIgnore;
                    foreach (ITaskItem taskItem in array)
                    {
                        trackedInputFilesToRemove.Add(taskItem.GetMetadata("FullPath"), taskItem);
                    }
                }
                trackedOutputFilesToRemove = new Dictionary<string, ITaskItem>(StringComparer.OrdinalIgnoreCase);
                if (base.TrackedOutputFilesToIgnore != null)
                {
                    ITaskItem[] array2 = base.TrackedOutputFilesToIgnore;
                    foreach (ITaskItem taskItem2 in array2)
                    {
                        trackedOutputFilesToRemove.Add(taskItem2.GetMetadata("FullPath"), taskItem2);
                    }
                }
                //if (PreprocessToFile)
                //{
                //    base.SourceOutputs.RemoveDependenciesFromEntryIfMissing(base.SourcesCompiled, preprocessOutput);
                //    base.SourceDependencies.RemoveDependenciesFromEntryIfMissing(base.SourcesCompiled, preprocessOutput);
                //}
                //else
                {
                    base.SourceOutputs.RemoveDependenciesFromEntryIfMissing(base.SourcesCompiled);
                    base.SourceDependencies.RemoveDependenciesFromEntryIfMissing(base.SourcesCompiled);
                }
                if (exitCode != 0 && !MinimalRebuild)
                {
                    ITaskItem[] array5;
                    ITaskItem[] upToDateSources;
                    if (!PreprocessToFile && base.SourcesCompiled.Length > 1)
                    {
                        KeyValuePair<string, bool>[] array3 = new KeyValuePair<string, bool>[]
                        {
                            new KeyValuePair<string, bool>("ObjectFile", value: true)
                        };
                        ITaskItem[] sources = Sources;
                        foreach (ITaskItem taskItem3 in sources)
                        {
                            string sourceKey = FileTracker.FormatRootingMarker(taskItem3);
                            KeyValuePair<string, bool>[] array4 = array3;
                            for (int l = 0; l < array4.Length; l++)
                            {
                                KeyValuePair<string, bool> keyValuePair = array4[l];
                                string metadata = taskItem3.GetMetadata(keyValuePair.Key);
                                if (keyValuePair.Value && !string.IsNullOrEmpty(metadata))
                                {
                                    base.SourceOutputs.AddComputedOutputForSourceRoot(sourceKey, metadata);
                                }
                            }
                        }
                        array5 = base.SourceDependencies.ComputeSourcesNeedingCompilation();
                        List<ITaskItem> list = new List<ITaskItem>();
                        int num = 0;
                        ITaskItem[] array6 = base.SourcesCompiled;
                        foreach (ITaskItem taskItem4 in array6)
                        {
                            if (num >= array5.Length)
                            {
                                list.Add(taskItem4);
                            }
                            else if (!array5[num].Equals(taskItem4))
                            {
                                list.Add(taskItem4);
                            }
                            else
                            {
                                num++;
                            }
                        }
                        upToDateSources = list.ToArray();
                        ITaskItem[] sources2 = Sources;
                        foreach (ITaskItem taskItem5 in sources2)
                        {
                            string sourceRoot = FileTracker.FormatRootingMarker(taskItem5);
                            KeyValuePair<string, bool>[] array7 = array3;
                            for (int num2 = 0; num2 < array7.Length; num2++)
                            {
                                KeyValuePair<string, bool> keyValuePair2 = array7[num2];
                                string metadata2 = taskItem5.GetMetadata(keyValuePair2.Key);
                                if (keyValuePair2.Value && !string.IsNullOrEmpty(metadata2))
                                {
                                    base.SourceOutputs.RemoveOutputForSourceRoot(sourceRoot, metadata2);
                                }
                            }
                        }
                    }
                    else
                    {
                        array5 = base.SourcesCompiled;
                        upToDateSources = new ITaskItem[0];
                    }
                    // base.SourceOutputs.RemoveEntriesForSource(array5, preprocessOutput);
                    base.SourceOutputs.SaveTlog(includeInTLog);
                    base.SourceDependencies.RemoveEntriesForSource(array5);
                    base.SourceDependencies.SaveTlog(dependencyFilter);
                    ConstructCommandTLog(upToDateSources, dependencyFilter);
                }
                //else if (PreprocessToFile)
                //{
                //    bool flag = true;
                //    if (string.IsNullOrEmpty(PreprocessOutputPath))
                //    {
                //        ITaskItem[] array8 = base.SourcesCompiled;
                //        foreach (ITaskItem source in array8)
                //        {
                //            flag = flag && MovePreprocessedOutput(source, base.SourceDependencies, base.SourceOutputs);
                //        }
                //    }
                //    if (flag)
                //    {
                //        AddPdbToCompactOutputs(base.SourcesCompiled, base.SourceOutputs);
                //        base.SourceOutputs.SaveTlog(includeInTLog);
                //        base.SourceDependencies.SaveTlog(dependencyFilter);
                //        ConstructCommandTLog(base.SourcesCompiled, dependencyFilter);
                //    }
                //}
                else
                {
                    AddPdbToCompactOutputs(base.SourcesCompiled, base.SourceOutputs);
                    RemoveTaskSpecificInputs(base.SourceDependencies);
                    base.SourceOutputs.SaveTlog(includeInTLog);
                    base.SourceDependencies.SaveTlog(dependencyFilter);
                    ConstructCommandTLog(base.SourcesCompiled, dependencyFilter);
                }
                TrackedVCToolTask.DeleteEmptyFile(base.TLogWriteFiles);
                TrackedVCToolTask.DeleteEmptyFile(base.TLogReadFiles);
                TrackedVCToolTask.DeleteFiles(base.TLogDeleteFiles);
            }
            //else if (PreprocessToFile)
            //{
            //    bool flag2 = true;
            //    if (string.IsNullOrEmpty(PreprocessOutputPath))
            //    {
            //        ITaskItem[] array9 = base.SourcesCompiled;
            //        foreach (ITaskItem source2 in array9)
            //        {
            //            flag2 = flag2 && MovePreprocessedOutput(source2, null, null);
            //        }
            //    }
            //    if (!flag2)
            //    {
            //        exitCode = -1;
            //    }
            //}
            return exitCode;
        }

        protected void ConstructCommandTLog(ITaskItem[] upToDateSources, DependencyFilter inputFilter)
        {
            IDictionary<string, string> dictionary = MapSourcesToCommandLines();
            string text = GenerateCommandLineExceptSwitches(new string[1] { "Sources" }, CommandLineFormat.ForTracking);
            if (upToDateSources != null)
            {
                foreach (ITaskItem taskItem in upToDateSources)
                {
                    string metadata = taskItem.GetMetadata("FullPath");
                    if (inputFilter == null || inputFilter(metadata))
                    {
                        dictionary[FileTracker.FormatRootingMarker(taskItem)] = text + " " + metadata/*.ToUpperInvariant()*/;
                    }
                    else
                    {
                        dictionary.Remove(FileTracker.FormatRootingMarker(taskItem));
                    }
                }
            }
            WriteSourcesToCommandLinesTable(dictionary);
        }


        protected void AddPdbToCompactOutputs(ITaskItem[] sources, CanonicalTrackedOutputFiles compactOutputs)
        {
            // todo: gcc的符号？？
        }

        protected void ComputeObjectFiles()
        {
            ObjectFiles = new string[Sources.Length];
            int num = 0;
            ITaskItem[] sources = Sources;
            foreach (ITaskItem taskItem in sources)
            {
                ObjectFiles[num] = Helpers.GetOutputFileName(taskItem.ItemSpec, ObjectFileName, "o");
                taskItem.SetMetadata("ObjectFile", ObjectFiles[num]);
                num++;
            }
        }

        protected override void SaveTracking()
        {
            if(ObjectFiles == null)
                ComputeObjectFiles();

            // 保存Write文件
            {
                string WriteFilePath = TLogWriteFiles[0].GetMetadata("FullPath");
                Directory.CreateDirectory(Path.GetDirectoryName(WriteFilePath));
                using StreamWriter WriteFileWriter = FileUtilities.OpenWrite(WriteFilePath, append: true, Encoding.Unicode);

                KeyValuePair<string, bool>[] array = new KeyValuePair<string, bool>[]
                {
                    new KeyValuePair<string, bool>("ObjectFile", value: true)
                };

                foreach (ITaskItem taskItem in Sources)
                {
                    string sourceKey = FileTracker.FormatRootingMarker(taskItem);
                    WriteFileWriter.WriteLine("^" + sourceKey);
                    KeyValuePair<string, bool>[] array2 = array;
                    for (int j = 0; j < array2.Length; j++)
                    {
                        KeyValuePair<string, bool> keyValuePair = array2[j];
                        string metadata = taskItem.GetMetadata(keyValuePair.Key);
                        if (keyValuePair.Value && !string.IsNullOrEmpty(metadata))
                        {
                            FileUtilities.UpdateFileExistenceCache(metadata);
                            WriteFileWriter.WriteLine(metadata);
                        }
                    }
                }
            }

            // 保存Read文件
            {
                string ReadFilePath = TLogReadFiles[0].GetMetadata("FullPath");
                Directory.CreateDirectory(Path.GetDirectoryName(ReadFilePath));
                using StreamWriter WriteFileWriter = FileUtilities.OpenWrite(ReadFilePath, append: true, Encoding.Unicode);

                GCCMapReader MapReader = new GCCMapReader();

                foreach (ITaskItem taskItem in Sources)
                {
                    var DependenceFile = ObjectFileName + ".d";

                    if (MapReader.Init(DependenceFile))
                    {
                        string sourceKey = FileTracker.FormatRootingMarker(taskItem);
                        WriteFileWriter.WriteLine("^" + sourceKey);

                        for (; ; )
                        {
                            var Tmp = MapReader.ReadLine();
                            if (Tmp == null)
                                break;

                            if (Tmp.Length == 0)
                                continue;

                            WriteFileWriter.WriteLine(FileUtilities.NormalizePath(Tmp));
                        }
                    }

                    try
                    {
                        File.Delete(DependenceFile);
                    }
                    catch
                    {

                    }
                }
            }
        }

        protected internal override bool ComputeOutOfDateSources()
        {
            if (base.MinimalRebuildFromTracking || base.TrackFileAccess)
            {
                AssignDefaultTLogPaths();
            }

#if __REMOVE
            if (PreprocessToFile)
            {
                ComputePreprocessedOutputFiles();
            }
#endif
            if (base.MinimalRebuildFromTracking && !ForcedRebuildRequired())
            {
                base.SourceOutputs = new CanonicalTrackedOutputFiles(this, base.TLogWriteFiles, constructOutputsFromTLogs: false);
                ComputeObjectFiles();
#if __REMOVE
                ComputeBrowseInformationFiles();
                ComputeXmlDocumentationFiles();
#endif
                KeyValuePair<string, bool>[] array = new KeyValuePair<string, bool>[]
                {
                    new KeyValuePair<string, bool>("ObjectFile", value: true)
                    // new KeyValuePair<string, bool>("BrowseInformationFile", BrowseInformation),
                    // new KeyValuePair<string, bool>("XMLDocumentationFileName", GenerateXMLDocumentationFiles)
                };
#if __REMOVE
                if (PreprocessToFile)
                {
                    if (string.IsNullOrEmpty(PreprocessOutputPath))
                    {
                        base.RootSource = FileTracker.FormatRootingMarker(Sources, preprocessOutput);
                        base.SourceOutputs.AddComputedOutputsForSourceRoot(base.RootSource, preprocessOutput);
                    }
                    array[0] = new KeyValuePair<string, bool>("PreprocessOutputFile", value: true);
                }
#endif
                ITaskItem[] sources = Sources;
                foreach (ITaskItem taskItem in sources)
                {
                    string sourceKey = FileTracker.FormatRootingMarker(taskItem);
                    KeyValuePair<string, bool>[] array2 = array;
                    for (int j = 0; j < array2.Length; j++)
                    {
                        KeyValuePair<string, bool> keyValuePair = array2[j];
                        string metadata = taskItem.GetMetadata(keyValuePair.Key);
                        if (keyValuePair.Value && !string.IsNullOrEmpty(metadata))
                        {
                            base.SourceOutputs.AddComputedOutputForSourceRoot(sourceKey, metadata);
                            if (File.Exists(taskItem.GetMetadata("ObjectFile")) && !File.Exists(metadata))
                            {
                                File.Delete(taskItem.GetMetadata("ObjectFile"));
                            }
                        }
                    }
                }

#if __REMOVE
                if (IsPropertySet("PrecompiledHeader") && PrecompiledHeader == "Create" && IsPropertySet("PrecompiledHeaderOutputFile"))
                {
                    ITaskItem[] sources2 = Sources;
                    foreach (ITaskItem source in sources2)
                    {
                        string sourceKey2 = FileTracker.FormatRootingMarker(source);
                        base.SourceOutputs.AddComputedOutputForSourceRoot(sourceKey2, PrecompiledHeaderOutputFile);
                    }
                }
#endif
                base.SourceDependencies = new CanonicalTrackedInputFiles(this, base.TLogReadFiles, Sources, base.ExcludedInputPaths, base.SourceOutputs, useMinimalRebuildOptimization: true, MaintainCompositeRootingMarkers);
                ITaskItem[] sourcesOutOfDateThroughTracking = base.SourceDependencies.ComputeSourcesNeedingCompilation();
                List<ITaskItem> sourcesWithChangedCommandLines = GenerateSourcesOutOfDateDueToCommandLine();
                base.SourcesCompiled = MergeOutOfDateSourceLists(sourcesOutOfDateThroughTracking, sourcesWithChangedCommandLines);
                if (base.SourcesCompiled.Length == 0)
                {
                    base.SkippedExecution = true;
                    return base.SkippedExecution;
                }
                if (!MinimalRebuild || PreprocessToFile)
                {
                    base.SourceDependencies.RemoveEntriesForSource(base.SourcesCompiled);
                    base.SourceDependencies.SaveTlog();
                    if (base.DeleteOutputOnExecute)
                    {
                        TrackedVCToolTask.DeleteFiles(base.SourceOutputs.OutputsForSource(base.SourcesCompiled, searchForSubRootsInCompositeRootingMarkers: false));
                    }
                    base.SourceOutputs = new CanonicalTrackedOutputFiles(this, base.TLogWriteFiles);
                    base.SourceOutputs.RemoveEntriesForSource(base.SourcesCompiled /*, preprocessOutput*/);
                    base.SourceOutputs.SaveTlog();
                    IDictionary<string, string> dictionary = MapSourcesToCommandLines();
                    ITaskItem[] array3 = base.SourcesCompiled;
                    foreach (ITaskItem source2 in array3)
                    {
                        dictionary.Remove(FileTracker.FormatRootingMarker(source2));
                    }
                    WriteSourcesToCommandLinesTable(dictionary);
                }
                AssignOutOfDateSources(base.SourcesCompiled);
            }
            else
            {
                base.SourcesCompiled = Sources;
            }
            if (string.IsNullOrEmpty(base.RootSource))
            {
#if __REMOVE
                if (PreprocessToFile && string.IsNullOrEmpty(PreprocessOutputPath))
                {
                    if (!base.MinimalRebuildFromTracking)
                    {
                        base.RootSource = FileTracker.FormatRootingMarker(Sources, preprocessOutput);
                    }
                }else 
#endif
                if (base.TrackFileAccess)
                {
                    base.RootSource = FileTracker.FormatRootingMarker(base.SourcesCompiled);
                }
            }

#if __REMOVE
            if (base.UseMsbuildResourceManager && !EnableCLServerMode && IsSetToTrue("MultiProcessorCompilation"))
            {
                int num = base.BuildEngine9.RequestCores(base.SourcesCompiled.Length);
                if (ProcessorNumber == 0 || ProcessorNumber > num)
                {
                    ProcessorNumber = num;
                }
            }
#endif
            base.SkippedExecution = false;
            return base.SkippedExecution;
        }
    }
}