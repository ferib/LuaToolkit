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
            throw new NotImplementedException();
        }
        
        public static APIResponse<ResponseHighlighter> Highlight()
        {
            throw new NotImplementedException();
        }
    }
}
