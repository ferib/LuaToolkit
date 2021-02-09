using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;

namespace LuaSharpVM.Obfuscater.Plugin
{
    public class LOFlow
    {
        // tamper with the control flow

        private LuaCFile Lua;

        public LOFlow(ref LuaCFile lua)
        {
            this.Lua = lua;
        }
    }
}
