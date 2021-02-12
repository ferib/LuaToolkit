using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Models;
using LuaSharpVM.Decompiler;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Obfuscator.Plugin;

namespace LuaSharpVM.Obfuscator
{
    public class LuaObfuscator
    {
        private LuaDecoder OriginalDecoder;
        public LuaDecoder ObfuscatedDecoder;
        private LuaDecompiler OriginalDecompiler;
        private LuaDecompiler ObfuscatedDecompiler;
        private byte[] ObfuscatedLuaC;
        private byte[] OriginalLuaC;

        public LuaObfuscator(byte[] originalLuaC)
        {
            this.OriginalLuaC = originalLuaC;
            this.OriginalDecoder = new LuaDecoder(new LuaCFile(this.OriginalLuaC));
            this.OriginalDecompiler = new LuaDecompiler(this.OriginalLuaC);
            if(originalLuaC != null)
                Obfuscate();
        }

        private void Obfuscate()
        {
            //LOEncrypt encrypt = new LOEncrypt(ref OriginalDecoder.File);

            //LOString obfString = new LOString(ref this.OriginalDecoder);
            LODebug obfDebug = new LODebug(ref this.OriginalDecoder, LODebugLevel.RandomLow);

            // add watermark
            this.OriginalDecoder.File.Function.Constants.Add(new StringConstant("cromulon.io"));
        }

        public string DecompileOriginalLuaC()
        {
            if(this.OriginalDecompiler.Result == null)
            {
                this.OriginalDecompiler.Write(this.OriginalDecoder.File.Function);
            }
            return this.OriginalDecompiler.Result;
        }

        public string DecompileObfuscatedLuaC()
        {
            if(this.ObfuscatedDecoder == null && this.ObfuscatedLuaC != null && this.ObfuscatedDecompiler == null)
            {
                this.ObfuscatedDecoder = new LuaDecoder(new LuaCFile(this.ObfuscatedLuaC));
                this.ObfuscatedDecompiler.Write(this.ObfuscatedDecoder.File.Function);
            }
            
            if(this.ObfuscatedDecompiler != null)
                return this.ObfuscatedDecompiler.Result;

            return "NO RESULT";
        }

    }
}
