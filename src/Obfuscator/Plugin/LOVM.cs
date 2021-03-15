using System;
using System.Collections.Generic;
using System.Text;
using LuaToolkit.Core;
using LuaToolkit.Disassembler;

namespace LuaToolkit.Obfuscator.Plugin
{
    public class LOVM : LOPlugin
    {
        // Add custom instruction by replacing pairs of existing ones
        static string desc = "TODO";
        private static string Name = "VM";

        public LOVM(ref LuaDecoder decoder) : base(ref decoder, desc)
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
