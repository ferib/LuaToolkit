using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LuaToolkit.Beautifier
{
    // TODO: merge into Decompiler?
    public static class LuaBeautifier
    {
        // NOTE: this is a little more complex when you do NOT have the LuaScriptBlock/LuaScriptLine info :/
        private static string[] EndLineKeyword = { "end", "then", ")"};
        private static string[] SingleLineKeyword = { "else" };
        private static string[] StartLineKeyword = { "if", "function", "local function", "for", "elseif", "return", "local"};

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

            int j = 0;
            while(j < words.Length)
            {
                string prefix = "";
                string postfix = "";
                string line = "";

                //if(StartLineKeyword.Contains(words[j]))
                //{
                //    tabDepth++;
                //    line = "\r\n" +  new string(' ', tabDepth * 4) + words[j];
                //}
                //else 
                if(EndLineKeyword.Contains(words[j]))
                {
                    line = " " + words[j] + "\r\n";
                }
                else if(SingleLineKeyword.Contains(words[j]))
                {
                    //tabDepth--;
                    line = "\r\n" + new string(' ', tabDepth * 4) + words[j] + "\r\n";
                    //tabDepth++;
                }
                else if(words[j] == "=")
                {
                    // MOVE
                    if(words.Length > j+2 && words[j+2] != "..")
                    {
                        line = words[j] + " " + words[j + 1] + "\r\n";
                        j += 1;
                    }else if(words.Length > j + 2)
                    {
                        // concat
                        line = words[j] + " " + words[j + 1];
                        int vcount = 1;
                        while (words[vcount + j + 1] == ".." && vcount + j < words.Length) // find concats
                        {
                            line += " " + words[j + vcount + 1] + " " + words[j + vcount + 2];
                            vcount += 2;
                        }
                        line += "\r\n";
                        j += vcount;
                    }
                }
                else
                {
                    line = words[j] + " ";
                }

                result += line;
                Console.Write(line);
                j++;
            }
            return result;
        }

        private static string ClearnScript(string text)
        {
            text = text.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
            while (text.Contains("  "))
                text.Replace("  ", " ");
            return text;
        }
    }
}
