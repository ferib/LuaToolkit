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
        // For starters, add a few jumps/ifs that dont make sense?
        public override void Obfuscate()
        {
            throw new NotImplementedException();
        }

        public override string GetName()
        {
            return Name;
        }
    }
}
