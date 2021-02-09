using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscater.Plugin
{
    public class LOVM
    {
        // Add custom instruction by replacing pairs of existing ones
        private LuaDecoder File;

        public LOVM(ref LuaDecoder file)
        {
            this.File = file;
        }
    }
}
