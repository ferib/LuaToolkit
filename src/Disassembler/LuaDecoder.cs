using System;
using System.Collections.Generic;

namespace LuaToolkit.Disassembler
{
    // TODO: add new classes for each version?
    //
    public class LuaDecoder
    {
        public LuaCFile File;
        private int Index;

        // NOTE: read the LuaCFILE and create stuff
        public LuaDecoder(LuaCFile file)
        {
            this.File = file;
            this.Index = 0;

            if (ReadHeader())
            {
                this.File.Function = DecodeFunctionblock(); // init the Lua stuff
            }

        }

        // check if input is as expected
        private bool ReadHeader()
        {
            // check magic bytes
            if (GetString(4) != "\x1BLua")
            {
                Console.WriteLine("Error, LuaC File Expected!");
                return false;
            }

            // check version
            if (GetByte() != 0x51)
            {
                Console.WriteLine("Error, Only Lua with version 5.1 is supported!");
                return false;
            }

            // general settings
            // LuaFormat being used, 0 == official format.
            this.File.Format = GetByte();
            // 1 == little endian
            this.File.BigEndian = GetByte() == 0;
            this.File.IntSize = GetByte();
            this.File.SizeTSize = GetByte();
            this.File.InstructionSize = GetByte();
            this.File.LuaNumberSize = GetByte();
            this.File.Integral = GetByte();

            return true;
        }

        // decode the metadata and what not from the LuaC file
        private Function DecodeFunctionblock()
        {
            Function function = new Function();

            // First is the length of the string.
            // Followed by the source file name.
            function.Name = GetString();     // Function name
            function.FirstLineNr = GetInt();   // First line // 4 or 8?
            function.LastLineNr = GetInt();    // Last line // 4 or 8?

            if (function.Name != "")
                function.Name = function.Name.Substring(0, function.Name.Length - 1);

            // point around
            function.UpvaluesCount = GetByte();
            function.ArgsCount = GetByte();
            function.VarArg = (VarArg)GetByte();
            function.MaxStackSize = GetByte();

            // Decode instructions
            
            ReadInstructions(function);

            // Decode constants
            ReadConstants(function);

            // Decode functions
            ReadFunctions(function);

            // Decode debuginfo: Line Numbers
            ReadDebugLines(function);

            // Decode debuginfo: Locals
            ReadDebugLocals(function);

            // Decode debuginfo: Upvalues
            ReadUpvalues(function);

            return function;
        }

        private void ReadInstructions(Function func)
        {
            int count = GetInt(); // 4 or 8?
            for (int i = 0; i < count; i++)
            {
                // TODO: handle CLOSURE??
                Instruction instr = new Instruction(GetUInt(), i + 1);
                func.AddInstruction(instr);
            }
        }

        private void ReadConstants(Function func)
        {
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                byte type = GetByte();

                switch ((LuaType)type)
                {
                    case LuaType.Nil:
                        func.AddConstant(new NilByteConstant());
                        break;
                    case LuaType.Bool:
                        func.AddConstant(new BoolByteConstant(GetByte() != 0));
                        break;
                    case LuaType.Number:
                        func.AddConstant(new NumberByteConstant(GetFloat2()));
                        break;
                    case LuaType.String:
                        func.AddConstant(new StringByteConstant(GetString()));
                        break;
                }
            }
        }

        private void ReadFunctions(Function func)
        {
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                func.AddFunction(DecodeFunctionblock());
            }
        }

        // TODO Should be a map that links pc to line number.
        // i is the pc and int the line number.
        private void ReadDebugLines(Function func)
        {
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                func.AddDebugLine(GetInt());
            }
        }

        private void ReadDebugLocals(Function func)
        {
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                func.AddLocal(new Local(GetString(), GetInt(), GetInt()));
            }
        }

        private void ReadUpvalues(Function func)
        {
            // Upvals are a list of strings that refer to a local var from the parent.
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                func.AddUpval(GetString()); // Upvalue name
            }
        }

        // Helpers
        #region Helpers
        private int GetBits(int input, int n, int n2 = -1)
        {
            if (n2 != -1)
            {
                int total = 0;
                int digitn = 0;
                for (int i = n; i < n2; i++)
                {
                    total += 2 ^ digitn * GetBits(input, i);
                }
                return total;
            }
            else
            {
                int pn = 2 ^ (n - 1);
                bool res = false;
                if (pn != 0)
                    res = ((input % (pn + pn) >= pn));
                //bool res = ((input % (pn + pn) >= pn) && 1 || 0);
                if (res)
                    return 1;
                return 0;
            }

        }

        private byte GetByte()
        {
            this.Index++;
            return this.File.Buffer[this.Index - 1];
        }

        private int GetInt()
        {
            this.Index += 4;
            return BitConverter.ToInt32(this.File.Buffer, this.Index - 4);
        }

        private uint GetUInt()
        {
            this.Index += 4;
            return BitConverter.ToUInt32(this.File.Buffer, this.Index - 4);
        }

        //private double ReadNumber(byte numSize)
        //{
        //    byte[] bytes = this.Buffer.Co(numSize);
        //    double ret = 0;

        //    if (numSize == 8)
        //    {
        //        ret = BitConverter.ToDouble(bytes, 0);
        //    }
        //    else if (numSize == 4)
        //    {
        //        ret = BitConverter.ToSingle(bytes, 0);
        //    }
        //    else
        //    {
        //        throw new NotImplementedException("Uhm...");
        //    }

        //    return ret;
        //}

        private float GetFloat()
        {
            this.Index += 4;
            return BitConverter.ToSingle(this.File.Buffer, this.Index - 4);
        }

        private double GetFloat2()
        {
            this.Index += 8;
            return BitConverter.ToDouble(this.File.Buffer, this.Index - 8);
        }

        private long GetLong()
        {
            this.Index += 8;
            return BitConverter.ToInt64(this.File.Buffer, this.Index - 8);
        }

        private string GetString(long len = 0)
        {
            if (len == 0)
            {
                if (this.File.SizeTSize == 4)
                    len = GetInt(); // get_size_t (4 byte?)
                else if (this.File.SizeTSize == 8)
                    len = GetLong(); // get_size_t (8 byte?)
            }

            if (this.File.Buffer == null)
                return "error";

            string str = "";
            for (int i = 0; i < len && this.Index + i < this.File.Buffer.Length; i++)
                str += (char)this.File.Buffer[this.Index + i];
            this.Index += (int)len;
            return str;
        }
        #endregion
    }
}
