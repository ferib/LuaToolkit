using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Util
{
    public class StringUtil
    {
        public const string NewLineChar = "\r\n";

        public static string StripNewlines(string str)
        {
            return str.Replace(NewLineChar, "");
        }
    }
}
