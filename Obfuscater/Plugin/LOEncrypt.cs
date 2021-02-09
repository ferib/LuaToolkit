using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Models;
using LuaSharpVM.Core;

namespace LuaSharpVM.Obfuscater
{
    public class LOEncrypt
    {
        // Encrypt a given stub using a XOR to break basic tools

        private LuaCFile Lua;

        public LOEncrypt(ref LuaCFile lua)
        {
            this.Lua = lua;
        }
    }
}
