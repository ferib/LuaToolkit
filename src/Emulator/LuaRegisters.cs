//using System;
//using System.Collections.Generic;
//using System.Text;

//namespace LuaSharpVM.Emulator
//{
//    public class LuaRegisters
//    {
//        public byte A;  // 8 bits
//        public ushort B;  // 9 bits
//        public ushort C;  // 9 buts
//        public int Ax  // 26 nits (A, B and C)
//        {
//            get { return A + B + C; }
//            //set { A = value; }
//        }
//        public int Bx;  // 18 bits (B and C)
//        public int sBx // signed Bx
//        {
//            get { return (short)Bx; }
//            set { Bx = value; }
//        }
//        public int IP;

//        public LuaRegisters()
//        {
//            this.A = 0;
//            this.B = 0;
//            this.C = 0;
//            this.sBx = 0;
//            this.IP = 0;
//        }
//    }
//}
