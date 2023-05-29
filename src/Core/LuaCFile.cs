namespace LuaToolkit
{
    public class LuaCFile
    {
        public byte Format = 0;
        public bool BigEndian = false;
        // Byte size of integers used in LuaScript.
        public byte IntSize = 4;
        // Byte size of size_t
        public byte SizeTSize = 8;
        // Byte size of instructions
        public byte InstructionSize = 4;
        // Byte size of numbers
        public byte LuaNumberSize = 8;
        // 1 == integral    
        public byte Integral = 0;

        public byte[] Buffer;
        public LuaFunction Function;

        public LuaCFile(byte[] buffer)
        {
            this.Buffer = buffer;
        }
    }
}
