using System;
using System.Collections.Generic;
using LuaSharpVM.Models;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Emulator;

namespace LuaSharpVM.Disassembler
{
    public class LuaDecoder
    {
        public LuaCFile File;
        private int Index;

        // NOTE: read the LuaCFILE and create stuff
        public LuaDecoder(LuaCFile file)
        {
            this.File = file;
            this.Index = 0;
            
            if(ReadHeader())
                this.File.Function = DecodeFunctionblock(); // init the Lua stuff
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

            GetByte(); // another bytecode
            this.File.BigEndian = GetByte() == 0;
            this.File.IntSize = GetByte();
            this.File.SizeT = GetByte();

            // TODO: figure out what size_t and so are used for

            string bytecodeTarget = GetString(3);
            if (bytecodeTarget != "\x04\x08\x00")
                return false;

            // Index = 11 after this

            return true;
        }

        // decode the metadata and what not from the LuaC file
        private LuaFunction DecodeFunctionblock()
        {
            LuaFunction Function = new LuaFunction();

            Function.Name = GetString();     // Function name
            Function.FirstLineNr = GetInt();   // First line // 4 or 8?
            Function.LastLineNr = GetInt();    // Last line // 4 or 8?

            if (Function.Name != "")
                Function.Name = Function.Name.Substring(0, Function.Name.Length-1);

            // point around
            Function.UpvaluesCount = GetByte();
            Function.ArgsCount = GetByte();
            Function.Vargs = (VarArg)GetByte();
            Function.MaxStackSize = GetByte();

            // Decode Instructions
            Function.Instructions = ReadInstructions();

            // Decode constants
            Function.Constants = ReadConstants();

            // Decode functions
            Function.Functions = ReadFunctions();

            // Decode debuginfo: Line Numbers
            Function.DebugLines = ReadDebugLines();

            // Decode debuginfo: Locals
            Function.DebugLocals = ReadDebugLocals();
            
            // Decode debuginfo: Upvalues
            Function.DebugUpvalues = ReadDebugUpvalues();

            return Function;
        }

        private List<LuaInstruction> ReadInstructions()
        {
            List<LuaInstruction> Instructions = new List<LuaInstruction>();
            int count = GetInt(); // 4 or 8?
            for (int i = 0; i < count; i++)
            {
                LuaInstruction instr = new LuaInstruction(GetInt());
                Instructions.Add(instr);
            }
            return Instructions;
        }

        private List<LuaConstant> ReadConstants()
        {
            List<LuaConstant> Constants = new List<LuaConstant>();
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                byte type = GetByte();

                switch ((LuaType)type)
                {
                    case LuaType.Nil:
                        Constants.Add(new NilConstant());
                        break;
                    case LuaType.Bool:
                        Constants.Add(new BoolConstant(GetByte() != 0));
                        break;
                    case LuaType.Number:
                        Constants.Add(new NumberConstant(GetFloat2()));
                        break;
                    case LuaType.String:
                        Constants.Add(new StringConstant(GetString()));
                        break;
                }
            }
            return Constants;
        }

        private List<LuaFunction> ReadFunctions()
        {
            List<LuaFunction> functions = new List<LuaFunction>();
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                functions.Add(DecodeFunctionblock());
            }
            return functions;
        }

        private List<int> ReadDebugLines()
        {
            List<int> debuglines = new List<int>();
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                debuglines.Add(GetInt());
            }
            return debuglines;
        }

        private List<LuaLocal> ReadDebugLocals()
        {
            List<LuaLocal> locals = new List<LuaLocal>();
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                locals.Add(new LuaLocal(GetString(), GetInt(), GetInt()));
            }
            return locals;
        }

        private List<string> ReadDebugUpvalues()
        {
            List<string> upvalues = new List<string>();
            int count = GetInt();
            for (int i = 0; i < count; i++)
            {
                upvalues.Add(GetString()); // Upvalue name
            }
            return upvalues;
        }

        // Helpers
        #region Helpers
        private int GetBits(int input, int n, int n2 = -1)
        {
            if (n2 != -1)
            {
                int total = 0;
                int digitn = 0;
                for(int i = n; i < n2; i++)
                {
                    total += 2 ^ digitn * GetBits(input, i);
                }
                return total;
            }
            else
            {
                int pn = 2 ^ (n - 1);
                bool res = false;
                if(pn != 0)
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
            if(len == 0)
            {
                if(this.File.SizeT == 4)
                    len = GetInt(); // get_size_t (4 byte?)
                else if (this.File.SizeT == 8)
                    len = GetLong(); // get_size_t (8 byte?)
            }
                
            string str = "";
            for(int i = 0; i < len && this.Index + i < this.File.Buffer.Length; i++)
                str += (char)this.File.Buffer[this.Index+i];
            this.Index += (int)len;
            return str;
        }
        #endregion
    }
}
