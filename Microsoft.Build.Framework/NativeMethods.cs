using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Framework
{
    internal class NativeMethods
    {
        static DateTime LastWriteFileUtcTime(string path)
        {
            DateTime result = DateTime.MinValue;
#if __
                if (IsWindows)
                {
                    if (Traits.Instance.EscapeHatches.AlwaysUseContentTimestamp)
                    {
                        return GetContentLastWriteFileUtcTime(path);
                    }
                    WIN32_FILE_ATTRIBUTE_DATA lpFileInformation = default(WIN32_FILE_ATTRIBUTE_DATA);
                    if (GetFileAttributesEx(path, 0, ref lpFileInformation) && (lpFileInformation.fileAttributes & 0x10) == 0)
                    {
                        result = DateTime.FromFileTimeUtc((long)(((ulong)lpFileInformation.ftLastWriteTimeHigh << 32) | lpFileInformation.ftLastWriteTimeLow));
                        if ((lpFileInformation.fileAttributes & 0x400) == 1024 && !Traits.Instance.EscapeHatches.UseSymlinkTimeInsteadOfTargetTime)
                        {
                            result = GetContentLastWriteFileUtcTime(path);
                        }
                    }
                    return result;
                }
#endif
            if (!File.Exists(path))
            {
                return DateTime.MinValue;
            }
            return File.GetLastWriteTimeUtc(path);
        }

        internal static DateTime GetLastWriteFileUtcTime(string fullPath)
        {
#if __
            if (Traits.Instance.EscapeHatches.AlwaysDoImmutableFilesUpToDateCheck)
            {
                return LastWriteFileUtcTime(fullPath);
            }
            bool flag = FileClassifier.Shared.IsNonModifiable(fullPath);
#else
            bool flag = true;
#endif

            if (flag && ImmutableFilesTimestampCache.Shared.TryGetValue(fullPath, out var lastModified))
            {
                return lastModified;
            }
            DateTime dateTime = LastWriteFileUtcTime(fullPath);
            if (flag && dateTime != DateTime.MinValue)
            {
                ImmutableFilesTimestampCache.Shared.TryAdd(fullPath, dateTime);
            }
            return dateTime;

        }

    }
}
