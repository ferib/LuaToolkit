using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscater.Plugin
{
    public class LOFlow
    {
        // tamper with the control flow

        private LuaDecoder File;

        public LOFlow(ref LuaDecoder file)
        {
            this.File = file;
        }
    }
}
