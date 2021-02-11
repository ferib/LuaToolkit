using System;
using System.Collections.Generic;
using System.Text;
using LuaSharpVM.Models;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;

namespace LuaSharpVM.Decompiler
{
    public class LuaWriter
    {
        private LuaDecoder Decoder;

        private Dictionary<int, int> UsedConstants; // to definde locals

        private List<LuaScriptFunction> LuaFunctions;
        private LuaScriptLine LuaCode;


        private int FunctionCounter;

        public string LuaScript
        {
            get { return GetScript(); }
        }

        public LuaWriter(ref LuaDecoder decoder)
        {
            this.Decoder = decoder;
            this.LuaFunctions = new List<LuaScriptFunction>();
            WriteFile();
        }

        private void WriteFile()
        {
            FunctionCounter = 0;
            for (int i = 0; i < this.Decoder.File.Function.Functions.Count; i++)
            {
                FunctionCounter++;
                WriteFunction(this.Decoder.File.Function.Functions[i], 2);
            }
            WriteFunction(this.Decoder.File.Function);
        }

        private void WriteFunction(LuaFunction func, int dpth = 0)
        {
            // TODO: move header in LuaScriptFunction class
            string header = $"func" + FunctionCounter + "(";
            for (int i = 0; i < func.ArgsCount; ++i)
                header += "var" + i + (i + 1 != func.ArgsCount ? ", " : ")");
            FunctionCounter++;

            LuaScriptFunction newFunction = new LuaScriptFunction(header, ref func, ref this.Decoder);
            this.LuaFunctions.Add(newFunction);
            // TODO: move the above into a LuaScriptHeader or smthing

            for (int i = 0; i < func.Instructions.Count; i++)
            {
                newFunction.Lines.Add(new LuaScriptLine(func.Instructions[i], ref this.Decoder, ref func)
                {
                    Number = i,
                    Depth = dpth+1
                });
            }
            newFunction.Complete();
        }

        private string GetScript()
        {
            string result = "";
            for(int i = 0; i < this.LuaFunctions.Count; i++)
                result += this.LuaFunctions[i].Text;

            if(this.LuaCode != null)
                result += this.LuaCode.Text;

            return result;
        }
    }
}
