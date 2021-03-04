using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscator.Plugin
{
    public class LOFlow : LOPlugin
    {
        // tamper with the control flow

        static string desc = "Tempers the control flow to make it harder to understand.";
        private static string Name = "FlowControl";

        public LOFlow(ref LuaDecoder decoder) : base(ref decoder, desc)
        {

        }

        public override void Obfuscate()
        {
            // lets assume we only have 1 function
            var target = base.Decoder.File.Function.Functions[3].ScriptFunction;

            var oldc = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.DarkMagenta;

            // duplicate reverse ifs?

            Console.WriteLine(target.Text);

            Console.ForegroundColor = oldc;
        }

        public override string GetName()
        {
            return Name;
        }
    }
}
