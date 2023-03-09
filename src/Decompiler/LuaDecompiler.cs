using LuaToolkit.Disassembler;
using System.Collections.Generic;

namespace LuaToolkit.Decompiler
{
    public class LuaDecompiler
    {
        public List<LuaScriptFunction> LuaFunctions;

        private LuaDecoder Decoder;
        private Dictionary<int, int> UsedConstants; // to definde locals
        private LuaFunction RootFunction
        {
            get { return this.Decoder.File.Function; }
        }

        public LuaDecompiler(LuaDecoder decoder)
        {
            this.Decoder = decoder;
        }


        public string Decompile(bool debugInfo = false)
        {
            //// keep lazy init?
            //if (this.LuaFunctions == null)
            //{
            // Start decompilation at RootFunction
            this.LuaFunctions = new List<LuaScriptFunction>();
            this.RootFunction.Name = "CRoot"; // or maybe main?
            InitializeScriptFunction(this.RootFunction);
            //}

            // Decompile main ScriptFunction will also decompile its childs
            return this.RootFunction.ScriptFunction.Decompile(debugInfo);
        }
        //
        //
        // Initialises a function:
        //   - Gives function a name.
        //   - interates over childeren:
        //      - creates a script function
        //      - initialize.
        private void InitializeScriptFunction(LuaFunction func)
        {
            CreateScripFunction(func); // root first and then inside ?
            // TODO: write functions on CLOSURE and not each list?
            for (int i = 0; i < func.Functions.Count; i++)
            {
                var child = func.Functions[i];
                child.Name = func.Name + "_" + i; // set default name
                CreateScripFunction(child, func.ScriptFunction.Depth + 1);
                foreach (var f in child.Functions)
                {
                    InitializeScriptFunction(f); // children NOTE: write children in body of parent?
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
                    Depth = dpth + 1
                });
            }
            newFunction.Finalize();
        }

        // Deprecated?
        private List<KeyValuePair<string, bool>> GetFunctionNames()
        {
            // NOTE: moving to LuaScriptFunction.HandleUpvalues()!!!
            // TODO: move this over to NOT ONLY the root function!!
            List<KeyValuePair<string, bool>> names = new List<KeyValuePair<string, bool>>();

            // NOTE: Global functions use GETGLOBAL to get first parts, then
            // CLOSURE to set the variables they just got, and then SETTABLE
            // to move a constant (second part of func name) into the global
            // also, while we are at it, lets check if it sets global or not

            for (int i = 0; i < this.Decoder.File.Function.Instructions.Count; i++)
            {
                var instr = this.Decoder.File.Function.Instructions[i];
                switch (instr.OpCode)
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
                                globalName = globalName.Substring(1, globalName.Length - 2);
                                break; // job's done
                            }
                            j++;
                        }

                        j = i + 1;
                        // Find SETTABLE
                        while (j < this.Decoder.File.Function.Instructions.Count)
                        {
                            if (this.Decoder.File.Function.Instructions[j].OpCode == LuaOpcode.CLOSURE)
                                break; // meh

                            if (this.Decoder.File.Function.Instructions[j].OpCode == LuaOpcode.MOVE)
                            {
                                // upvalues!
                                if (this.Decoder.File.Function.Instructions[j].A == 0) // 0 = _ENV
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
                                name = name.Substring(1, name.Length - 2);
                                break; // job's done
                            }
                            else if (this.Decoder.File.Function.Instructions[j].OpCode == LuaOpcode.SETGLOBAL)
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
    }
}
