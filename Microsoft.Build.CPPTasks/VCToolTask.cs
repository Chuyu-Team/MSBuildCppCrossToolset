using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Build.CPPTasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Shared;
using Microsoft.Build.Utilities;

namespace Microsoft.Build.CPPTasks
{
    public abstract class VCToolTask : ToolTask
    {
        public enum CommandLineFormat
        {
            ForBuildLog,
            ForTracking
        }

        [Flags]
        public enum EscapeFormat
        {
            Default = 0,
            EscapeTrailingSlash = 1
        }

        protected class MessageStruct
        {
            public string Category { get; set; } = "";


            public string SubCategory { get; set; } = "";


            public string Code { get; set; } = "";


            public string Filename { get; set; } = "";


            public int Line { get; set; }

            public int Column { get; set; }

            public string Text { get; set; } = "";


            public void Clear()
            {
                Category = "";
                SubCategory = "";
                Code = "";
                Filename = "";
                Line = 0;
                Column = 0;
                Text = "";
            }

            public static void Swap(ref MessageStruct lhs, ref MessageStruct rhs)
            {
                MessageStruct messageStruct = lhs;
                lhs = rhs;
                rhs = messageStruct;
            }
        }

        private Dictionary<string, ToolSwitch> activeToolSwitchesValues = new Dictionary<string, ToolSwitch>();

#if __REMOVE
        private IntPtr cancelEvent;

        private string cancelEventName;
#endif

        private bool fCancelled;

        private Dictionary<string, ToolSwitch> activeToolSwitches = new Dictionary<string, ToolSwitch>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, Dictionary<string, string>> values = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        private string additionalOptions = string.Empty;

        private char prefix = '/';

        private TaskLoggingHelper logPrivate;

        protected List<Regex> errorListRegexList = new List<Regex>();

        protected List<Regex> errorListRegexListExclusion = new List<Regex>();

        protected MessageStruct lastMS = new MessageStruct();

        protected MessageStruct currentMS = new MessageStruct();

        protected Dictionary<string, ToolSwitch> ActiveToolSwitches => activeToolSwitches;

        protected static Regex FindBackSlashInPath { get; } = new Regex("(?<=[^\\\\])\\\\(?=[^\\\\\\\"\\s])|(\\\\(?=[^\\\"]))|((?<=[^\\\\][\\\\])\\\\(?=[\\\"]))", RegexOptions.Compiled);


        public string AdditionalOptions
        {
            get
            {
                return additionalOptions;
            }
            set
            {
                additionalOptions = TranslateAdditionalOptions(value);
            }
        }

        public bool UseMsbuildResourceManager { get; set; }

        protected override Encoding ResponseFileEncoding => Encoding.Unicode;

        protected virtual ArrayList SwitchOrderList => null;

#if __REMOVE
        protected string CancelEventName => cancelEventName;
#endif

        protected TaskLoggingHelper LogPrivate => logPrivate;

        protected override MessageImportance StandardOutputLoggingImportance => MessageImportance.High;

        protected override MessageImportance StandardErrorLoggingImportance => MessageImportance.High;

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

        public ITaskItem[] ErrorListRegex
        {
            set
            {
                foreach (ITaskItem taskItem in value)
                {
                    errorListRegexList.Add(new Regex(taskItem.ItemSpec, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100.0)));
                }
            }
        }

        public ITaskItem[] ErrorListListExclusion
        {
            set
            {
                foreach (ITaskItem taskItem in value)
                {
                    errorListRegexListExclusion.Add(new Regex(taskItem.ItemSpec, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromMilliseconds(100.0)));
                }
            }
        }

        public bool EnableErrorListRegex { get; set; } = true;


        public bool ForceSinglelineErrorListRegex { get; set; }

        public virtual string[] AcceptableNonzeroExitCodes { get; set; }

        public Dictionary<string, ToolSwitch> ActiveToolSwitchesValues
        {
            get
            {
                return activeToolSwitchesValues;
            }
            set
            {
                activeToolSwitchesValues = value;
            }
        }

        public string EffectiveWorkingDirectory { get; set; }

        [Output]
        public string ResolvedPathToTool { get; protected set; }

        protected bool IgnoreUnknownSwitchValues { get; set; }

