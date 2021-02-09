using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscater.Plugin
{
    public class LOFlow : LOPlugin
    {
        // tamper with the control flow

        static string desc = "TODO";
        public LOFlow(ref LuaDecoder decoder) : base(ref decoder, desc)
        {

        }
        // For starters, add a few jumps/ifs that dont make sense?
    }
}
