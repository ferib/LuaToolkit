﻿using System;
using System.Collections.Generic;
using System.Text;
using LuaToolkit;
using LuaToolkit.Core;
using LuaToolkit.Models;
using LuaToolkit.Decompiler;
using LuaToolkit.Disassembler;

namespace Recompiler
{
    public class Decompiler
    {
        private LuaDecompiler LuaDecompiler;
        private LuaDecoder Decoder;

        public Decompiler(byte[] Buffer)
        {
            this.Decoder = new LuaDecoder(new LuaCFile(Buffer));
            this.LuaDecompiler = new LuaDecompiler(this.Decoder);
        }

        public string GetResult()
        {
            return this.LuaDecompiler.LuaScript;
        }
    }
}