        protected VCToolTask(ResourceManager taskResources)
            : base(taskResources)
        {
#if __REMOVE
            cancelEventName = "MSBuildConsole_CancelEvent" + Guid.NewGuid().ToString("N");
            cancelEvent = VCTaskNativeMethods.CreateEventW(IntPtr.Zero, bManualReset: false, bInitialState: false, cancelEventName);
#endif
            fCancelled = false;
            logPrivate = new TaskLoggingHelper(this);
#if __REMOVE
            logPrivate.TaskResources = Microsoft.Build.Shared.AssemblyResources.PrimaryResources;
#endif
            logPrivate.HelpKeywordPrefix = "MSBuild.";
            IgnoreUnknownSwitchValues = false;
        }

        protected virtual string TranslateAdditionalOptions(string options)
        {
            return options;
        }

        protected override string GetWorkingDirectory()
        {
            return EffectiveWorkingDirectory;
        }

        protected override string GenerateFullPathToTool()
        {
            return ToolName;
        }

        protected override bool ValidateParameters()
        {
            if (!logPrivate.HasLoggedErrors)
            {
                return !base.Log.HasLoggedErrors;
            }
            return false;
        }

        public string GenerateCommandLine(CommandLineFormat format = CommandLineFormat.ForBuildLog, EscapeFormat escapeFormat = EscapeFormat.Default)
        {
            string text = GenerateCommandLineCommands(format, escapeFormat);
            string text2 = GenerateResponseFileCommands(format, escapeFormat);
            if (!string.IsNullOrEmpty(text))
            {
                return text + " " + text2;
            }
            return text2;
        }

        public string GenerateCommandLineExceptSwitches(string[] switchesToRemove, CommandLineFormat format = CommandLineFormat.ForBuildLog, EscapeFormat escapeFormat = EscapeFormat.Default)
        {
            string text = GenerateCommandLineCommandsExceptSwitches(switchesToRemove, format, escapeFormat);
            string text2 = GenerateResponseFileCommandsExceptSwitches(switchesToRemove, format, escapeFormat);
            if (!string.IsNullOrEmpty(text))
            {
                return text + " " + text2;
            }
            return text2;
        }

        // Linux下对 ResponseFile 支持不好，所以与 CommandLine 调换。
        protected virtual string /*GenerateCommandLineCommandsExceptSwitches*/GenerateResponseFileCommandsExceptSwitches(string[] switchesToRemove, CommandLineFormat format = CommandLineFormat.ForBuildLog, EscapeFormat escapeFormat = EscapeFormat.Default)
        {
            return string.Empty;
        }

        protected override string GenerateResponseFileCommands()
        {
            return GenerateResponseFileCommands(CommandLineFormat.ForBuildLog, EscapeFormat.Default);
        }

        protected virtual string GenerateResponseFileCommands(CommandLineFormat format, EscapeFormat escapeFormat)
        {
            return GenerateResponseFileCommandsExceptSwitches(new string[0], format, escapeFormat);
        }

        protected override string GenerateCommandLineCommands()
        {
            return GenerateCommandLineCommands(CommandLineFormat.ForBuildLog, EscapeFormat.Default);
        }

        protected virtual string GenerateCommandLineCommands(CommandLineFormat format, EscapeFormat escapeFormat)
        {
            return GenerateCommandLineCommandsExceptSwitches(new string[0], format, escapeFormat);
        }

        protected virtual bool GenerateCostomCommandsAccordingToType(CommandLineBuilder builder, string switchName, bool dummyForBackwardCompatibility, CommandLineFormat format = CommandLineFormat.ForBuildLog, EscapeFormat escapeFormat = EscapeFormat.Default)
        {
            return false;
        }

        protected virtual string /*GenerateResponseFileCommandsExceptSwitches*/GenerateCommandLineCommandsExceptSwitches(string[] switchesToRemove, CommandLineFormat format = CommandLineFormat.ForBuildLog, EscapeFormat escapeFormat = EscapeFormat.Default)
        {
            bool flag = false;
            AddDefaultsToActiveSwitchList();
            AddFallbacksToActiveSwitchList();
            PostProcessSwitchList();
            CommandLineBuilder commandLineBuilder = new CommandLineBuilder(quoteHyphensOnCommandLine: true);
            foreach (string switchOrder in SwitchOrderList)
            {
                if (GenerateCostomCommandsAccordingToType(commandLineBuilder, switchOrder, dummyForBackwardCompatibility: false, format, escapeFormat))
                {
                    // 已经处理
                }
                else if(IsPropertySet(switchOrder))
                {
                    ToolSwitch toolSwitch = activeToolSwitches[switchOrder];
                    if (!VerifyDependenciesArePresent(toolSwitch) || !VerifyRequiredArgumentsArePresent(toolSwitch, throwOnError: false))
                    {
                        continue;
                    }
                    bool flag2 = true;
                    if (switchesToRemove != null)
                    {
                        foreach (string value in switchesToRemove)
                        {
                            if (switchOrder.Equals(value, StringComparison.OrdinalIgnoreCase))
                            {
                                flag2 = false;
                                break;
                            }
                        }
                    }
                    if (flag2 && !IsArgument(toolSwitch))
                    {
                        GenerateCommandsAccordingToType(commandLineBuilder, toolSwitch, dummyForBackwardCompatibility: false, format, escapeFormat);
                    }
                } 
                else if (string.Equals(switchOrder, "additionaloptions", StringComparison.OrdinalIgnoreCase))
                {
                    BuildAdditionalArgs(commandLineBuilder);
                    flag = true;
                }
                else if (string.Equals(switchOrder, "AlwaysAppend", StringComparison.OrdinalIgnoreCase))
                {
                    commandLineBuilder.AppendSwitch(AlwaysAppend);
                }
            }
            if (!flag)
            {
                BuildAdditionalArgs(commandLineBuilder);
            }
            return commandLineBuilder.ToString();
        }

