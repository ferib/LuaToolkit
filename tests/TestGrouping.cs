using LuaToolkit.Disassembler.ControlFlowAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public class TestGrouping
    {
        [Fact]
        public void TestIfGroup()
        {
            var instructions = new List<Instruction>()
            {
                new LoadBoolInstruction(1) { A=0, B=1 },
                new TestInstruction(2) { A=0, B=0, C=0 }, // if start
                new JmpInstruction(3) { sBx=2 },
                new LoadBoolInstruction(4) { A=0, B=0 },
                new ReturnInstruction(5) { A=0, B=0 }, // if end
                new ReturnInstruction(6) { A=0, B=1 }
            };
            var jmp = InstructionConvertor<JmpInstruction>.Convert(instructions[2]).Value;
            jmp.Target = instructions[5];

            Function func = new Function();
            foreach(var instr in instructions)
            {
                func.AddInstruction(instr);
            }

            var ifMatcher = new IfMatcher();
            var condition = instructions[1];
            Assert.True(ifMatcher.MatchBegin(condition)); // If starts at test instruction
            var end = ifMatcher.FindEnd(condition, instructions);
            Assert.Equal(instructions[4], end);
            var range = instructions.GetRange(
                        1, 5 - 1 + 1);
            Assert.True(ifMatcher.Match(condition, end, range));
            var instrGroup = ifMatcher.GenerateGroup(condition, end, range);
            var ifGroupOrErr = GroupConvertor<IfGroup>.Convert(instrGroup);
            Assert.False(ifGroupOrErr.HasError());
            var ifGroup = ifGroupOrErr.Value;
            Assert.Contains(instructions[3], ifGroup.Instructions);
            Assert.Contains(instructions[1], ifGroup.Condition.Instructions);
        }

        [Fact]
        public void TestIfOrGroup()
        {
            var instructions = new List<Instruction>()
            {
                new LoadBoolInstruction(1) { A=0, B=1 },
                new LoadBoolInstruction(2) { A=1, B=0 },
                new TestInstruction(3) { A=0, B=0, C=0 }, // if start
                new JmpInstruction(4) { sBx=2 },
                new TestInstruction(5) { A=1, B=0, C=0 },
                new JmpInstruction(6) { sBx=2 },
                new LoadBoolInstruction(7) { A=0, B=0 },
                new ReturnInstruction(8) { A=0, B=0 }, // if end
                new ReturnInstruction(9) { A=0, B=1 }
            };
            var jmp = InstructionConvertor<JmpInstruction>.Convert(instructions[3]).Value;
            jmp.Target = instructions[8];
            var jmp2 = InstructionConvertor<JmpInstruction>.Convert(instructions[5]).Value;
            jmp2.Target = instructions[8];

            Function func = new Function();
            foreach (var instr in instructions)
            {
                func.AddInstruction(instr);
            }

            var ifMatcher = new IfMatcher();
            var condition = instructions[2];
            Assert.True(ifMatcher.MatchBegin(condition)); // If starts at test instruction
            var end = ifMatcher.FindEnd(condition, instructions);
            Assert.Equal(instructions[7], end);
            var range = InstructionUtil.GetRange(condition, end, instructions);
            Assert.True(ifMatcher.Match(condition, end, range));
            var instrGroup = ifMatcher.GenerateGroup(condition, end, range);
            var ifGroupOrErr = GroupConvertor<IfGroup>.Convert(instrGroup);
            Assert.False(ifGroupOrErr.HasError());
            var ifGroup = ifGroupOrErr.Value;
            Assert.Contains(instructions[6], ifGroup.Instructions);
            Assert.Contains(instructions[2], ifGroup.Condition.Instructions);
            Assert.Contains(instructions[4], ifGroup.Condition.Instructions);
        }

        [Fact]
        public void TestAndConditionGroup()
        {
            var instructions = new List<Instruction>()
            {
                new TestInstruction(1) { A=0, B=0, C=0 }, // begin
                new JmpInstruction(2) { sBx=5 },
                new TestInstruction(3) { A=0, B=0, C=0 },
                new JmpInstruction(4) { sBx=3 },
                new TestInstruction(5) { A=0, B=0, C=0 },
                new JmpInstruction(6) { sBx=1 }, // end
                new ReturnInstruction(7) { A=0, B=1 },
                new ReturnInstruction(8) { A=0, B=1 }
            };

            Function func = new Function();
            foreach (var instr in instructions)
            {
                func.AddInstruction(instr);
            }
        }

        [Fact]
        public void TestAndConditionGroupReverse()
        {
            var instructions = new List<Instruction>()
            {
                new LoadBoolInstruction(1) {A=0, B=0, C=0 },
                new TestInstruction(2) { A=0, B=0, C=0 }, // begin
                new JmpInstruction(3) { sBx=5 },
                new TestInstruction(4) { A=0, B=0, C=0 },
                new JmpInstruction(5) { sBx=3 },
                new TestInstruction(6) { A=0, B=0, C=0 },
                new JmpInstruction(7) { sBx=1 }, // end
                new ReturnInstruction(8) { A=0, B=1 },
                new ReturnInstruction(9) { A=0, B=1 }
            };

            Function func = new Function();
            foreach (var instr in instructions)
            {
                func.AddInstruction(instr);
            }

            var matcher = new ConditionMatcher();
            var end = instructions[6];
            Assert.True(matcher.MatchEnd(end));
            var begin = instructions[1];
            Assert.Equal(begin,
                matcher.FindBegin(end, instructions));
            Assert.True(matcher.Match(begin, end,
                InstructionUtil.GetRange(begin, end, instructions))) ;
            matcher.GenerateGroup(begin, end,
                InstructionUtil.GetRange(begin, end, instructions));
        }

        [Fact]
        public void TestOrConditionGroup()
        {
            var instructions = new List<Instruction>()
            {
                new TestInstruction(1) { A=0, B=0, C=0 }, // begin
                new JmpInstruction(2) { sBx=4 },
                new TestInstruction(3) { A=0, B=0, C=0 },
                new JmpInstruction(4) { sBx=2 },
                new TestInstruction(5) { A=0, B=0, C=0 },
                new JmpInstruction(6) { sBx=1 }, // end
                new ReturnInstruction(7) { A=0, B=1 },
                new ReturnInstruction(8) { A=0, B=1 }
            };

            Function func = new Function();
            foreach (var instr in instructions)
            {
                func.AddInstruction(instr);
            }

            var matcher = new ConditionMatcher();
            Assert.True(matcher.MatchBegin(instructions[0]));
            Assert.Equal(instructions[5],
                matcher.FindEnd(instructions[0], instructions));
            Assert.True(matcher.Match(instructions[0], instructions[5], 
                instructions.GetRange(0, 6)));
            matcher.GenerateGroup(instructions[0], instructions[5],
                instructions.GetRange(0, 6));
        }

        [Fact]
        public void TestWhileGroup()
        {
            var instructions = new List<Instruction>()
            {
                new LoadBoolInstruction(1)            { A = 0, B = 1, C = 0},  // local var0 = true
                new TestInstruction(2)                { A = 0, B = 0, C = 0 }, // if var0
                new JmpInstruction(3)                 { sBx = 2 },             // JMP out of loop
                new LoadBoolInstruction(4)            { A = 0, B = 0, C = 0 }, // var0 = false
                new JmpInstruction(5)                 { sBx = -4 },            // while end 
                new ReturnInstruction(6)              { A=0, B=1 },       // 
            };

            var jmp = InstructionConvertor<JmpInstruction>.Convert(instructions[2]).Value;
            jmp.Target = instructions[5];
            instructions[5].Branchers.Add(jmp);
            var jmp2 = InstructionConvertor<JmpInstruction>.Convert(instructions[4]).Value;
            jmp2.Target = instructions[1];
            instructions[1].Branchers.Add(jmp2);

            Function func = new Function();
            foreach (var instr in instructions)
            {
                func.AddInstruction(instr);
            }

            var matcher = new WhileMatcher();
            var begin = instructions[1];
            Assert.True(matcher.MatchBegin(begin));
            var end = instructions[4];
            Assert.Equal(end, matcher.FindEnd(begin, instructions));
            Assert.True(matcher.Match(begin, end, 
                InstructionUtil.GetRange(begin, end, instructions)));
            var group = matcher.GenerateGroup(begin, end, 
                InstructionUtil.GetRange(begin, end, instructions));
            var whileGroup = GroupConvertor<WhileInstructionGroup>.Convert(group).Value;
            Assert.Contains(end, whileGroup.Jmp.Instructions);
            Assert.Contains(begin, whileGroup.Condition.Instructions);
            Assert.Contains(InstructionUtil.GetNextInstruction(begin), 
                whileGroup.Condition.Instructions);
        }

        [Fact]
        public void TestWhileAndGroup()
        {
            var instructions = new List<Instruction>()
            {
                new LoadBoolInstruction(1)            { A = 0, B = 1, C = 0},  // local var0 = true
                new LoadBoolInstruction(2)            { A = 1, B = 1, C = 0},  // local var1 = true
                new TestInstruction(3)                { A = 0, B = 0, C = 0 }, // if var0
                new JmpInstruction(4)                 { sBx = 4 },             // JMP out of loop
                new TestInstruction(5)                { A = 1, B = 0, C = 0 }, // if var1
                new JmpInstruction(6)                 { sBx = 2 },             // JMP out of loop
                new LoadBoolInstruction(7)            { A = 2, B = 0, C = 0 }, // var2 = false
                new JmpInstruction(8)                 { sBx = -6 },            // while end 
                new ReturnInstruction(9)              { A=0, B=1 },       // 
            };

            var jmp = InstructionConvertor<JmpInstruction>.Convert(instructions[3]).Value;
            jmp.Target = instructions[8];
            instructions[8].Branchers.Add(jmp);
            var jmp2 = InstructionConvertor<JmpInstruction>.Convert(instructions[5]).Value;
            jmp2.Target = instructions[8];
            instructions[8].Branchers.Add(jmp2);
            var jmp3 = InstructionConvertor<JmpInstruction>.Convert(instructions[7]).Value;
            jmp3.Target = instructions[2];
            instructions[2].Branchers.Add(jmp3);

            Function func = new Function();
            foreach (var instr in instructions)
            {
                func.AddInstruction(instr);
            }

            var matcher = new WhileMatcher();
            var begin = instructions[2];
            Assert.True(matcher.MatchBegin(begin));
            var end = instructions[7];
            Assert.Equal(end, matcher.FindEnd(begin, instructions));
            Assert.True(matcher.Match(begin, end,
                InstructionUtil.GetRange(begin, end, instructions)));
            var group = matcher.GenerateGroup(begin, end,
                InstructionUtil.GetRange(begin, end, instructions));
            var whileGroup = GroupConvertor<WhileInstructionGroup>.Convert(group).Value;
            Assert.Contains(end, whileGroup.Jmp.Instructions);
            Assert.Contains(begin, whileGroup.Condition.Instructions);
            Assert.True(InstructionUtil.GetRange(begin, instructions[5], instructions)
                .TrueForAll(whileGroup.Condition.Instructions.Contains));
        }

        [Fact]
        public void TestRepeatGroup()
        {
            var instructions = new List<Instruction>()
            {
                new LoadKInstruction(1)         { A=0, Bx=-1 },     // var0 = 0
                new LoadKInstruction(2)         { A=0, Bx=-2 },     // var1 = 10
                                                                                // repeat begin
                new AddInstruction(3)           { A=0, B=0, C=-3 }, // var0 = var0 + 1
                new EqInstruction(4)            { A=0, B=0, C=1 },  // until var0 == var1
                new JmpInstruction(5)           { sBx=-3 },          // JMP -3
                new ReturnInstruction(6)        { A=0, B=1 },       // return
            };

            var jmp = InstructionConvertor<JmpInstruction>.Convert(instructions[4]).Value;
            jmp.Target = instructions[2];
            instructions[2].Branchers.Add(jmp);

            Function func = new Function();
            foreach (var instr in instructions)
            {
                func.AddInstruction(instr);
            }

            var matcher = new RepeatMatcher();
            var begin = instructions[2];
            var end = instructions[4];
            Assert.True(matcher.MatchBegin(begin));
            Assert.Equal(end, matcher.FindEnd(begin, instructions));
            Assert.True(matcher.Match(begin, end, 
                InstructionUtil.GetRange(begin, end, instructions)));
            var group = matcher.GenerateGroup(begin, end, 
                InstructionUtil.GetRange(begin, end, instructions));
            var repeatGroup = GroupConvertor<RepeatGroup>.Convert(group).Value;
            Assert.Contains(instructions[2], repeatGroup.Instructions);
            Assert.Contains(instructions[3], repeatGroup.Condition.Instructions);
        }

        [Fact]
        public void TestRepeatAndGroup()
        {
            var instructions = new List<Instruction>()
            {
                new LoadBoolInstruction(1)         { A=0, B=0, C=0 },   // var0 = false
                new LoadBoolInstruction(2)         { A=1, B=0, C=0 },   // var1 = false
                                                                        // repeat body
                new AddInstruction(3)           { A=0, B=0, C=-3 },     // var0 = var0 + 1
                new TestInstruction(4)          { A=0, B=0, C=0 },      // condition begin
                new JmpInstruction(5)           { sBx=-3 },             // 
                new TestInstruction(6)          { A=1, B=0, C=0 },      // 
                new JmpInstruction(7)           { sBx=-5 },             // condition end
                new ReturnInstruction(8)        { A=0, B=1 },           // return
            };

            var jmp = InstructionConvertor<JmpInstruction>.Convert(instructions[4]).Value;
            jmp.Target = instructions[2];
            instructions[2].Branchers.Add(jmp);
            var jmp2 = InstructionConvertor<JmpInstruction>.Convert(instructions[6]).Value;
            jmp2.Target = instructions[2];
            instructions[2].Branchers.Add(jmp2);

            Function func = new Function();
            foreach (var instr in instructions)
            {
                func.AddInstruction(instr);
            }

            var matcher = new RepeatMatcher();
            var begin = instructions[2];
            var end = instructions[6];
            Assert.True(matcher.MatchBegin(begin));
            Assert.Equal(end, matcher.FindEnd(begin, instructions));
            Assert.True(matcher.Match(begin, end,
                InstructionUtil.GetRange(begin, end, instructions)));
            var group = matcher.GenerateGroup(begin, end,
                InstructionUtil.GetRange(begin, end, instructions));
            var repeatGroup = GroupConvertor<RepeatGroup>.Convert(group).Value;
            Assert.Contains(instructions[2], repeatGroup.Instructions);
            Assert.Contains(instructions[3], repeatGroup.Condition.Instructions);
            Assert.Contains(instructions[5], repeatGroup.Condition.Instructions);
        }
    }
}
