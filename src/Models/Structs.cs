namespace LuaToolkit.Models
{
    public struct LuaInstructionOLD
    {
        public LuaOpcode Opcode;
        public OpcodeType Type; // ABC, ABx, AsBx
        public int A;
        public int B;
        public int Bx;
        public int C;
        public int sBx;
    }
}
