using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;

namespace LuaSharpVM.Obfuscater.Plugin
{
    public class LOJunk
    {
        // add junk code inbetween things
        // add dead code that cant be defined on static analyses
        // multiply basic math instructions

        private LuaCFile Lua;

        public LOJunk(ref LuaCFile lua)
        {
            this.Lua = lua;
        }
    }
}
