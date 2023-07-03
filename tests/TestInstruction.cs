using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public class TestInstructions
    {
        // Verifies that setting and reading the opcode works correctly.
        [Fact] 
        public void TestOpcodes()
        {
            LuaOpcode first = LuaOpcode.MOVE;
            LuaOpcode last = LuaOpcode.VARARG;
            for(LuaOpcode opcode = first; opcode != last; ++opcode)
            {
                var instruction = new Instruction(0, 0);
                instruction.OpCode = opcode;
                Assert.Equal(opcode, instruction.OpCode);
                Assert.Contains(opcode.ToString(), instruction.Dump());
            }
        }

        // Verifies that setting and reading the A register works correctly.
        [Fact]
        public void TestA()
        {
            for(int i = 0; i < Instruction.MAX_ARG_A; i++)
            {
                var instruction = new Instruction(0, 0);
                instruction.A = i;
                Assert.Equal(i, instruction.A);
                Assert.Equal(0, instruction.B);
                Assert.Equal(0, instruction.C);
            }
        }

        // Verifies that setting and reading the B register works correctly.
        [Fact]
        public void TestB()
        {
            for (int i = 0; i < Instruction.MAX_ARG_B; i++)
            {
                var instruction = new Instruction(0, 0);
                instruction.B = i;
                Assert.Equal(0, instruction.A);
                Assert.Equal(i, instruction.B);
                Assert.Equal(0, instruction.C);
            }
        }

        // Verifies that setting and reading the C register works correctly.
        [Fact]
        public void TestC()
        {
            for (int i = 0; i < Instruction.MAX_ARG_C; i++)
            {
                var instruction = new Instruction(0, 0);
                instruction.C = i;
                Assert.Equal(0, instruction.A);
                Assert.Equal(0, instruction.B);
                Assert.Equal(i, instruction.C);
            }
        }

        // Verifies that setting and reading the Bx register works correctly.
        [Fact]
        public void TestBx()
        {
            for (int i = 0; i < Instruction.MAX_ARG_Bx; i++)
            {
                var instruction = new Instruction(0, 0);
                instruction.Bx = i;
                Assert.Equal(0, instruction.A);
                Assert.Equal(i, instruction.Bx);
            }
        }

        // Verifies that setting and reading the sBx register works correctly.
        [Fact]
        public void TestsBx()
        {
            for (int i = -Instruction.MAX_ARG_sBx; i < Instruction.MAX_ARG_sBx; i++)
            {
                var instruction = new Instruction(0, 0);
                instruction.sBx = i;
                Assert.Equal(0, instruction.A);
                Assert.Equal(i, instruction.sBx);
            }
        }
    }
}
