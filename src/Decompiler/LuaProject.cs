using System;
using System.Collections.Generic;
using System.Text;
using LuaToolkit.Disassembler;
using LuaToolkit.Core;
using LuaToolkit.Models;

namespace LuaToolkit.Decompiler
{
    // NOTE: a LuaProject is a collection of LuaScriptFiles that are used to define eachother
    public class LuaProject
    {
        List<LuaCFile> LuaFiles; // defines the librarys and everything
        // wait, doesnt LuaC compile all files into one?

        public LuaProject()
        {
            this.LuaFiles = new List<LuaCFile>();
        }
    }
}
