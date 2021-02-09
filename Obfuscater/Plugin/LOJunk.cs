using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscater.Plugin
{
    public class LOJunk : LOPlugin
    {
        // add junk code inbetween things
        // add dead code that cant be defined on static analyses
        // multiply basic math instructions

        static string desc = "TODO";
        public LOJunk(ref LuaDecoder decoder) : base(ref decoder, desc)
        {

        }
    }
}
