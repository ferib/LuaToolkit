using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscator.Plugin
{
    public class LOVM : LOPlugin
    {
        // Add custom instruction by replacing pairs of existing ones
        static string desc = "TODO";
        public LOVM(ref LuaDecoder decoder) : base(ref decoder, desc)
        {

        }
    }
}
