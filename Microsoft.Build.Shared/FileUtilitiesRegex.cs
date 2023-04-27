using System.Runtime.CompilerServices;

namespace Microsoft.Build.Shared
{
    internal static class FileUtilitiesRegex
    {
        private static readonly char _backSlash = '\\';

        private static readonly char _forwardSlash = '/';

        internal static bool IsDrivePattern(string pattern)
        {
            if (pattern.Length == 2)
            {
                return StartsWithDrivePattern(pattern);
            }
            return false;
        }

        internal static bool IsDrivePatternWithSlash(string pattern)
        {
            if (pattern.Length == 3)
            {
                return StartsWithDrivePatternWithSlash(pattern);
            }
            return false;
        }

        internal static bool StartsWithDrivePattern(string pattern)
        {
            if (pattern.Length >= 2 && ((pattern[0] >= 'A' && pattern[0] <= 'Z') || (pattern[0] >= 'a' && pattern[0] <= 'z')))
            {
                return pattern[1] == ':';
            }
            return false;
        }

        internal static bool StartsWithDrivePatternWithSlash(string pattern)
        {
            if (pattern.Length >= 3 && StartsWithDrivePattern(pattern))
            {
                if (pattern[2] != _backSlash)
                {
                    return pattern[2] == _forwardSlash;
                }
                return true;
            }
            return false;
        }

        internal static bool IsUncPattern(string pattern)
        {
            return StartsWithUncPatternMatchLength(pattern) == pattern.Length;
        }

        internal static bool StartsWithUncPattern(string pattern)
        {
            return StartsWithUncPatternMatchLength(pattern) != -1;
        }

        internal static int StartsWithUncPatternMatchLength(string pattern)
        {
            if (!MeetsUncPatternMinimumRequirements(pattern))
            {
                return -1;
            }
            bool flag = true;
            bool flag2 = false;
            for (int i = 2; i < pattern.Length; i++)
            {
                if (pattern[i] == _backSlash || pattern[i] == _forwardSlash)
                {
                    if (flag)
                    {
                        return -1;
                    }
                    if (flag2)
                    {
                        return i;
                    }
                    flag2 = true;
                    flag = true;
                }
                else
                {
                    flag = false;
                }
            }
            if (!flag2)
            {
                return -1;
            }
            return pattern.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool MeetsUncPatternMinimumRequirements(string pattern)
        {
            if (pattern.Length >= 5 && (pattern[0] == _backSlash || pattern[0] == _forwardSlash))
            {
                if (pattern[1] != _backSlash)
                {
                    return pattern[1] == _forwardSlash;
                }
                return true;
            }
            return false;
        }
    }
}
