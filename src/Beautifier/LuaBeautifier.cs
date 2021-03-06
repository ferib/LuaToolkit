using System;
using System.Collections.Generic;
using System.Text;

namespace LuaSharpVM.Beautifier
{
    public static class LuaBeautifier
    {
        // TODO: create a beautifier for Lua to format our code properly (fork from existing)
        public static string BeautifieCode(string text)
        {
            // text based because we did wanky things instead of respecting the list	
            int tabCount = 1;
            string[] lines = text.Replace("\r", "").Replace("\t", "").Split('\n');
            string newText = "";
            for (int i = 0; i < lines.Length; i++)
            {
                bool postAdd = false;
                bool postSub = false;
                if (lines[i].StartsWith("if") || lines[i].StartsWith("function") || lines[i].StartsWith("local function") || lines[i].StartsWith("for"))
                    postAdd = true;
                else if (lines[i].StartsWith("else"))
                {
                    if (i < lines.Length - 1 && lines[i + 1].StartsWith("if"))
                    {
                        // elseif	
                        newText += $"{new string('\t', tabCount)}{lines[i]}{lines[i + 1]}\r\n";
                        i += 1; // brrrr fuck y'all, i skip next one this way!	
                        continue;
                    }
                    else
                    {
                        // else	
                        tabCount -= 1;
                        postAdd = true;
                    }
                }
                else if (lines[i].StartsWith("end"))
                    tabCount -= 1;

                if (tabCount < 0)
                    tabCount = 0;

                if (lines[i].StartsWith("if"))
                    newText += $"{new string('\t', tabCount)}{lines[i]}";
                else if (lines[i].EndsWith("or") || lines[i].EndsWith("and") || lines[i].StartsWith(" not"))
                    newText += $"{lines[i]}";
                else if (lines[i] == "")
                    newText += "";
                else
                    newText += $"{new string('\t', tabCount)}{lines[i]}\r\n";

                if (lines[i].EndsWith("then"))
                    newText += "\r\n";

                if (postAdd)
                    tabCount += 1;
                if (postSub)
                    tabCount -= 1;
            }
            return newText;
        }

    }
}
