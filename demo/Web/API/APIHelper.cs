using System;
using System.Collections.Generic;
using System.Text;
using LuaToolkit.Beautifier;
using LuaToolkit.Core;
using LuaToolkit.Decompiler;
using LuaToolkit.Disassembler;
using LuaToolkit.Models;

namespace Web.API
{
    public static class APIHelper
    {
        public static APIResponse<ResponseDecompiler> Decompile(byte[] luacFile)
        {
            APIResponse<ResponseDecompiler> result = new APIResponse<ResponseDecompiler>();
            result.status = "Error";
            if(luacFile == null)
            {
                result.message = "Error, input file empty.";
                return result;
            }

            //luacFile = System.IO.File.ReadAllBytes(@"L:\Projects\LuaBytcodeInterpreter\lua_installer\files\upvalues.luac");
            try
            {
                // decompile
                LuaCFile cfile = new LuaCFile(luacFile);
                LuaDecoder decoder = new LuaDecoder(cfile);

                if (decoder.File.IntSize == 0)
                {
                    result.message = "Error, decoding lua binary failed.";
                    return result;
                }

                LuaWriter decompiler = new LuaWriter(decoder);

                // handle error
                if (luacFile.Length == 0)
                {
                    result.message = "Error, input file empty.";
                    return result;
                }
                else if (decompiler.LuaScript.Length == 0)
                {
                    result.message = "Error, decompilation failed.";
                    return result;
                }

                result.status = "Ok";
                result.data.decompiled = decompiler.LuaScript;
                return result;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
                result.status = "Error";
                result.message = "Unknown error!";
                return result;
            }
            return null;
        }
        
        public static APIResponse<ResponseBeautifier> Beautifie()
        {
            APIResponse<ResponseBeautifier> result = new APIResponse<ResponseBeautifier>();
            result.status = "N/A";
            result.message = "Error, not yet implemented!";
            return result;
        }
        
        public static APIResponse<ResponseHighlighter> Highlight()
        {
            APIResponse<ResponseHighlighter> result = new APIResponse<ResponseHighlighter>();
            result.status = "N/A";
            result.message = "Error, not yet implemented!";
            return result;
        }
    }
}
