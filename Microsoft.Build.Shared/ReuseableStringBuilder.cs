using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Build.Shared
{
    internal sealed class ReuseableStringBuilder : IDisposable
    {
        private static class ReuseableStringBuilderFactory
        {
            private const int MaxBuilderSize = 1024;

            private static StringBuilder s_sharedBuilder;

            internal static StringBuilder Get(int capacity)
            {
                StringBuilder stringBuilder = Interlocked.Exchange(ref s_sharedBuilder, null);
                if (stringBuilder == null)
                {
                    stringBuilder = new StringBuilder(capacity);
                }
                else if (stringBuilder.Capacity < capacity)
                {
                    stringBuilder.Capacity = capacity;
                }
                return stringBuilder;
            }

            internal static void Release(StringBuilder returningBuilder)
            {
                if (returningBuilder.Capacity < 1024)
                {
                    returningBuilder.Clear();
                    Interlocked.Exchange(ref s_sharedBuilder, returningBuilder);
                }
            }
        }

        private StringBuilder _borrowedBuilder;

        private int _capacity;

        public int Length
        {
            get
            {
                if (_borrowedBuilder != null)
                {
                    return _borrowedBuilder.Length;
                }
                return 0;
            }
            set
            {
                LazyPrepare();
                _borrowedBuilder.Length = value;
            }
        }

        internal ReuseableStringBuilder(int capacity = 16)
        {
            _capacity = capacity;
        }

        public override string ToString()
        {
            if (_borrowedBuilder == null)
            {
                return string.Empty;
            }
            return _borrowedBuilder.ToString();
        }

        void IDisposable.Dispose()
        {
            if (_borrowedBuilder != null)
            {
                ReuseableStringBuilderFactory.Release(_borrowedBuilder);
                _borrowedBuilder = null;
                _capacity = -1;
            }
        }

        internal ReuseableStringBuilder Append(char value)
        {
            LazyPrepare();
            _borrowedBuilder.Append(value);
            return this;
        }

        internal ReuseableStringBuilder Append(string value)
        {
            LazyPrepare();
            _borrowedBuilder.Append(value);
            return this;
        }

        internal ReuseableStringBuilder Append(string value, int startIndex, int count)
        {
            LazyPrepare();
            _borrowedBuilder.Append(value, startIndex, count);
            return this;
        }

        public ReuseableStringBuilder AppendSeparated(char separator, ICollection<string> strings)
        {
            LazyPrepare();
            int num = strings.Count - 1;
            foreach (string @string in strings)
            {
                _borrowedBuilder.Append(@string);
                if (num > 0)
                {
                    _borrowedBuilder.Append(separator);
                }
                num--;
            }
            return this;
        }

        public ReuseableStringBuilder Clear()
        {
            LazyPrepare();
            _borrowedBuilder.Clear();
            return this;
        }

        internal ReuseableStringBuilder Remove(int startIndex, int length)
        {
            LazyPrepare();
            _borrowedBuilder.Remove(startIndex, length);
            return this;
        }

        private void LazyPrepare()
        {
            if (_borrowedBuilder == null)
            {
                ErrorUtilities.VerifyThrow(_capacity != -1, "Reusing after dispose");
                _borrowedBuilder = ReuseableStringBuilderFactory.Get(_capacity);
            }
        }
    }
}
