//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace LuaToolkit.Beautifier
//{
//    public static class LuaHighlight
//    {
//        // TODO: create a Console based highlighting class
//        private static string Keywords = "class extends implements import interface new case do while else if for in switch throw get set function var try catch finally while with default break continue delete return each const namespace package include use is as instanceof typeof author copy default deprecated eventType example exampleText exception haxe inheritDoc internal link mtasc mxmlc param private return see serial serialData serialField since throws usage version langversion playerversion productversion dynamic private public partial static intrinsic internal native override protected AS3 final super this arguments null Infinity NaN undefined true false abstract as base bool break by byte case catch char checked class const continue decimal default delegate do double descending explicit event extern else enum false finally fixed float for foreach from goto group if implicit in int interface internal into is lock long new null namespace object operator out override orderby params private protected public readonly ref return switch struct sbyte sealed short sizeof stackalloc static string select this throw true try typeof uint ulong unchecked unsafe ushort using var virtual volatile void while where yield";

//        private static Dictionary<string, ConsoleColor> ColorMap = new Dictionary<string, ConsoleColor>()
//        {
//            {Keywords, ConsoleColor.Red }
//        };

//        private static ConsoleColor KeywordColor = ConsoleColor.Yellow;
//        private static ConsoleColor TextColor = ConsoleColor.Cyan;
//        private static ConsoleColor CommentColor = ConsoleColor.DarkGreen;

//        public static void PrintColor(string input)
//        {
//            string[] lines = input.Split('\n');

//            // save colors
//            ConsoleColor oldForeground = Console.ForegroundColor;
//            ConsoleColor oldBackground = Console.BackgroundColor;

//            ConsoleColor lastColor = Console.ForegroundColor;

//            bool isCommentLine;
//            string output = "";
//            for(int i = 0; i < lines.Length; i++)
//            {
//                // TODO:
//                // - highlight keywords
//                // - highlight words between quotes
//                // - highlight numbers
//                // - highlight comments (--) and handle multi lines ([[)

//                if(lines[i].Contains("--"))
//                {
//                    // cut this part out and check for [[ to find multiline
//                }

//                void printLastColor(string s)
//                {
//                    Console.ForegroundColor = lastColor;
//                    Console.WriteLine(s);
//                }
//                output += "\n";
//            }

//            // restore colors
//            Console.ForegroundColor = oldForeground;
//            Console.BackgroundColor = oldBackground;
//        }
//    }
//}
