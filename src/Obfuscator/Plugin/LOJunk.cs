using System;
using System.Collections.Generic;
using System.Text;
using LuaToolkit.Core;
using LuaToolkit.Disassembler;

namespace LuaToolkit.Obfuscator.Plugin
{
    public class LOJunk : LOPlugin
    {
        // add junk code inbetween things
        // add dead code that cant be defined on static analyses
        // multiply basic math instructions

        static string desc = "Add junk opcodes here and there to confuse decompilers.";
        private static string Name = "JunkCode";

        public LOJunk(ref LuaDecoder decoder) : base(ref decoder, desc)
        {

        }

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
