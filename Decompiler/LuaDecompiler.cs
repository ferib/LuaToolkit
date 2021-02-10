using LuaSharpVM.Core;
using LuaSharpVM.Models;
using System.Text;
using System.Collections.Generic;

namespace LuaSharpVM.Decompiler
{
    public class LuaDecompiler
    {
        private uint FunctionsCount;
        public string Result;
        private byte[] Buffer;

        private Dictionary<int, List<int>> VariableUsageCache = new Dictionary<int, List<int>>();
        private int CurrentIndentLevel;

        public LuaDecompiler(byte[] Buffer)
        {
            this.Buffer = Buffer;
        }

        public void Write(LuaFunction function, int indentLevel = 0)
        {
            // reset
            if(indentLevel == 0)
                VariableUsageCache = new Dictionary<int, List<int>>();
            if(!VariableUsageCache.ContainsKey(indentLevel))
                VariableUsageCache.Add(indentLevel, new List<int>());
            CurrentIndentLevel = indentLevel;

            // top level function
            if (function.FirstLineNr == 0 && function.LastLineNr == 0)
            {
                WriteChildFunctions(function);
                WriteInstructions(function);
            }
            else
            {
                string indents = new string('\t', indentLevel);

                // TODO: add name based on Main Function Constants?
                string functionHeader = indents + $"function func" + FunctionsCount + "(";

                for (int i = 0; i < function.ArgsCount; ++i)
                {
                    functionHeader += "arg" + i + (i + 1 != function.ArgsCount ? ", " : ")");
                }


                this.Result += functionHeader;
                if (function.ArgsCount == 0)
                    this.Result += ")";
                this.Result += "\r\n";
                //writer.Write(functionHeader);
                ++FunctionsCount;

                // iterate variable cache
                CurrentIndentLevel += 1;
                if(!VariableUsageCache.ContainsKey(CurrentIndentLevel))
                    VariableUsageCache.Add(CurrentIndentLevel, new List<int>());

                //WriteConstants(function, indentLevel + 1);

                WriteChildFunctions(function, indentLevel + 1);

                WriteInstructions(function, indentLevel + 1);
            }
        }

        private void WriteConstants(LuaFunction function, int indentLevel = 0)
        {
            uint constCount = 0;

            string tabs = new string('\t', indentLevel);

            foreach (var c in function.Constants)
            {
                this.Result += $"{tabs}const{constCount} = {c.ToString()}\r\n";
                ++constCount;
            }
        }

        private void WriteChildFunctions(LuaFunction function, int indentLevel = 0)
        {
            foreach (var f in function.Functions)
            {
                if(!VariableUsageCache.ContainsKey(indentLevel + 1))
                    VariableUsageCache.Add(indentLevel + 1, new List<int>());
                Write(f, indentLevel + 1);
            }
        }

        private void WriteInstructions(LuaFunction function, int indentLevel = 0)
        {
            // TODO: complete all instructions
            string tabs = new string('\t', indentLevel);

            int subIdentCount = 0;

            for(int i = 0; i < function.Instructions.Count; i++)
            {
                switch (function.Instructions[i].OpCode)
                {
                    case LuaOpcode.MOVE:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = {WriteIndex(function.Instructions[i].B, function, false)}\r\n";
                        break;

                    case LuaOpcode.LOADK:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = {GetConstant(function.Instructions[i].Bx, function)}\r\n";
                        break;

                    case LuaOpcode.LOADBOOL:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = {(function.Instructions[i].B != 0 ? "true" : "false")}\r\n"; // TODO: check double instructions
                        break;

                    case LuaOpcode.LOADNIL:
                        for (int x = function.Instructions[i].A; x < function.Instructions[i].B + 1; ++x)
                            this.Result += $"{tabs}{WriteIndex(x, function, false)} = nil\r\n";
                        break;

                    case LuaOpcode.GETUPVAL:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = upvalue[{function.Instructions[i].B}]\r\n";
                        break;

                    case LuaOpcode.GETGLOBAL:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = _G[{GetConstant(function.Instructions[i].Bx, function)}]\r\n";
                        break;

                    case LuaOpcode.GETTABLE:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = var{function.Instructions[i].B}[{WriteIndex(function.Instructions[i].C, function)}]\r\n";
                        break;

                    case LuaOpcode.SETGLOBAL:
                        this.Result += $"{tabs}_G[{GetConstant(function.Instructions[i].Bx, function)}] = var{function.Instructions[i].A}\r\n";
                        break;

                    case LuaOpcode.SETUPVAL:
                        this.Result += $"{tabs}upvalue[{function.Instructions[i].B}] = var{function.Instructions[i].A}\r\n";
                        break;

                    case LuaOpcode.SETTABLE:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)}[{WriteIndex(function.Instructions[i].B, function)}] = {WriteIndex(function.Instructions[i].C, function)}\r\n";
                        break;

                    case LuaOpcode.NEWTABLE:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = {{}}\r\n"; // NOTE: do we even need to display this?
                        break;

                    case LuaOpcode.SELF:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = var{function.Instructions[i].B}\r\n";
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = var{function.Instructions[i].B}[{WriteIndex(function.Instructions[i].C, function)}]\r\n";
                        break;

                    case LuaOpcode.ADD:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = var{function.Instructions[i].B} + var{function.Instructions[i].C}\r\n";
                        break;

                    case LuaOpcode.SUB:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = var{function.Instructions[i].B} - var{function.Instructions[i].C}\r\n";
                        break;

                    case LuaOpcode.MUL:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = var{function.Instructions[i].B} * var{function.Instructions[i].C}\r\n";
                        break;

                    case LuaOpcode.DIV:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = var{function.Instructions[i].B} / var{function.Instructions[i].C}\r\n";
                        break;

                    case LuaOpcode.MOD:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = var{function.Instructions[i].B} % var{function.Instructions[i].C}\r\n";
                        break;

                    case LuaOpcode.POW:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = var{function.Instructions[i].B} ^ var{function.Instructions[i].C}\r\n";
                        break;

                    case LuaOpcode.UNM:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = -var{function.Instructions[i].B}\r\n";
                        break;

                    case LuaOpcode.NOT:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = not var{function.Instructions[i].B}\r\n";
                        break;

                    case LuaOpcode.LEN:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = #var{function.Instructions[i].B}\r\n";
                        break;

                    case LuaOpcode.CONCAT:
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function, false)} = ";