        protected override bool HandleTaskExecutionErrors()
        {
            if (IsAcceptableReturnValue())
            {
                return true;
            }
            return base.HandleTaskExecutionErrors();
        }

        public override bool Execute()
        {
            if (fCancelled)
            {
                return false;
            }
            bool result = base.Execute();
#if __REMOVE
            VCTaskNativeMethods.CloseHandle(cancelEvent);
#endif
            PrintMessage(ParseLine(null), base.StandardOutputImportanceToUse);
            return result;
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            ResolvedPathToTool = Environment.ExpandEnvironmentVariables(pathToTool);
            return base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
        }

        public override void Cancel()
        {
            fCancelled = true;
#if __REMOVE
            VCTaskNativeMethods.SetEvent(cancelEvent);
#endif
            base.Cancel();
        }

        protected bool VerifyRequiredArgumentsArePresent(ToolSwitch property, bool throwOnError)
        {
            if (property.ArgumentRelationList != null)
            {
                foreach (ArgumentRelation argumentRelation in property.ArgumentRelationList)
                {
                    if (argumentRelation.Required && (property.Value == argumentRelation.Value || argumentRelation.Value == string.Empty) && !HasSwitch(argumentRelation.Argument))
                    {
                        string text = "";
                        text = ((!(string.Empty == argumentRelation.Value)) ? base.Log.FormatResourceString("MissingRequiredArgumentWithValue", argumentRelation.Argument, property.Name, argumentRelation.Value) : base.Log.FormatResourceString("MissingRequiredArgument", argumentRelation.Argument, property.Name));
                        base.Log.LogError(text);
                        if (throwOnError)
                        {
                            throw new LoggerException(text);
                        }
                        return false;
                    }
                }
            }
            return true;
        }

        protected bool IsAcceptableReturnValue()
        {
            return IsAcceptableReturnValue(base.ExitCode);
        }

        protected bool IsAcceptableReturnValue(int code)
        {
            if (AcceptableNonzeroExitCodes != null)
            {
                string[] acceptableNonzeroExitCodes = AcceptableNonzeroExitCodes;
                foreach (string value in acceptableNonzeroExitCodes)
                {
                    if (code == Convert.ToInt32(value, CultureInfo.InvariantCulture))
                    {
                        return true;
                    }
                }
            }
            return code == 0;
        }

        protected void RemoveSwitchToolBasedOnValue(string switchValue)
        {
            if (ActiveToolSwitchesValues.Count > 0 && ActiveToolSwitchesValues.ContainsKey("/" + switchValue))
            {
                ToolSwitch toolSwitch = ActiveToolSwitchesValues["/" + switchValue];
                if (toolSwitch != null)
                {
                    ActiveToolSwitches.Remove(toolSwitch.Name);
                }
            }
        }

        protected void AddActiveSwitchToolValue(ToolSwitch switchToAdd)
        {
            if (switchToAdd.Type != 0 || switchToAdd.BooleanValue)
            {
                if (switchToAdd.SwitchValue != string.Empty)
                {
                    ActiveToolSwitchesValues.Add(switchToAdd.SwitchValue, switchToAdd);
                }
            }
            else if (switchToAdd.ReverseSwitchValue != string.Empty)
            {
                ActiveToolSwitchesValues.Add(switchToAdd.ReverseSwitchValue, switchToAdd);
            }
        }

