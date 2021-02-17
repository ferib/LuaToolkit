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

        public List<LuaScriptFunction> LuaFunctions;
        private LuaScriptLine LuaCode;

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
            // Get function names from root
            var names = GetFunctionNames();
            for (int i = 0; i < this.Decoder.File.Function.Functions.Count; i++)
            {
                WriteFunction(this.Decoder.File.Function.Functions[i], 1, names[i]);
            }
            WriteFunction(this.Decoder.File.Function);

            // allign/format/whatever each function
            foreach (var f in this.LuaFunctions)
                f.Complete();
        }

        private void WriteFunction(LuaFunction func, int dpth = 0, string name = "")
        {
            // TODO: move header in LuaScriptFunction class
            string funcName = "";
            List<string> args = new List<string>();
            for (int i = 0; i < func.ArgsCount; i++)
                args.Add($"var{i}");

            if (dpth == 0)
                funcName = null; // destroy header on root

            if (funcName != null)
                funcName = name; // TODO: remp fix, cleanup soonTM

            LuaScriptFunction newFunction = new LuaScriptFunction(funcName, args, ref func, ref this.Decoder);
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
        }

        private List<string> GetFunctionNames()
        {
            List<string> names = new List<string>();

            // NOTE: Global functions use GETGLOBAL to get first part, then
            // CLOSURE to set the variables they just got, and then SETTABLE
            // to move a constant (second part of func name) into the global

            for(int i = 0; i < this.Decoder.File.Function.Instructions.Count; i++)
            {
                var instr = this.Decoder.File.Function.Instructions[i];
                switch(instr.OpCode)
                {
                    case LuaOpcode.CLOSURE:
                        string name = "unknown";
                        string globalName = "";
                        int e = this.Decoder.File.Function.Instructions.Count-1;

                        int j = i - 1;
                        // Find GETGLOBAL
                        while (j >= 0)
                        {
                            if (this.Decoder.File.Function.Instructions[j].OpCode == LuaOpcode.CLOSURE)
                                break; // start of another closure

                            if (this.Decoder.File.Function.Instructions[j].OpCode == LuaOpcode.GETGLOBAL)
                            {
                                globalName = this.Decoder.File.Function.Constants[j].ToString();
                                globalName = globalName.Substring(1, globalName.Length-2);
                                break; // job's done
                            }
                            j++;
                        }

                        j = i+1;
                        // Find SETTABLE
                        while (j < this.Decoder.File.Function.Instructions.Count)
                        {
                            if (this.Decoder.File.Function.Instructions[j].OpCode == LuaOpcode.CLOSURE)
                                break; // meh
                            if (this.Decoder.File.Function.Instructions[j].OpCode == LuaOpcode.SETTABLE)
                            {
                                name = this.Decoder.File.Function.Constants[this.Decoder.File.Function.Instructions[j].C].ToString();
                                name = name.Substring(1, name.Length-2);
                                break; // job's done
                            }else if(this.Decoder.File.Function.Instructions[j].OpCode == LuaOpcode.SETGLOBAL)
                            {
                                // is local!
                                name = this.Decoder.File.Function.Constants[this.Decoder.File.Function.Instructions[j].C].ToString();
                                name = name.Substring(1, name.Length - 2);
                                break; 
                            }
                            j++;
                        }

                        if (globalName != "")
                            name = globalName + ":" + name;
                        names.Add(name);
                        break;
                }
            }

            return names;
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
