using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Disassembler
{
    public class LuaEncoder
    {
        public LuaCFile File;
        private List<byte> Buffer; // temp storage

        // NOTE: turns a Decoded Lua file back to its bytecode
        public LuaEncoder(LuaCFile file)
        {
            this.File = file;
            this.Buffer = new List<byte>();
        }

        public byte[] SaveFile()
        {
            WriteHeader();
            EncodeFunctionblock(this.File.Function);
            return this.Buffer.ToArray();
        }

        public void WriteHeader()
        {
            // write the magic
            SetString("\x1BLua", false);

            // set version
            SetByte(0x51);

            SetByte(this.File.Format);
            if (this.File.BigEndian)
                SetByte(0);
            else
                SetByte(1);
            SetByte(this.File.IntSize);
            SetByte(this.File.SizeTSize);
            SetByte(this.File.InstructionSize);
            SetByte(this.File.LuaNumberSize);
            SetByte(this.File.Integral);
        }

        public void EncodeFunctionblock(Function Function)
        {
            SetString(Function.Name);
            SetInt(Function.FirstLineNr);
            SetInt(Function.LastLineNr);

            SetByte(Function.UpvaluesCount);
            SetByte(Function.ArgsCount);
            SetByte((byte)Function.VarArg);
            SetByte(Function.MaxStackSize);

            // Encode instructions
            WriteInstructions(Function.Instructions);

            // Encode constants
            WriteConstants(Function.Constants);

            // Encode functions
            WriteFunctions(Function.Functions);

            // Encode debuginfo: Line Numbers
            WriteDebugLines(Function.DebugLines);

            // Encode debuginfo: Locals
            WriteDebugLocals(Function.Locals);

            // Encode debuginfo: Upvalues
            WriteDebugUpvals(Function.Upvals);
        }

        public void WriteInstructions(List<Instruction> Instructions)
        {
            SetInt(Instructions.Count);
            for (int i = 0; i < Instructions.Count; i++)
            {
                SetUInt(Instructions[i].Data);
            }
        }

        public void WriteConstants(List<ByteConstant> Constants)
        {
            SetInt(Constants.Count);
            for (int i = 0; i < Constants.Count; i++)
            {
                SetByte((byte)Constants[i].Type);

                switch (Constants[i].Type)
                {
                    case LuaType.Nil:
                        break;
                    case LuaType.Bool:
                        var val = (BoolByteConstant)Constants[i];
                        if (val.Value)
                            SetByte(1);
                        else
                            SetByte(0);
                        break;
                    case LuaType.Number:
                        var num = (NumberByteConstant)Constants[i];
                        SetFloat2(num.Value);
                        break;
                    case LuaType.String:
                        var str = (StringByteConstant)Constants[i];
                        SetString(str.Value);
                        break;
                }
            }
        }

        public void WriteFunctions(List<Function> Functions)
        {
            SetInt(Functions.Count);
            for (int i = 0; i < Functions.Count; i++)
            {
                EncodeFunctionblock(Functions[i]);
            }
        }

        public void WriteDebugLines(List<int> Lines)
        {
            SetInt(Lines.Count);
            for (int i = 0; i < Lines.Count; i++)
            {
                SetInt(Lines[i]);
            }
        }

        public void WriteDebugLocals(List<Local> Locals)
        {
            SetInt(Locals.Count);
            for (int i = 0; i < Locals.Count; i++)
            {
                SetString(Locals[i].Name);
                SetInt(Locals[i].ScopeStart);
                SetInt(Locals[i].ScopeEnd);
            }
        }

        public void WriteDebugUpvals(List<string> Upvals)
        {
            SetInt(Upvals.Count);
            for (int i = 0; i < Upvals.Count; i++)
            {
                SetString(Upvals[i]);
            }
        }

        // Helpers
        #region Helpser

        public void SetByte(byte d)
        {
            this.Buffer.Add(d);
        }

        public void SetInt(int d)
        {
            this.Buffer.AddRange(BitConverter.GetBytes(d));
        }

        public void SetUInt(uint d)
        {
            this.Buffer.AddRange(BitConverter.GetBytes(d));
        }

        public void SetFloat(float d)
        {
            this.Buffer.AddRange(BitConverter.GetBytes(d));
        }

        public void SetFloat2(double d)
        {
            this.Buffer.AddRange(BitConverter.GetBytes(d));
        }

        public void SetLong(long d)
        {
            this.Buffer.AddRange(BitConverter.GetBytes(d));
        }

        public void SetString(string str, bool setLen = true)
        {
            //str += "\0";
            if (setLen)
            {
                if (this.File.SizeTSize == 4)
                    this.Buffer.AddRange(BitConverter.GetBytes((int)str.Length));
                if (this.File.SizeTSize == 8)
                    this.Buffer.AddRange(BitConverter.GetBytes((long)str.Length));
            }

            this.Buffer.AddRange(Encoding.UTF8.GetBytes(str));
        }

        #endregion

    }
}
