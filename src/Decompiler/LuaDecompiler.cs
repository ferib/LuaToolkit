﻿using System;
using System.Collections.Generic;
using System.Text;
using LuaToolkit.Models;
using LuaToolkit.Core;
using LuaToolkit.Disassembler;

namespace LuaToolkit.Decompiler
{
    public class LuaDecompiler
    {
        private LuaDecoder Decoder;

        private Dictionary<int, int> UsedConstants; // to definde locals

        public List<LuaScriptFunction> LuaFunctions;
        private LuaScriptLine LuaCode;

        public string LuaScript
        {
            get { return GetScript(); }
        }

        public LuaDecompiler(LuaDecoder decoder)
        {
            this.Decoder = decoder;
            this.LuaFunctions = new List<LuaScriptFunction>();
            WriteFile();
        }

        private void WriteFile()
        {
            // create Script Functions
            this.Decoder.File.Function.Name = "CRoot"; // or main?
            WriteF(this.Decoder.File.Function);

            // NOTE: this is done on GetText
            //// allign/format/whatever each function
            //foreach (var f in this.LuaFunctions)
            //    f.Complete();

        }

        private void WriteF(LuaFunction func)
        {
            CreateScripFunction(func); // root first and then inside ?
            // TODO: write functions on CLOSURE and not each list?
            for (int i = 0; i < func.Functions.Count; i++)
            {
                func.Functions[i].Name = func.Name + "_" + i; // set default name
                CreateScripFunction(func.Functions[i], func.ScriptFunction.Depth+1);
                foreach (var f in func.Functions[i].Functions)
                {
                    WriteF(f); // children NOTE: write children in body of parent?
                }
            }
        }

        private void CreateScripFunction(LuaFunction func, int dpth = 0, bool isGlobal = false)
        {
            LuaScriptFunction newFunction = new LuaScriptFunction(func, this.Decoder);
            this.LuaFunctions.Add(newFunction);
            // TODO: move the above into a LuaScriptHeader or smthing

            for (int i = 0; i < func.Instructions.Count; i++)
            {
                newFunction.GetLines().Add(new LuaScriptLine(func.Instructions[i], this.Decoder, func)
                {
                    Number = i,
                    Depth = dpth+1
                });
            }
            newFunction.Complete();
        }

        private List<KeyValuePair<string, bool>> GetFunctionNames()
        {
            // NOTE: moving to LuaScriptFunction.HandleUpvalues()!!!
            // TODO: move this over to NOT ONLY the root function!!
            List<KeyValuePair<string, bool>> names = new List<KeyValuePair<string, bool>>();

            // NOTE: Global functions use GETGLOBAL to get first parts, then
            // CLOSURE to set the variables they just got, and then SETTABLE
            // to move a constant (second part of func name) into the global
            // also, while we are at it, lets check if it sets global or not

            for(int i = 0; i < this.Decoder.File.Function.Instructions.Count; i++)
            {
                var instr = this.Decoder.File.Function.Instructions[i];
                switch(instr.OpCode)
                {
                    case LuaOpcode.CLOSURE:
                        string name = "";
                        string globalName = "";
                        bool isGlobal = false;

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

                            if (this.Decoder.File.Function.Instructions[j].OpCode == LuaOpcode.MOVE)
                            {
                                // upvalues!
                                if(this.Decoder.File.Function.Instructions[j].A == 0) // 0 = _ENV
                                {
                                    LuaConstant cons;
                                    //if (this.Decoder.File.Function.Constants.Count > this.Decoder.File.Function.Instructions[j].B)
                                    //    cons = this.Decoder.File.Function.Constants[this.Decoder.File.Function.Instructions[j].B];
                                    //else
                                        cons = new StringConstant("unknown" + this.Decoder.File.Function.Instructions[j].B);
                                    this.Decoder.File.Function.Upvalues.Add(cons);
                                }
                            }
                            else if (this.Decoder.File.Function.Instructions[j].OpCode == LuaOpcode.SETTABLE)
                            {
                                isGlobal = true;
                                name = this.Decoder.File.Function.Constants[this.Decoder.File.Function.Instructions[j].C].ToString();
                                name = name.Substring(1, name.Length-2);
                                break; // job's done
                            }else if(this.Decoder.File.Function.Instructions[j].OpCode == LuaOpcode.SETGLOBAL)
                            {
                                // is global!
                                isGlobal = true;
                                name = this.Decoder.File.Function.Constants[this.Decoder.File.Function.Instructions[j].C].ToString();
                                name = name.Substring(1, name.Length - 2);
                                break; 
                            }
                            j++;
                        }

                        if (globalName != "")
                            name = globalName + ":" + name;
                        names.Add(new KeyValuePair<string, bool>(name, isGlobal));
                        break;
                }
            }

            return names;
        }

        private string GetScript()
        {
            string result = "";
            result += this.Decoder.File.Function.ScriptFunction.GetText(); // only need main, right?
            //for(int i = 0; i < this.LuaFunctions.Count; i++)
            //    result += this.LuaFunctions[i].Text;

            if(this.LuaCode != null)
                result += this.LuaCode.Text;

            return result;
        }
    }
}
