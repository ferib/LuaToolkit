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
        private List<LuaScriptLine> LuaScriptLines;

        private string LuaScript
        {
            get { return GetScript(); }
        }

        public LuaWriter(ref LuaDecoder decoder)
        {
            this.Decoder = decoder;
            this.LuaScriptLines = new List<LuaScriptLine>();
            WriteFunction(decoder.File.Function);
        }


        private void WriteFunction(LuaFunction func, int dpth = 0)
        {
            for(int i = 0; i < func.Instructions.Count; i++)
            {
                this.LuaScriptLines.Add(new LuaScriptLine(func.Instructions[i], ref this.Decoder, ref func)
                {
                    Number = i,
                    Depth = dpth
                });
            }
        }

        private string GetScript()
        {
            string result = "";
            foreach (var l in LuaScriptLines)
                result += l.Text;
            return result;
        }
    }

    public class LuaScriptLine
    {
        public int Depth;
        public int Number // Line number OR index?
        {
            set 
            {
                Number = value;
                NumberEnd = value; // + 1;
            }
            get { return Number; }
        }

        public int NumberEnd; // in case we need more

        private OpcodeType OpType;

        private LuaDecoder Decoder;
        private LuaFunction Func;
        private LuaInstruction Instr;

        private string Op1; // opperands ;D
        private string Op2;
        private string Op3;

        public string Text
        {
            get { return ToString(); }
        }

        public LuaScriptLine(LuaInstruction instr, ref LuaDecoder decoder, ref LuaFunction func)
        {
            this.Instr = instr;
            this.Func = func;
            SetType();
            SetMain();
        }

        private void SetMain()
        {
            switch (this.Instr.OpCode)
            {
                case LuaOpcode.MOVE:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = WriteIndex(Instr.B);
                    break;
                case LuaOpcode.LOADK:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = GetConstant(Instr.Bx);
                    break;
                case LuaOpcode.LOADBOOL:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = Instr.B != 0 ? "true" : "false";
                    break;
                case LuaOpcode.LOADNIL:
                    for (int i = Instr.A; i < Instr.B + 1; ++i)
                    {
                        this.Op1 += $"{WriteIndex(i)} = nil\r\n"; // TODO: turn into new class?
                        this.NumberEnd++;
                    }   
                    break;
                case LuaOpcode.GETUPVAL:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = $"upvalue[{Instr.B}]";
                    break;
                case LuaOpcode.GETGLOBAL:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = $"_G[{GetConstant(Instr.B)}]";
                    break;
                case LuaOpcode.GETTABLE:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = $"var{Instr.B}[{WriteIndex(Instr.C)}]";
                    break;
                case LuaOpcode.SETGLOBAL:
                    this.Op1 = $"_G[{WriteIndex(Instr.Bx)}]";
                    this.Op2 = " = ";
                    this.Op3 = $"var{Instr.A}]";
                    break;
                case LuaOpcode.SETUPVAL:
                    this.Op1 = $"upvalue[{WriteIndex(Instr.B)}]";
                    this.Op2 = " = ";
                    this.Op3 = $"var[{GetConstant(Instr.A)}]";
                    break;
                case LuaOpcode.SETTABLE:
                    this.Op1 = $"{WriteIndex(Instr.A)}[{WriteIndex(Instr.B)}]";
                    this.Op2 = " = ";
                    this.Op3 = WriteIndex(Instr.C);
                    break;
                case LuaOpcode.NEWTABLE:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = "{{}}";
                    break;
                case LuaOpcode.SELF:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = " = ";
                    this.Op3 = $"var{Instr.B}\r\b{WriteIndex(Instr.A)} = var{Instr.B}[{WriteIndex(Instr.C)}]";
                    // TODO fix, multiline?
                    break;
                case LuaOpcode.ADD:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" + var{Instr.C}";
                    break;
                 case LuaOpcode.SUB:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" + var{Instr.C}";
                    break;
                 case LuaOpcode.MUL:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" + var{Instr.C}";
                    break;
                 case LuaOpcode.DIV:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" + var{Instr.C}";
                    break;
                 case LuaOpcode.MOD:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" + var{Instr.C}";
                    break;
                 case LuaOpcode.POW:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" + var{Instr.C}";
                    break;
                 case LuaOpcode.UNM:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" + var{Instr.C}";
                    break;
                 case LuaOpcode.NOT:
                    this.Op1 = WriteIndex(Instr.A);
                    this.Op2 = $" = var{Instr.B}";
                    this.Op3 = $" + var{Instr.C}";
                    break;
                

                    // ez
            }
        }

        private void SetType()
        {
            switch (this.Instr.OpCode)
            {
                case LuaOpcode.LOADK:
                case LuaOpcode.GETGLOBAL:
                case LuaOpcode.SETGLOBAL:
                case LuaOpcode.CLOSURE:
                    this.OpType = OpcodeType.ABx;
                    break;
                case LuaOpcode.FORLOOP:
                case LuaOpcode.FORPREP:
                case LuaOpcode.JMP:
                    this.OpType = OpcodeType.AsBx;
                    break;
                default:
                    this.OpType = OpcodeType.ABC;
                    break;
            }
        }

        private string GetConstant(int index)
        {
            return this.Func.Constants[index].ToString();
        }

        private string WriteIndex(int value)
        {
            bool constant = false;
            int index = ToIndex(value, out constant);

            if (constant)
                return this.Func.Constants[index].ToString();
            else
            {
                // TODO: check if local and not yet used!
                return "var" + index;
            }
        }

        private int ToIndex(int value, out bool isConstant)
        {
            // this is the logic from lua's source code (lopcodes.h)
            if (isConstant = (value & 1 << 8) != 0)
                return value & ~(1 << 8);
            else
                return value;
        }

        public override string ToString()
        {
            string tab = new string(' ', Depth); // NOTE: singple space for debugging
            return $"{tab}{Op1}{Op2}{Op3}\r\n";
        }
    }
}