        protected string GetEffectiveArgumentsValues(ToolSwitch property, CommandLineFormat format = CommandLineFormat.ForBuildLog)
        {
            StringBuilder stringBuilder = new StringBuilder();
            bool flag = false;
            string text = string.Empty;
            if (property.ArgumentRelationList != null)
            {
                foreach (ArgumentRelation argumentRelation in property.ArgumentRelationList)
                {
                    if (text != string.Empty && text != argumentRelation.Argument)
                    {
                        flag = true;
                    }
                    text = argumentRelation.Argument;
                    if ((property.Value == argumentRelation.Value || argumentRelation.Value == string.Empty || (property.Type == ToolSwitchType.Boolean && property.BooleanValue)) && HasSwitch(argumentRelation.Argument))
                    {
                        ToolSwitch toolSwitch = ActiveToolSwitches[argumentRelation.Argument];
                        stringBuilder.Append(argumentRelation.Separator);
                        CommandLineBuilder commandLineBuilder = new CommandLineBuilder();
                        GenerateCommandsAccordingToType(commandLineBuilder, toolSwitch, dummyForBackwardCompatibility: true, format);
                        stringBuilder.Append(commandLineBuilder.ToString());
                    }
                }
            }
            CommandLineBuilder commandLineBuilder2 = new CommandLineBuilder();
            if (flag)
            {
                commandLineBuilder2.AppendSwitchIfNotNull("", stringBuilder.ToString());
            }
            else
            {
                commandLineBuilder2.AppendSwitchUnquotedIfNotNull("", stringBuilder.ToString());
            }
            return commandLineBuilder2.ToString();
        }

        protected virtual void PostProcessSwitchList()
        {
            ValidateRelations();
            ValidateOverrides();
        }

        protected virtual void ValidateRelations()
        {
        }

