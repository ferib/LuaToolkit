using LuaSharpVM.Core;
using LuaSharpVM.Models;
using System.Text;

namespace LuaSharpVM.Decompiler
{
    public class LuaDecompiler
    {
        private uint FunctionsCount;
        public string Result;
        private byte[] Buffer;

        public LuaDecompiler(byte[] Buffer)
        {
            this.Buffer = Buffer;
        }

        public void Write(LuaFunction function, int indentLevel = 0)
        {
            // top level function
            if (function.FirstLineNr == 0 && function.LastLineNr == 0)
            {
                WriteChildFunctions(function);
                WriteInstructions(function);
            }
            else
            {
                string indents = new string('\t', indentLevel);

                string functionHeader = indents + "function func" + FunctionsCount + "(";

                for (int i = 0; i < function.ArgsCount; ++i)
                {
                    functionHeader += "arg" + i + (i + 1 != function.ArgsCount ? ", " : ")");
                }

                this.Result += functionHeader + "\r\n";
                //writer.Write(functionHeader);
                ++FunctionsCount;

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
                Write(f, indentLevel + 1);
            }
        }

        private void WriteInstructions(LuaFunction function, int indentLevel = 0)
        {
            // TODO: complete all instructions
            string tabs = new string('\t', indentLevel);

            int subIdentCount = 0;

            foreach (var i in function.Instructions)
            {
                switch (i.OpCode)
                {
                    case LuaOpcode.MOVE:
                        this.Result += $"{tabs}var{i.A} = var{i.B}\r\n";
                        break;

                    case LuaOpcode.LOADK:
                        this.Result += $"{tabs}var{i.A} = {GetConstant(i.Bx, function)}\r\n";
                        break;

                    case LuaOpcode.LOADBOOL:
                        this.Result += $"{tabs}var{i.A} = {(i.B != 0 ? "true" : "false")}\r\n"; // TODO: check double instructions
                        break;

                    case LuaOpcode.LOADNIL:
                        for (int x = i.A; x < i.B + 1; ++x)
                            this.Result += $"{tabs}var{x} = nil\r\n";
                        break;

                    case LuaOpcode.GETUPVAL:
                        this.Result += $"{tabs}var{i.A} = upvalue[{i.B}]\r\n";
                        break;

                    case LuaOpcode.GETGLOBAL:
                        this.Result += $"{tabs}var{i.A} = _G[{GetConstant(i.Bx, function)}]\r\n";
                        break;

                    case LuaOpcode.GETTABLE:
                        this.Result += $"{tabs}var{i.A} = var{i.B}[{WriteIndex(i.C, function)}]\r\n";
                        break;

                    case LuaOpcode.SETGLOBAL:
                        this.Result += $"{tabs}_G[{GetConstant(i.Bx, function)}] = var{i.A}\r\n";
                        break;

                    case LuaOpcode.SETUPVAL:
                        this.Result += $"{tabs}upvalue[{i.B}] = var{i.A}\r\n";
                        break;

                    case LuaOpcode.SETTABLE:
                        this.Result += $"{tabs}var{i.A}[{WriteIndex(i.B, function)}] = {WriteIndex(i.C, function)}\r\n";
                        break;

                    case LuaOpcode.NEWTABLE:
                        this.Result += $"{tabs}var{i.A} = {{}}\r\n";
                        break;

                    case LuaOpcode.SELF:
                        this.Result += $"{tabs}var{i.A} = var{i.B}\r\n";
                        this.Result += $"{tabs}var{i.A} = var{i.B}[{WriteIndex(i.C, function)}]\r\n";
                        break;

                    case LuaOpcode.ADD:
                        this.Result += $"{tabs}var{i.A} = var{i.B} + var{i.C}\r\n";
                        break;

                    case LuaOpcode.SUB:
                        this.Result += $"{tabs}var{i.A} = var{i.B} - var{i.C}\r\n";
                        break;

                    case LuaOpcode.MUL:
                        this.Result += $"{tabs}var{i.A} = var{i.B} * var{i.C}\r\n";
                        break;

                    case LuaOpcode.DIV:
                        this.Result += $"{tabs}var{i.A} = var{i.B} / var{i.C}\r\n";
                        break;

                    case LuaOpcode.MOD:
                        this.Result += $"{tabs}var{i.A} = var{i.B} % var{i.C}\r\n";
                        break;

                    case LuaOpcode.POW:
                        this.Result += $"{tabs}var{i.A} = var{i.B} ^ var{i.C}\r\n";
                        break;

                    case LuaOpcode.UNM:
                        this.Result += $"{tabs}var{i.A} = -var{i.B}\r\n";
                        break;

                    case LuaOpcode.NOT:
                        this.Result += $"{tabs}var{i.A} = not var{i.B}\r\n";
                        break;

                    case LuaOpcode.LEN:
                        this.Result += $"{tabs}var{i.A} = #var{i.B}\r\n";
                        break;

                    case LuaOpcode.CONCAT:
                        this.Result += $"{tabs}var{i.A} = ";

                        for (int x = i.B; x < i.C; ++x)
                            this.Result += $"var{x} .. \r\n";

                        this.Result += $"var{i.C}\r\n";
                        break;

                    case LuaOpcode.JMP:
                        
                        this.Result += $"{tabs}JMP ({(short)i.sBx})\r\n"; // TODO:
                        // JMP A sBx   pc+=sBx; if (A) close all upvalues >= R(A - 1)
                        if((short)i.sBx == 0)
                        {
                            subIdentCount--; // TODO: verify subs
                            tabs = tabs.Substring(1);
                        }
                        break;

                    case LuaOpcode.EQ:
                        this.Result += $"{tabs}if ({WriteIndex(i.B, function)} == {WriteIndex(i.C, function)}) ~= {i.A} then\r\n";
                        subIdentCount++;
                        tabs += "\t";
                        break;

                    case LuaOpcode.LT:
                        this.Result += $"{tabs}if ({WriteIndex(i.B, function)} < {WriteIndex(i.C, function)}) ~= {i.A} then\r\n";
                        subIdentCount++;
                        tabs += "\t";
                        break;

                    case LuaOpcode.LE:
                        this.Result += $"{tabs}if ({WriteIndex(i.B, function)} <= {WriteIndex(i.C, function)}) ~= {i.A} then\r\n";
                        subIdentCount++;
                        tabs += "\t";
                        break;

                    case LuaOpcode.TEST:
                        this.Result += $"{tabs}if not var{i.A} <=> {i.C} then\r\n";
                        subIdentCount++;
                        tabs += "\t";
                        break;

                    case LuaOpcode.TESTSET:
                        subIdentCount++;
                        tabs += "\t";
                        this.Result += $"{tabs}if var{i.B} <=> {i.C} then\n";
                        this.Result += $"{tabs}\tvar{i.A} = var{i.B}\n";
                        subIdentCount--;
                        tabs = tabs.Substring(1);
                        this.Result += $"end\n";
                        break;

                    case LuaOpcode.CALL:
                        StringBuilder sb = new StringBuilder();

                        if (i.C != 0)
                        {
                            sb.Append(tabs);
                            var indentLen = sb.Length;

                            // return values
                            for (int x = i.A; x < i.A + i.C - 2; ++x)
                                sb.AppendFormat("var{0}, ", x);

                            if (sb.Length - indentLen > 2)
                            {
                                sb.Remove(sb.Length - 2, 2);
                                sb.Append(" = ");
                            }
                        }
                        else
                        {
                            this.Result += "i.C == 0\n";
                        }

                        // function
                        sb.AppendFormat("var{0}(", i.A);

                        if (i.B != 0)
                        {
                            var preArgsLen = sb.Length;

                            // arguments
                            for (int x = i.A; x < i.A + i.B - 1; ++x)
                                sb.AppendFormat("var{0}, ", x + 1);

                            if (sb.Length - preArgsLen > 2)
                                sb.Remove(sb.Length - 2, 2);

                            sb.Append(')');
                        }
                        else
                        {
                            this.Result += $"{tabs}i.B == 0\r\n";
                        }

                        this.Result += sb.ToString() + "\r\n";
                        break;

                    case LuaOpcode.TAILCALL:
                        this.Result += $"{tabs}TAILCALL\r\n"; // TODO: implement
                        break;
                    case LuaOpcode.RETURN:
                        if (tabs.Length == 0)
                        {
                            this.Result += "end\r\n";
                            break;
                        }
                            
                        if (i.B == 1)
                            this.Result += $"{tabs}return\r\n";
                        else
                        {
                            this.Result += $"{tabs}return ";
                            var start = i.B-1;
                            if (start < 0)
                                for (int j = i.A; j <= function.MaxStackSize; j++)
                                {
                                    this.Result += $"{WriteIndex(i.A + j, function)}"; // from A to top of stack
                                    if (j < function.MaxStackSize)
                                        this.Result += ", ";
                                }
                                    
                            else
                                for (int j = start; j <= i.B; j++)
                                {
                                    this.Result += $"{WriteIndex(i.A + j - 2, function)}"; // from A to A+(B-2)
                                    if (j < i.B)
                                        this.Result += ", ";
                                }
                                    
                            this.Result += "\r\n";
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
                        this.Result += $"{tabs}SETLIST\r\n"; // TODO: implement
                        // SETLIST A B C   R(A)[(C-1)*FPF+i] := R(A+i), 1 <= i <= B
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

        private string WriteIndex(int value, LuaFunction function)
        {
            bool constant;
            int idx = ToIndex(value, out constant);

            if (constant)
                return function.Constants[idx].ToString();
            else
                return "var" + idx;
        }

    }
}
