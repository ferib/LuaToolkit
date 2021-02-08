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
        private bool BigEndian;
        private int IntSize;
        private int SizeT;
        private int Index;
        private byte[] Buffer;
        public LuaFunction Function;
        private LuaRegisters Registers;
        private Dictionary<int, object> Stack;
        //private new List<LuaConstant> Constants;
        private Dictionary<int, object> Upvalues;
        private Dictionary<int, object> Environment;
        private Dictionary<LuaOpcode, Action> InstructionTable;

        private int hits = 0;

        public LuaDecoder(byte[] LuaC)
        {
            this.Index = 0;
            this.Buffer = LuaC;
            this.Registers = new LuaRegisters();
            this.Stack = new Dictionary<int, object>();
            //this.Constants = new List<LuaConstant>();
            this.Upvalues = new Dictionary<int, object>();
            this.Environment = new Dictionary<int, object>();
            
            if(Verify())
                this.Function = DecodeFunctionblock(); // init the Lua stuff
        }

        public void Execute()
        {
            List<int> localStack = new List<int>();
            List<int> ghostStack = new List<int>();

            loop(); // execute the bytecode
        }

        // check if input is as expected
        private bool Verify()
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
            this.BigEndian = GetByte() == 0;
            this.IntSize = GetByte();
            this.SizeT = GetByte();

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

        private void loop()
        {
            while (this.Registers.IP < this.Buffer.Length)
            {
                Console.WriteLine($"{this.Registers.IP.ToString("X4")}: {((LuaOpcode)this.Buffer[this.Registers.IP]).ToString().PadLeft(8)} ...");
                this.InstructionTable[(LuaOpcode)this.Buffer[this.Registers.IP]]();
            }
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
            return this.Buffer[this.Index - 1];
        }

        private int GetInt()
        {
            this.Index += 4;
            return BitConverter.ToInt32(this.Buffer, this.Index - 4);
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
            return BitConverter.ToSingle(this.Buffer, this.Index - 4);
        }

        private double GetFloat2()
        {
            this.Index += 8;
            return BitConverter.ToDouble(this.Buffer, this.Index - 8);
        }

        private long GetLong()
        {
            this.Index += 8;
            return BitConverter.ToInt64(this.Buffer, this.Index - 8);
        }

        private string GetString(long len = 0)
        {
            if(len == 0)
            {
                if(this.SizeT == 4)
                    len = GetInt(); // get_size_t (4 byte?)
                else if (this.SizeT == 8)
                    len = GetLong(); // get_size_t (8 byte?)
            }
                
            string str = "";
            for(int i = 0; i < len && this.Index + i < this.Buffer.Length; i++)
                str += (char)this.Buffer[this.Index+i];
            this.Index += (int)len;
            return str;
        }
        #endregion
    }
}
