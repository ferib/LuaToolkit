using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LuaToolkit.Beautifier
{
    public static class LuaBeautifier
    {
        private static string[] EndLineKeyword = { "end", "then", "else", ")"};
        private static string[] StartLineKeyword = { "if", "function", "local function", "for", "elseif", "return"};

        public static string Delimiter = "\r\n";
        public static int Spaces = 4;

        // TODO: create a beautifier for Lua to format our code properly (fork from existing)
        public static string BeautifieScript(string text, bool minified = false)
        {
            // text based because we did wanky things instead of respecting the list	
            int tabCount = 1;

            // NOTE: remove comment lines?
            for(int i = 0; i < text.Length-3; i++)
            {
                if (!(text[i] == '-' && text[i + 1] == '-' && text[i + 2] != '[' && text[i + 3] != '['))
                    continue;

                // find endline
                int index = i + 2;
                do
                {
                    index++;
                }
                while (text[index] != '\n' && index < text.Length);
                // idk?
            }

            string minifiedText = text.Replace("\r", "").Replace("\t", "").Replace("\n", " ");

            while(minifiedText.Contains("  "))
                minifiedText = minifiedText.Replace("  ", " ");

            if (minified)
                return minifiedText; 

            string[] words = minifiedText.Split(' ');
            string result = "";
            int tabDepth = 0;

            for (int i = 0; i < words.Length; i++)
            {
                string prefix = "";
                string postfix = "";
                if (EndLineKeyword.Contains(words[i]))
                {
                    postfix = "\r\n";
                    if (tabDepth < 0)
                        tabDepth = 0;
                    prefix = new string('\t', tabDepth);
                    tabDepth--;
                }else if (i > 0 && words[i - 1] == "=")
                {
                    if (tabDepth < 0)
                        tabDepth = 0;
                    postfix = "\r\n" + new string('\t', tabDepth);
                }
                else if(StartLineKeyword.Contains(words[i]))
                {
                    tabDepth++;
                    prefix = new string('\t', tabDepth);
                }

                result += " " + prefix + words[i] + postfix;
            }
            return result;
        }

    }
}
