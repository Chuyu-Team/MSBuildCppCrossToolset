using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Build.Shared;
using Microsoft.NET.StringTools;

namespace Microsoft.Build.Shared
{
    internal static class EscapingUtilities
    {
        private static Dictionary<string, string> s_unescapedToEscapedStrings = new Dictionary<string, string>(StringComparer.Ordinal);

        private static readonly char[] s_charsToEscape = new char[9] { '%', '*', '?', '@', '$', '(', ')', ';', '\'' };

        private static bool TryDecodeHexDigit(char character, out int value)
        {
            if (character >= '0' && character <= '9')
            {
                value = character - 48;
                return true;
            }
            if (character >= 'A' && character <= 'F')
            {
                value = character - 65 + 10;
                return true;
            }
            if (character >= 'a' && character <= 'f')
            {
                value = character - 97 + 10;
                return true;
            }
            value = 0;
            return false;
        }

        internal static string UnescapeAll(string escapedString, bool trim = false)
        {
            if (string.IsNullOrEmpty(escapedString))
            {
                return escapedString;
            }
            int num = escapedString.IndexOf('%');
            if (num == -1)
            {
                if (!trim)
                {
                    return escapedString;
                }
                return escapedString.Trim();
            }
            StringBuilder stringBuilder = StringBuilderCache.Acquire(escapedString.Length);
            int i = 0;
            int num2 = escapedString.Length;
            if (trim)
            {
                for (; i < escapedString.Length && char.IsWhiteSpace(escapedString[i]); i++)
                {
                }
                if (i == escapedString.Length)
                {
                    return string.Empty;
                }
                while (char.IsWhiteSpace(escapedString[num2 - 1]))
                {
                    num2--;
                }
            }
            while (num != -1)
            {
                if (num <= num2 - 3 && TryDecodeHexDigit(escapedString[num + 1], out var value) && TryDecodeHexDigit(escapedString[num + 2], out var value2))
                {
                    stringBuilder.Append(escapedString, i, num - i);
                    char value3 = (char)((value << 4) + value2);
                    stringBuilder.Append(value3);
                    i = num + 3;
                }
                num = escapedString.IndexOf('%', num + 1);
            }
            stringBuilder.Append(escapedString, i, num2 - i);
            return StringBuilderCache.GetStringAndRelease(stringBuilder);
        }

        internal static string EscapeWithCaching(string unescapedString)
        {
            return EscapeWithOptionalCaching(unescapedString, cache: true);
        }

        internal static string Escape(string unescapedString)
        {
            return EscapeWithOptionalCaching(unescapedString, cache: false);
        }

        private static string EscapeWithOptionalCaching(string unescapedString, bool cache)
        {
            if (string.IsNullOrEmpty(unescapedString) || !ContainsReservedCharacters(unescapedString))
            {
                return unescapedString;
            }
            if (cache)
            {
                lock (s_unescapedToEscapedStrings)
                {
                    if (s_unescapedToEscapedStrings.TryGetValue(unescapedString, out var value))
                    {
                        return value;
                    }
                }
            }
            StringBuilder stringBuilder = StringBuilderCache.Acquire(unescapedString.Length * 2);
            AppendEscapedString(stringBuilder, unescapedString);
            if (!cache)
            {
                return StringBuilderCache.GetStringAndRelease(stringBuilder);
            }
            string text = Strings.WeakIntern(stringBuilder.ToString());
            StringBuilderCache.Release(stringBuilder);
            lock (s_unescapedToEscapedStrings)
            {
                s_unescapedToEscapedStrings[unescapedString] = text;
                return text;
            }
        }

        private static bool ContainsReservedCharacters(string unescapedString)
        {
            return -1 != unescapedString.IndexOfAny(s_charsToEscape);
        }

        internal static bool ContainsEscapedWildcards(string escapedString)
        {
            if (escapedString.Length < 3)
            {
                return false;
            }
            for (int num = escapedString.IndexOf('%', 0, escapedString.Length - 2); num != -1; num = escapedString.IndexOf('%', num + 1, escapedString.Length - (num + 1) - 2))
            {
                if (escapedString[num + 1] == '2' && (escapedString[num + 2] == 'a' || escapedString[num + 2] == 'A'))
                {
                    return true;
                }
                if (escapedString[num + 1] == '3' && (escapedString[num + 2] == 'f' || escapedString[num + 2] == 'F'))
                {
                    return true;
                }
            }
            return false;
        }

        private static char HexDigitChar(int x)
        {
            return (char)(x + ((x < 10) ? 48 : 87));
        }

        private static void AppendEscapedChar(StringBuilder sb, char ch)
        {
            sb.Append('%');
            sb.Append(HexDigitChar((int)ch / 16));
            sb.Append(HexDigitChar(ch & 0xF));
        }

        private static void AppendEscapedString(StringBuilder sb, string unescapedString)
        {
            int num = 0;
            while (true)
            {
                int num2 = unescapedString.IndexOfAny(s_charsToEscape, num);
                if (num2 == -1)
                {
                    break;
                }
                sb.Append(unescapedString, num, num2 - num);
                AppendEscapedChar(sb, unescapedString[num2]);
                num = num2 + 1;
            }
            sb.Append(unescapedString, num, unescapedString.Length - num);
        }
    }
}
