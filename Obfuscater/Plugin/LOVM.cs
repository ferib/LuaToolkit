using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;

namespace LuaSharpVM.Obfuscater.Plugin
{
    public class LOVM
    {
        // Add custom instruction by replacing pairs of existing ones
        private LuaCFile Lua;

        public LOVM(ref LuaCFile lua)
        {
            this.Lua = lua;
        }
    }
}
