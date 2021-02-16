using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Models;

namespace LuaSharpVM.Obfuscator.Plugin
{
    public class LOString : LOPlugin
    {
        // randomize strings?
        static string desc = "Randomize function names for all the global functions.";

        private Dictionary<string, string> StringMap = new Dictionary<string, string>();
        
        public LOString(ref LuaDecoder decoder) : base(ref decoder, desc)
        {
        }

        public override void Obfuscate(LOSettings settings)
        {
            // iterate all constants in root function
            for (int i = 0; i < base.Decoder.File.Function.Constants.Count; i++)
            {
                if (base.Decoder.File.Function.Constants[i].Type == LuaType.String)
                {
                    var LuaStr = (StringConstant)base.Decoder.File.Function.Constants[i];
                    if (!StringMap.ContainsKey(LuaStr.Value))
                    {
                        GenerateNewString(LuaStr.Value);
                        Console.WriteLine($"Replaced {LuaStr.Value} with {StringMap[LuaStr.Value]}");
                    }
                    LuaStr.Value = StringMap[LuaStr.Value];
                }
            }
        }

        private string RandomString()
        {
            char[] chars = new char[] { 'i', 'I', 'l' };
            Random rnd = new Random();

            string result = "";

            int len = rnd.Next(10, 13);

            while (result.Length != len)
            {
                char c = chars[rnd.Next(0, chars.Length)];
                if (result.Length == 0 && !char.IsLetter(c))
                    continue; // we need to start with a letter

                result += c;
            }
            return result;
        }

        private void GenerateNewString(string key)
        {
            do
            {
                string str = RandomString();
                if (!this.StringMap.ContainsKey(key))
                    if(!this.StringMap.ContainsValue(str))
                        this.StringMap.Add(key, str);
            }
            while (!this.StringMap.ContainsKey(key)); // repeat in case of failure
        }
    }
}
