using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace YY.Build.Cross.Tasks.Cross
{
    // 用于读取GCC -MD 生成的Map文件。
    internal class GCCMapReader
    {
        private StreamReader StreamReader;
        private string TextBuffer;
        private int CurrentTextBufferIndex = 0;
        private bool FileEnd = true;

        public bool Init(string MapFile)
        {
            TextBuffer = null;
            CurrentTextBufferIndex = 0;
            FileEnd = false;

            try
            {
                StreamReader = File.OpenText(MapFile);

                var ObjectName = ReadLine();
                // 文件内容不对。
                if (ObjectName == null || ObjectName.Length == 0 || ObjectName[ObjectName.Length - 1] != ':')
                {
                    return false;
                }

                ObjectName = ReadLine();
                // 开头指向自己，所以跳过即可。
                if(ObjectName == null || ObjectName.Length == 0)
                {
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private char? GetChar()
        {
            if (FileEnd)
                return null;
          Read:
            if (TextBuffer == null || TextBuffer.Length == CurrentTextBufferIndex)
            {
                CurrentTextBufferIndex = 0;
                TextBuffer = StreamReader.ReadLine();
                if (TextBuffer == null)
                {
                    FileEnd = true;
                    return null;
                }
            }

            var ch = TextBuffer[CurrentTextBufferIndex];
            ++CurrentTextBufferIndex;

            if (ch == ' ')
                return '\0';

            // 转义 ?
            if (ch == '\\')
            {
                if (TextBuffer.Length == CurrentTextBufferIndex)
                {
                    // 这是一个连接符，重新再读一行
                    goto Read;
                }

                ch = TextBuffer[CurrentTextBufferIndex];
                ++CurrentTextBufferIndex;
            }

            return ch;
        }

        public string? ReadLine()
        {
            if (FileEnd)
                return null;

            string Tmp = "";

            char? ch;

            // 删除开头多余的0终止
            for(; ;)
            {
                ch = GetChar();
                if (ch == null)
                    return null;

                if (ch != '\0')
                    break;
            }

            do
            {
                Tmp += ch;

                ch = GetChar();
            } while (ch != null && ch != '\0');

            return Tmp;
        }
    }
}
