using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Models;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscater
{
    public class LOEncrypt
    {
        // Encrypt a given stub using a XOR to break basic tools

        private LuaDecoder File;

        public LOEncrypt(ref LuaDecoder file)
        {
            this.File = file;
        }
    }
}