                        for (int x = function.Instructions[i].B; x < function.Instructions[i].C; ++x)
                            this.Result += $"{WriteIndex(x, function, false)} .. \r\n";

                        this.Result += $"var{function.Instructions[i].C}\r\n";
                        break;

                    case LuaOpcode.JMP:

                        this.Result += $"{tabs}JMP ({(short)function.Instructions[i].sBx})\r\n"; // TODO:
                        // JMP A sBx   pc+=sBx; if (A) close all upvalues >= R(A - 1)
                        if ((short)function.Instructions[i].sBx == 0)
                        {
                            subIdentCount--; // TODO: verify subs
                            tabs = tabs.Substring(1);
                        }
                        break;

                    case LuaOpcode.EQ:
                        this.Result += $"{tabs}if ({WriteIndex(function.Instructions[i].B, function,false)} == {WriteIndex(function.Instructions[i].C, function)}) ~= {function.Instructions[i].A} then\r\n";
                        subIdentCount++;
                        tabs += "\t";
                        break;

                    case LuaOpcode.LT:
                        this.Result += $"{tabs}if ({WriteIndex(function.Instructions[i].B, function, false)} < {WriteIndex(function.Instructions[i].C, function)}) ~= {function.Instructions[i].A} then\r\n";
                        subIdentCount++;
                        tabs += "\t";
                        break;

                    case LuaOpcode.LE:
                        this.Result += $"{tabs}if ({WriteIndex(function.Instructions[i].B, function)} <= {WriteIndex(function.Instructions[i].C, function)}) ~= {function.Instructions[i].A} then\r\n";
                        subIdentCount++;
                        tabs += "\t";
                        break;

                    case LuaOpcode.TEST:
                        this.Result += $"{tabs}if not var{function.Instructions[i].A} <=> {function.Instructions[i].C} then\r\n";
                        subIdentCount++;
                        tabs += "\t";
                        break;

                    case LuaOpcode.TESTSET:
                        subIdentCount++;
                        tabs += "\t";
                        this.Result += $"{tabs}if var{function.Instructions[i].B} <=> {function.Instructions[i].C} then\n";
                        this.Result += $"{tabs}\tvar{function.Instructions[i].A} = var{function.Instructions[i].B}\n";
                        subIdentCount--;
                        tabs = tabs.Substring(1);
                        this.Result += $"end\n";
                        break;

                    case LuaOpcode.CALL:
                        StringBuilder sb = new StringBuilder();

                        if (function.Instructions[i].C != 0)
                        {
                            sb.Append(tabs);
                            var indentLen = sb.Length;

                            // return values
                            for (int x = function.Instructions[i].A; x < function.Instructions[i].A + function.Instructions[i].C - 2; ++x)
                                sb.AppendFormat("var{0}, ", x);

                            if (sb.Length - indentLen > 2)
                            {
                                sb.Remove(sb.Length - 2, 2);
                                sb.Append(" = ");
                            }
                        }
                        else
                        {
                            this.Result += "function.Instructions[i].C == 0\n";
                        }

                        // function
                        sb.AppendFormat("var{0}(", function.Instructions[i].A);

