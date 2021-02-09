using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscater.Plugin
{
    public class LOJunk
    {
        // add junk code inbetween things
        // add dead code that cant be defined on static analyses
        // multiply basic math instructions

        private LuaDecoder File;

        public LOJunk(ref LuaDecoder file)
        {
            this.File = file;
        }
    }
}
