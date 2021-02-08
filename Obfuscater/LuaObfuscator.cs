using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Models;
using LuaSharpVM.Decompiler;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Obfuscater
{
    public class LuaObfuscator
    {
        private LuaDecoder OriginalDecoder;
        private LuaDecoder ObfuscatedDecoder;
        private LuaDecompiler OriginalDecompiler;
        private LuaDecompiler ObfuscatedDecompiler;
        private byte[] ObfuscatedLuaC;
        private byte[] OriginalLuaC;

        public LuaObfuscator(byte[] originalLuaC)
        {
            this.OriginalLuaC = originalLuaC;
            this.OriginalDecoder = new LuaDecoder(this.OriginalLuaC);
            this.OriginalDecompiler = new LuaDecompiler(this.OriginalLuaC);
            if(originalLuaC != null)
                Obfuscate();
        }

        private void Obfuscate()
        {

        }

        public string DecompileOriginalLuaC()
        {
            if(this.OriginalDecompiler.Result == null)
            {
                this.OriginalDecompiler.Write(this.OriginalDecoder.Function);
            }
            return this.OriginalDecompiler.Result;
        }

        public string DecompileObfuscatedLuaC()
        {
            if(this.ObfuscatedDecoder == null && this.ObfuscatedLuaC != null && this.ObfuscatedDecompiler == null)
            {
                this.ObfuscatedDecoder = new LuaDecoder(this.ObfuscatedLuaC);
                this.ObfuscatedDecompiler.Write(this.ObfuscatedDecoder.Function);
            }
            
            if(this.ObfuscatedDecompiler != null)
                return this.ObfuscatedDecompiler.Result;

            return "NO RESULT";
        }

    }
}