        protected virtual void ValidateOverrides()
        {
            List<string> list = new List<string>();
            foreach (KeyValuePair<string, ToolSwitch> activeToolSwitch in ActiveToolSwitches)
            {
                foreach (KeyValuePair<string, string> @override in activeToolSwitch.Value.Overrides)
                {
                    if (!string.Equals(@override.Key, (activeToolSwitch.Value.Type == ToolSwitchType.Boolean && !activeToolSwitch.Value.BooleanValue) ? activeToolSwitch.Value.ReverseSwitchValue.TrimStart('/') : activeToolSwitch.Value.SwitchValue.TrimStart('/'), StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    foreach (KeyValuePair<string, ToolSwitch> activeToolSwitch2 in ActiveToolSwitches)
                    {
                        if (!string.Equals(activeToolSwitch2.Key, activeToolSwitch.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            if (string.Equals(activeToolSwitch2.Value.SwitchValue.TrimStart('/'), @override.Value, StringComparison.OrdinalIgnoreCase))
                            {
                                list.Add(activeToolSwitch2.Key);
                                break;
                            }
                            if (activeToolSwitch2.Value.Type == ToolSwitchType.Boolean && !activeToolSwitch2.Value.BooleanValue && string.Equals(activeToolSwitch2.Value.ReverseSwitchValue.TrimStart('/'), @override.Value, StringComparison.OrdinalIgnoreCase))
                            {
                                list.Add(activeToolSwitch2.Key);
                                break;
                            }
                        }
                    }
                }
            }
            foreach (string item in list)
            {
                ActiveToolSwitches.Remove(item);
            }
        }

        protected bool IsSwitchValueSet(string switchValue)
        {
            if (!string.IsNullOrEmpty(switchValue))
            {
                return ActiveToolSwitchesValues.ContainsKey("/" + switchValue);
            }
            return false;
        }

        protected virtual bool VerifyDependenciesArePresent(ToolSwitch value)
        {
            if (value.Parents.Count > 0)
            {
                bool flag = false;
                {
                    foreach (string parent in value.Parents)
                    {
                        flag = flag || HasDirectSwitch(parent);
                    }
                    return flag;
                }
            }
            return true;
        }

        protected virtual void AddDefaultsToActiveSwitchList()
        {
        }

        protected virtual void AddFallbacksToActiveSwitchList()
        {
        }

        protected virtual void GenerateCommandsAccordingToType(CommandLineBuilder builder, ToolSwitch toolSwitch, bool dummyForBackwardCompatibility, CommandLineFormat format = CommandLineFormat.ForBuildLog, EscapeFormat escapeFormat = EscapeFormat.Default)
        {
            GenerateCommandsAccordingToType(builder, toolSwitch, format, escapeFormat);
        }

        protected virtual void GenerateCommandsAccordingToType(CommandLineBuilder builder, ToolSwitch toolSwitch, CommandLineFormat format = CommandLineFormat.ForBuildLog, EscapeFormat escapeFormat = EscapeFormat.Default)
        {
            try
            {
                switch (toolSwitch.Type)
                {
                    case ToolSwitchType.Boolean:
                        EmitBooleanSwitch(builder, toolSwitch, format);
                        break;
                    case ToolSwitchType.String:
                        EmitStringSwitch(builder, toolSwitch);
                        break;
                    case ToolSwitchType.StringArray:
                        EmitStringArraySwitch(builder, toolSwitch);
                        break;
                    case ToolSwitchType.StringPathArray:
                        EmitStringArraySwitch(builder, toolSwitch, format, escapeFormat);
                        break;
                    case ToolSwitchType.Integer:
                        EmitIntegerSwitch(builder, toolSwitch);
                        break;
                    case ToolSwitchType.File:
                        EmitFileSwitch(builder, toolSwitch, format);
                        break;
                    case ToolSwitchType.Directory:
                        EmitDirectorySwitch(builder, toolSwitch, format);
                        break;
                    case ToolSwitchType.ITaskItem:
                        EmitTaskItemSwitch(builder, toolSwitch);
                        break;
                    case ToolSwitchType.ITaskItemArray:
                        EmitTaskItemArraySwitch(builder, toolSwitch, format);
                        break;
                    case ToolSwitchType.AlwaysAppend:
                        EmitAlwaysAppendSwitch(builder, toolSwitch);
                        break;
                    default:
                        Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(condition: false, "InternalError");
                        break;
                }
            }
            catch (Exception ex)
            {
                base.Log.LogErrorFromResources("GenerateCommandLineError", toolSwitch.Name, toolSwitch.ValueAsString, ex.Message);
                ex.RethrowIfCritical();
            }
        }

        protected void BuildAdditionalArgs(CommandLineBuilder cmdLine)
        {
            if (cmdLine != null && !string.IsNullOrEmpty(additionalOptions))
            {
                cmdLine.AppendSwitch(Environment.ExpandEnvironmentVariables(additionalOptions));
            }
        }

        protected bool ValidateInteger(string switchName, int min, int max, int value)
        {
            if (value < min || value > max)
            {
                logPrivate.LogErrorFromResources("ArgumentOutOfRange", switchName, value);
                return false;
            }
            return true;
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
                if (!IgnoreUnknownSwitchValues)
                {
                    logPrivate.LogErrorFromResources("ArgumentOutOfRange", propertyName, value);
                }
            }
            return string.Empty;
        }

        protected bool IsPropertySet(string propertyName)
        {
            return activeToolSwitches.ContainsKey(propertyName);
        }

        protected bool IsSetToTrue(string propertyName)
        {
            if (activeToolSwitches.ContainsKey(propertyName))
            {
                return activeToolSwitches[propertyName].BooleanValue;
            }
            return false;
        }

        protected bool IsExplicitlySetToFalse(string propertyName)
        {
            if (activeToolSwitches.ContainsKey(propertyName))
            {
                return !activeToolSwitches[propertyName].BooleanValue;
            }
            return false;
        }

        protected bool IsArgument(ToolSwitch property)
        {
            if (property != null && property.Parents.Count > 0)
            {
                if (string.IsNullOrEmpty(property.SwitchValue))
                {
                    return true;
                }
                foreach (string parent in property.Parents)
                {
                    if (!activeToolSwitches.TryGetValue(parent, out var value))
                    {
                        continue;
                    }
                    foreach (ArgumentRelation argumentRelation in value.ArgumentRelationList)
                    {
                        if (argumentRelation.Argument.Equals(property.Name, StringComparison.Ordinal))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        protected bool HasSwitch(string propertyName)
        {
            if (IsPropertySet(propertyName))
            {
                return !string.IsNullOrEmpty(activeToolSwitches[propertyName].Name);
            }
            return false;
        }

        protected bool HasDirectSwitch(string propertyName)
        {
            if (activeToolSwitches.TryGetValue(propertyName, out var value) && !string.IsNullOrEmpty(value.Name))
            {
                if (value.Type == ToolSwitchType.Boolean)
                {
                    return value.BooleanValue;
                }
                return true;
            }
            return false;
        }

        protected static string EnsureTrailingSlash(string directoryName)
        {
            Microsoft.Build.Shared.ErrorUtilities.VerifyThrow(directoryName != null, "InternalError");
            if (directoryName != null && directoryName.Length > 0)
            {
                char c = directoryName[directoryName.Length - 1];
                if (c != Path.DirectorySeparatorChar && c != Path.AltDirectorySeparatorChar)
                {
                    directoryName += Path.DirectorySeparatorChar;
                }
            }
            return directoryName;
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            if (EnableErrorListRegex && errorListRegexList.Count > 0)
            {
                PrintMessage(ParseLine(singleLine), messageImportance);
                if (ForceSinglelineErrorListRegex)
                {
                    PrintMessage(ParseLine(null), messageImportance);
                }
            }
            else
            {
                base.LogEventsFromTextOutput(singleLine, messageImportance);
            }
        }

        protected virtual void PrintMessage(MessageStruct message, MessageImportance messageImportance)
        {
            if (message != null && message.Text.Length > 0)
            {
                switch (message.Category)
                {
                    case "fatal error":
                    case "error":
                        base.Log.LogError(message.SubCategory, message.Code, null, message.Filename, message.Line, message.Column, 0, 0, message.Text.TrimEnd());
                        break;
                    case "warning":
                        base.Log.LogWarning(message.SubCategory, message.Code, null, message.Filename, message.Line, message.Column, 0, 0, message.Text.TrimEnd());
                        break;
                    case "note":
                        base.Log.LogCriticalMessage(message.SubCategory, message.Code, null, message.Filename, message.Line, message.Column, 0, 0, message.Text.TrimEnd());
                        break;
                    default:
                        base.Log.LogMessage(messageImportance, message.Text.TrimEnd());
                        break;
                }
                message.Clear();
            }
        }

        protected virtual MessageStruct ParseLine(string inputLine)
        {
            if (inputLine == null)
            {
                MessageStruct.Swap(ref lastMS, ref currentMS);
                currentMS.Clear();
                return lastMS;
            }
            if (string.IsNullOrWhiteSpace(inputLine))
            {
                return null;
            }
            bool flag = false;
            foreach (Regex item in errorListRegexListExclusion)
            {
                try
                {
                    Match match = item.Match(inputLine);
                    if (match.Success)
                    {
                        flag = true;
                        break;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                }
                catch (Exception e)
                {
                    ReportRegexException(inputLine, item, e);
                }
            }
            if (!flag)
            {
                foreach (Regex errorListRegex in errorListRegexList)
                {
                    try
                    {
                        Match match2 = errorListRegex.Match(inputLine);
                        if (match2.Success)
                        {
                            int result = 0;
                            int result2 = 0;
                            if (!int.TryParse(match2.Groups["LINE"].Value, out result))
                            {
                                result = 0;
                            }
                            if (!int.TryParse(match2.Groups["COLUMN"].Value, out result2))
                            {
                                result2 = 0;
                            }
                            MessageStruct.Swap(ref lastMS, ref currentMS);
                            currentMS.Clear();
                            currentMS.Category = match2.Groups["CATEGORY"].Value.ToLowerInvariant();
                            currentMS.SubCategory = match2.Groups["SUBCATEGORY"].Value.ToLowerInvariant();
                            currentMS.Filename = match2.Groups["FILENAME"].Value;
                            currentMS.Code = match2.Groups["CODE"].Value;
                            currentMS.Line = result;
                            currentMS.Column = result2;
                            MessageStruct messageStruct = currentMS;
                            messageStruct.Text = messageStruct.Text + match2.Groups["TEXT"].Value.TrimEnd() + Environment.NewLine;
                            flag = true;
                            return lastMS;
                        }
                    }
                    catch (RegexMatchTimeoutException)
                    {
                    }
                    catch (Exception e2)
                    {
                        ReportRegexException(inputLine, errorListRegex, e2);
                    }
                }
            }
            if (!flag && !string.IsNullOrEmpty(currentMS.Filename))
            {
                MessageStruct messageStruct2 = currentMS;
                messageStruct2.Text = messageStruct2.Text + inputLine.TrimEnd() + Environment.NewLine;
                return null;
            }
            MessageStruct.Swap(ref lastMS, ref currentMS);
            currentMS.Clear();
            currentMS.Text = inputLine;
            return lastMS;
        }

        protected void ReportRegexException(string inputLine, Regex regex, Exception e)
        {
#if __REMOVE
            if (Microsoft.Build.Shared.ExceptionHandling.IsCriticalException(e))
            {
                if (e is OutOfMemoryException)
                {
                    int cacheSize = Regex.CacheSize;
                    Regex.CacheSize = 0;
                    Regex.CacheSize = cacheSize;
                }
                base.Log.LogErrorWithCodeFromResources("TrackedVCToolTask.CannotParseToolOutput", inputLine, regex.ToString(), e.Message);
                e.Rethrow();
            }
            else
#endif
            {
                base.Log.LogWarningWithCodeFromResources("TrackedVCToolTask.CannotParseToolOutput", inputLine, regex.ToString(), e.Message);
            }
        }

        private static void EmitAlwaysAppendSwitch(CommandLineBuilder builder, ToolSwitch toolSwitch)
        {
            builder.AppendSwitch(toolSwitch.Name);
        }

        private static void EmitTaskItemArraySwitch(CommandLineBuilder builder, ToolSwitch toolSwitch, CommandLineFormat format = CommandLineFormat.ForBuildLog)
        {
            if (string.IsNullOrEmpty(toolSwitch.Separator))
            {
                ITaskItem[] taskItemArray = toolSwitch.TaskItemArray;
                foreach (ITaskItem taskItem in taskItemArray)
                {
                    builder.AppendSwitchIfNotNull(toolSwitch.SwitchValue, Environment.ExpandEnvironmentVariables(taskItem.ItemSpec));
                }
                return;
            }
            ITaskItem[] array = new ITaskItem[toolSwitch.TaskItemArray.Length];
            for (int j = 0; j < toolSwitch.TaskItemArray.Length; j++)
            {
                array[j] = new TaskItem(Environment.ExpandEnvironmentVariables(toolSwitch.TaskItemArray[j].ItemSpec));
                //if (format == CommandLineFormat.ForTracking)
                //{
                //    array[j].ItemSpec = array[j].ItemSpec.ToUpperInvariant();
                //}
            }
            builder.AppendSwitchIfNotNull(toolSwitch.SwitchValue, array, toolSwitch.Separator);
        }

        private static void EmitTaskItemSwitch(CommandLineBuilder builder, ToolSwitch toolSwitch)
        {
            if (!string.IsNullOrEmpty(toolSwitch.TaskItem.ItemSpec))
            {
                builder.AppendFileNameIfNotNull(Environment.ExpandEnvironmentVariables(toolSwitch.TaskItem.ItemSpec + toolSwitch.Separator));
            }
        }

        private static void EmitDirectorySwitch(CommandLineBuilder builder, ToolSwitch toolSwitch, CommandLineFormat format = CommandLineFormat.ForBuildLog)
        {
            if (!string.IsNullOrEmpty(toolSwitch.SwitchValue))
            {
                if (format == CommandLineFormat.ForBuildLog)
                {
                    builder.AppendSwitch(toolSwitch.SwitchValue + toolSwitch.Separator);
                }
                else
                {
                    builder.AppendSwitch(toolSwitch.SwitchValue.ToUpperInvariant() + toolSwitch.Separator);
                }
            }
        }

        private static void EmitFileSwitch(CommandLineBuilder builder, ToolSwitch toolSwitch, CommandLineFormat format = CommandLineFormat.ForBuildLog)
        {
            if (!string.IsNullOrEmpty(toolSwitch.Value))
            {
                string text = Environment.ExpandEnvironmentVariables(toolSwitch.Value);
                text = text.Trim();
                //if (format == CommandLineFormat.ForTracking)
                //{
                //    text = text.ToUpperInvariant();
                //}
                if (!text.StartsWith("\"", StringComparison.Ordinal))
                {
                    text = "\"" + text;
                    text = ((!text.EndsWith("\\", StringComparison.Ordinal) || text.EndsWith("\\\\", StringComparison.Ordinal)) ? (text + "\"") : (text + "\\\""));
                }
                builder.AppendSwitchUnquotedIfNotNull(toolSwitch.SwitchValue + toolSwitch.Separator, text);
            }
        }

        private void EmitIntegerSwitch(CommandLineBuilder builder, ToolSwitch toolSwitch)
        {
            if (toolSwitch.IsValid)
            {
                if (!string.IsNullOrEmpty(toolSwitch.Separator))
                {
                    builder.AppendSwitch(toolSwitch.SwitchValue + toolSwitch.Separator + toolSwitch.Number.ToString(CultureInfo.InvariantCulture) + GetEffectiveArgumentsValues(toolSwitch));
                }
                else
                {
                    builder.AppendSwitch(toolSwitch.SwitchValue + toolSwitch.Number.ToString(CultureInfo.InvariantCulture) + GetEffectiveArgumentsValues(toolSwitch));
                }
            }
        }

        private static void EmitStringArraySwitch(CommandLineBuilder builder, ToolSwitch toolSwitch, CommandLineFormat format = CommandLineFormat.ForBuildLog, EscapeFormat escapeFormat = EscapeFormat.Default)
        {
            string[] array = new string[toolSwitch.StringList.Length];
            char[] anyOf = new char[11]
            {
            ' ', '|', '<', '>', ',', ';', '-', '\r', '\n', '\t',
            '\f'
            };
            for (int i = 0; i < toolSwitch.StringList.Length; i++)
            {
                string text = ((!toolSwitch.StringList[i].StartsWith("\"", StringComparison.Ordinal) || !toolSwitch.StringList[i].EndsWith("\"", StringComparison.Ordinal)) ? Environment.ExpandEnvironmentVariables(toolSwitch.StringList[i]) : Environment.ExpandEnvironmentVariables(toolSwitch.StringList[i].Substring(1, toolSwitch.StringList[i].Length - 2)));
                if (!string.IsNullOrEmpty(text))
                {
                    //if (format == CommandLineFormat.ForTracking)
                    //{
                    //    text = text.ToUpperInvariant();
                    //}
                    if (escapeFormat.HasFlag(EscapeFormat.EscapeTrailingSlash) && text.IndexOfAny(anyOf) == -1 && text.EndsWith("\\", StringComparison.Ordinal) && !text.EndsWith("\\\\", StringComparison.Ordinal))
                    {
                        text += "\\";
                    }
                    array[i] = text;
                }
            }
            if (string.IsNullOrEmpty(toolSwitch.Separator))
            {
                string[] array2 = array;
                foreach (string parameter in array2)
                {
                    builder.AppendSwitchIfNotNull(toolSwitch.SwitchValue, parameter);
                }
            }
            else
            {
                builder.AppendSwitchIfNotNull(toolSwitch.SwitchValue, array, toolSwitch.Separator);
            }
        }

        private void EmitStringSwitch(CommandLineBuilder builder, ToolSwitch toolSwitch)
        {
            string empty = string.Empty;
            empty = empty + toolSwitch.SwitchValue + toolSwitch.Separator;
            StringBuilder stringBuilder = new StringBuilder(GetEffectiveArgumentsValues(toolSwitch));
            string value = toolSwitch.Value;
            if (!toolSwitch.MultipleValues)
            {
                value = value.Trim();
                if (!value.StartsWith("\"", StringComparison.Ordinal))
                {
                    value = "\"" + value;
                    value = ((!value.EndsWith("\\", StringComparison.Ordinal) || value.EndsWith("\\\\", StringComparison.Ordinal)) ? (value + "\"") : (value + "\\\""));
                }
                stringBuilder.Insert(0, value);
            }
            if (empty.Length != 0 || stringBuilder.ToString().Length != 0)
            {
                builder.AppendSwitchUnquotedIfNotNull(empty, stringBuilder.ToString());
            }
        }

        private void EmitBooleanSwitch(CommandLineBuilder builder, ToolSwitch toolSwitch, CommandLineFormat format = CommandLineFormat.ForBuildLog)
        {
            if (toolSwitch.BooleanValue)
            {
                if (!string.IsNullOrEmpty(toolSwitch.SwitchValue))
                {
                    StringBuilder stringBuilder = new StringBuilder(GetEffectiveArgumentsValues(toolSwitch, format));
                    stringBuilder.Insert(0, toolSwitch.Separator);
                    stringBuilder.Insert(0, toolSwitch.TrueSuffix);
                    stringBuilder.Insert(0, toolSwitch.SwitchValue);
                    builder.AppendSwitch(stringBuilder.ToString());
                }
            }
            else
            {
                EmitReversibleBooleanSwitch(builder, toolSwitch);
            }
        }

        private void EmitReversibleBooleanSwitch(CommandLineBuilder builder, ToolSwitch toolSwitch)
        {
            if (!string.IsNullOrEmpty(toolSwitch.ReverseSwitchValue))
            {
                string value = (toolSwitch.BooleanValue ? toolSwitch.TrueSuffix : toolSwitch.FalseSuffix);
                StringBuilder stringBuilder = new StringBuilder(GetEffectiveArgumentsValues(toolSwitch));
                stringBuilder.Insert(0, value);
                stringBuilder.Insert(0, toolSwitch.Separator);
                stringBuilder.Insert(0, toolSwitch.TrueSuffix);
                stringBuilder.Insert(0, toolSwitch.ReverseSwitchValue);
                builder.AppendSwitch(stringBuilder.ToString());
            }
        }

        private string Prefix(string toolSwitch)
        {
            if (!string.IsNullOrEmpty(toolSwitch) && toolSwitch[0] != prefix)
            {
                return prefix + toolSwitch;
            }
            return toolSwitch;
        }
    }

}
