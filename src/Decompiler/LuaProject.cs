using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Core;
using LuaSharpVM.Models;

namespace LuaSharpVM.Decompiler
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