                        if (function.Instructions[i].B != 0)
                        {
                            var preArgsLen = sb.Length;

                            // arguments
                            for (int x = function.Instructions[i].A; x < function.Instructions[i].A + function.Instructions[i].B - 1; ++x)
                                sb.AppendFormat("var{0}, ", x + 1);

                            if (sb.Length - preArgsLen > 2)
                                sb.Remove(sb.Length - 2, 2);

                            sb.Append(')');
                        }
                        else
                        {
                            this.Result += $"{tabs}function.Instructions[i].B == 0\r\n";
                        }

                        this.Result += sb.ToString() + "\r\n";
                        break;

                    case LuaOpcode.TAILCALL:
                        this.Result += $"{tabs}TAILCALL\r\n"; // TODO: this happends when a return statemenet has a single function call as the expression
                        break;
                    case LuaOpcode.RETURN:
                        if (tabs.Length == 0 || i == function.Instructions.Count-1)
                        {
                            this.Result += "end\r\n";
                            break;
                        }

                        if (function.Instructions[i].B == 1)
                            this.Result += $"{tabs}return\r\n";
                        else if (function.Instructions[i].B > 1)
                        {
                            this.Result += $"{tabs}return ";
                            for (int j = 0; j < function.Instructions[i].B - 1; j++)
                            {
                                this.Result += $"{WriteIndex(function.Instructions[i].A + j, function, false)}"; // from A to A+(B-2)
                                if (j < function.Instructions[i].B - 2)
                                    this.Result += ", ";
                            }

                            this.Result += "\r\n";
                        }
                        else
                        {
                            this.Result += $"{tabs}return ";
                            for (int j = function.Instructions[i].A; j < function.MaxStackSize; j++)
                            {
                                this.Result += $"{WriteIndex(function.Instructions[i].A + j, function)}"; // from A to top
                                if (j < function.MaxStackSize - 1)
                                    this.Result += ", ";
                            }
                        }
                        tabs = tabs.Substring(1);
                        subIdentCount++;
                        break;

                    case LuaOpcode.FORLOOP:
                        this.Result += $"{tabs}FORLOOP\r\n"; // TODO: implement
                        // FORLOOP    A sBx   R(A)+=R(A+2);
                        // if R(A) <?= R(A + 1) then { pc += sBx; R(A + 3) = R(A) }
                        break;
                    case LuaOpcode.FORPREP:
                        this.Result += $"{tabs}FORPREP\r\n"; // TODO: implement
                        // FORPREP    A sBx   R(A)-=R(A+2); pc+=sBx
                        break;
                    case LuaOpcode.TFORLOOP:
                        this.Result += $"{tabs}TFORLOOP\r\n"; // TODO: implement
                        // TFORLOOP    A sBx      if R(A+1) ~= nil then { R(A)=R(A+1); pc += sBx }
                        break;
                    case LuaOpcode.SETLIST:
                        // subtract lines from the result
                        this.Result += $"{tabs}{WriteIndex(function.Instructions[i].A, function)} = {{";
                        for (int j = 1; j <= function.Instructions[i].B; j++)
                        {
                            // table = function.Instructions[i].A
                            this.Result += $"{WriteIndex(function.Instructions[i].A+j, function)}";
                            if (j < function.Instructions[i].B)
                                this.Result += ", ";
                        }
                        this.Result += $"}}\r\n"; // TODO: implement
                        break;
                    case LuaOpcode.CLOSE:
                        this.Result += $"{tabs}CLOSE\r\n"; // TODO: implement
                        break;
                    case LuaOpcode.CLOSURE:
                        this.Result += $"{tabs}CLOSURE\r\n"; // TODO: implement
                        break;
                    case LuaOpcode.VARARG:
                        this.Result += $"{tabs}VARARG\r\n"; // TODO: implement
                        break;
                }
            }
        }

        private string GetConstant(int idx, LuaFunction function)
        {
            return function.Constants[idx].ToString();
        }

        private int ToIndex(int value, out bool isConstant)
        {
            // this is the logic from lua's source code (lopcodes.h)
            if (isConstant = (value & 1 << 8) != 0)
                return value & ~(1 << 8);
            else
                return value;
        }

        private string WriteIndex(int value, LuaFunction function, bool? constant = null)
        {
            bool constant2 = false;
            int idx = value;
            if (!constant.HasValue || constant == true)
                idx = ToIndex(value, out constant2);

            if (constant2)
                return function.Constants[idx].ToString();
            else
            {
                string data = "";
                if(VariableUsageCache[CurrentIndentLevel].IndexOf(value) == -1 && !constant.HasValue)
                {
                    data += "local ";
                    VariableUsageCache[CurrentIndentLevel].Add(value);
                }
                return data + "var" + idx;
            }
            
        }

    }
}
