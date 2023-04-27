using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.CPPTasks
{
    public sealed class Helpers
    {
        private Helpers()
        {
        }

        public static string GetOutputFileName(string sourceFile, string outputFileOrDir, string outputExtension)
        {
            string text;
            if (string.IsNullOrEmpty(outputFileOrDir))
            {
                text = sourceFile;
                if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(outputExtension))
                {
                    text = Path.ChangeExtension(text, outputExtension);
                }
            }
            else
            {
                text = outputFileOrDir;
                char c = outputFileOrDir[outputFileOrDir.Length - 1];
                if (c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar)
                {
                    text = Path.Combine(text, Path.GetFileName(sourceFile));
                    text = Path.ChangeExtension(text, outputExtension);
                }
                else
                {
                    string extension = Path.GetExtension(text);
                    if (string.IsNullOrEmpty(extension) && !string.IsNullOrEmpty(outputExtension))
                    {
                        text = Path.ChangeExtension(text, outputExtension);
                    }
                }
            }
            if (Path.IsPathRooted(text))
            {
                text = Path.GetFullPath(text);
            }
            return text;
        }
    }
}
