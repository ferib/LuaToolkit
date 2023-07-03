using LuaToolkit.Ast;
using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace LuaToolkit.Disassembler.ControlFlowAnalysis
{
    public abstract class InstructionPatternMatcher
    {
        // If the matcher saves information that has to be reset.
        // Called before every run.
        public virtual void Reset() { }
        public abstract bool Match(Instruction first, Instruction end, 
            List<Instruction> instructions);
        public abstract bool MatchBegin(Instruction instruction);
        public abstract bool MatchEnd(Instruction instruction);
        public abstract Instruction FindEnd(Instruction first, List<Instruction> instructions);
        public abstract InstructionGroup GenerateGroup(Instruction first, Instruction end, List<Instruction> instructions);

    }

    public class WhileMatcher : InstructionPatternMatcher
    {
        public WhileMatcher() { 
            ConditionMatcher = new ConditionMatcher();
        }

        public override void Reset()
        {
            base.Reset();
            ConditionMatcher.Reset();
            ConditionEnd = null;
        }
        // Pattern:
        // 1 Condition (TEST/LT/LE/EQ)
        // 2 JMP 5
        // 3 ... (body)
        // 4 JMP 2
        public override bool MatchBegin(Instruction instruction)
        {
            // While always starts with an if and a JMP to the if.
            return ConditionMatcher.MatchBegin(instruction) &&
                instruction.Branchers.Count == 1;
        }

        public override bool MatchEnd(Instruction instruction)
        {
            // The last instruction is the one that jumps to the first.
            return instruction.OpCode == LuaOpcode.JMP && 
                InstructionUtil.IsCondition(InstructionConvertor<JmpInstruction>.Convert(instruction).Value.Target);
        }
        public override bool Match(Instruction first, Instruction end, 
            List<Instruction> instructions)
        {
            Debug.Assert(MatchBegin(instructions[0]), 
                "The first instruction of the list should be the beginning of the group");
            Debug.Assert(MatchEnd(instructions.Last()),
                "The last instruction of the list should be the last of the group");

            if(!ConditionMatcher.Match(first, ConditionEnd, 
                InstructionUtil.GetRange(first, ConditionEnd, instructions)))
            {
                return false;
            }
            if(instructions.Count < 3)
            {
                return false;
            }
            var jmpInstruction = InstructionUtil.GetNextInstruction(first);
            if(jmpInstruction.OpCode != LuaOpcode.JMP)
            {
                // The condition is always followed by a test
                return false;
            }
            return true;
        }

        public override Instruction FindEnd(Instruction first, List<Instruction> instructions)
        {
            ConditionEnd = ConditionMatcher.FindEnd(first, instructions);
            Debug.Assert(ConditionEnd != null, "Failed to find Condition");
            Debug.Assert(first.Branchers.Count == 1,
                "There has to be an instruction that jumps to the first");
            return first.Branchers[0];
        }

        public override InstructionGroup GenerateGroup(Instruction first, Instruction end, List<Instruction> instructions)
        {
            var conditionGroup = ConditionMatcher.GenerateGroup(first, ConditionEnd, 
                InstructionUtil.GetRange(first, ConditionEnd, instructions));
            var jmpGroup = new InstructionGroup(new List<Instruction>() { end });
            var body = new List<Instruction>();
            var bodyBegin = InstructionUtil.GetNextInstruction(ConditionEnd);
            var bodyEnd = InstructionUtil.GetPreviousInstruction(end);
            body.AddRange(InstructionUtil.GetRange(bodyBegin, bodyEnd, instructions));
            return new WhileInstructionGroup(conditionGroup, jmpGroup, body);
        }

        Instruction ConditionEnd;
        ConditionMatcher ConditionMatcher;
    }

    public class ForLoopMatcher : InstructionPatternMatcher
    {
        public override Instruction FindEnd(Instruction first, List<Instruction> instructions)
        {
            var forPrepOrErr = InstructionConvertor<ForPrepInstruction>.Convert(first);
            if(forPrepOrErr.HasError())
            {
                Debug.Assert(false, "First is always a ForPrep");
                return null;
            }
            return forPrepOrErr.Value.ForLoop;
        }

        public override InstructionGroup GenerateGroup(Instruction first, Instruction end, List<Instruction> instructions)
        {
            var forPrepGroup = new InstructionGroup(new List<Instruction> { first });
            var forLoopGroup = new InstructionGroup(new List<Instruction> { end });
            var body = new List<Instruction>();
            // +1 to skip the first instruction
            // -2 to exclude first and last instruction.
            body.AddRange(instructions.GetRange(instructions.IndexOf(first) + 1, instructions.Count - 2));
            return new ForLoopGroup(forPrepGroup, forLoopGroup, body);
        }

        public override bool Match(Instruction first, Instruction end, List<Instruction> instructions)
        {
            return first.OpCode == LuaOpcode.FORPREP && end.OpCode == LuaOpcode.FORLOOP;
        }

        public override bool MatchBegin(Instruction instruction)
        {
            return instruction.OpCode == LuaOpcode.FORPREP; 
        }

        public override bool MatchEnd(Instruction instruction)
        {
            return instruction.OpCode == LuaOpcode.FORLOOP;
        }
    }

    public class TForLoopMatcher : InstructionPatternMatcher
    {
        // Matches the pattern of a TFORLOOP
        // 1 CALL (ForController Function)
        // 2 JMP  Jumps to TFORLOOP instruction
        // 3 ...  body
        // 4 TFORLOOP 
        // 5 JMP  3 Jumps back to body
        public override Instruction FindEnd(Instruction first, List<Instruction> instructions)
        {
            var call = InstructionConvertor<CallInstruction>.Convert(first).Value;
            var jmpInstr = InstructionUtil.GetNextInstruction(call);
            var jmpOrErr = InstructionConvertor<JmpInstruction>.Convert(jmpInstr);
            if(jmpOrErr.HasError())
            {
                return null;
            }
            var jmp = jmpOrErr.Value;
            var tForLoop = jmp.Target;
            if(tForLoop.OpCode != LuaOpcode.TFORLOOP )
            {
                return null;
            }
            var tForIndex = instructions.IndexOf(tForLoop);
            var nextjmp = instructions[tForIndex + 1];
            return nextjmp.OpCode == LuaOpcode.JMP ? nextjmp : null;
        }

        public override InstructionGroup GenerateGroup(Instruction first, Instruction end, List<Instruction> instructions)
        {
            var callIndex = instructions.IndexOf(first);
            var firstJmpInstr = instructions[callIndex + 1];
            var entryGroup = new InstructionGroup(new List<Instruction> { first, firstJmpInstr });
            var nextJmpIndex = instructions.IndexOf(end);
            var tForLoop = instructions[nextJmpIndex - 1];
            var tForLoopGroup = new InstructionGroup(new List<Instruction> { tForLoop, end });
            var body = new List<Instruction>();
            // +2 to skip the first 2 instruction
            // -4 to exclude first 2 and last 2 instruction.
            body.AddRange(instructions.GetRange(instructions.IndexOf(first) + 2, instructions.Count - 4));
            return new TForLoopGroup(entryGroup, tForLoopGroup, body);
        }

        public override bool Match(Instruction first, Instruction end, List<Instruction> instructions)
        {
            // There are always 2 jumps and an TFORLOOP
            if(instructions.Count < 3)
            {
                return false;
            }
            if(first.OpCode != LuaOpcode.CALL)
            {
                return false;
            }

            var callIndex = instructions.IndexOf(first);
            var jmp = instructions[callIndex + 1];
            if(jmp.OpCode != LuaOpcode.JMP)
            {
                return false;
            }

            var nextJmpIndex = instructions.IndexOf(end);
            var tForLoop = instructions[nextJmpIndex - 1];
            
            // TForLoop instruction is always followed by a Jump.
            return tForLoop.OpCode == LuaOpcode.TFORLOOP;
        }

        public override bool MatchBegin(Instruction instruction)
        {
            // TODO If there is no Control Function the first instr would be a loadnil
            return instruction.OpCode == LuaOpcode.CALL;

            // return instruction.OpCode == LuaOpcode.LOADNIL;
        }

        public override bool MatchEnd(Instruction instruction)
        {
            return instruction.OpCode == LuaOpcode.JMP;
        }
    }

    public class RepeatMatcher : InstructionPatternMatcher
    {
        public RepeatMatcher()
        {
            ConditionMatcher = new ConditionMatcher();
        }

        public override void Reset()
        {
            base.Reset();
            ConditionMatcher.Reset();
        }

        public override Instruction FindEnd(Instruction first, List<Instruction> instructions)
        {
            Instruction last = null;
            // Search for the last instruction that branches to this one.
            foreach(var instr in first.Branchers)
            {
                if (last == null)
                {
                    last = instr;
                    continue;
                }
                if(last.LineNumber < instr.LineNumber)
                {
                    last = instr;
                }
            }
            if(last.LineNumber < first.LineNumber)
            {
                return null;
            }
            return last;
        }

        public override InstructionGroup GenerateGroup(Instruction first, Instruction end, List<Instruction> instructions)
        {
            var entryGroup = new InstructionGroup(new List<Instruction> { first });

            var conditionBegin = ConditionMatcher.FindBegin(end, instructions);
            var conditionGroup = ConditionMatcher.GenerateGroup(conditionBegin, end,
                InstructionUtil.GetRange(conditionBegin, end, instructions));
            var bodyEnd = InstructionUtil.GetPreviousInstruction(conditionBegin);
            var body = InstructionUtil.GetRange(first, bodyEnd, instructions);
            return new RepeatGroup(entryGroup, conditionGroup, body);
        }

        public override bool Match(Instruction first, Instruction end, List<Instruction> instructions)
        {
            if (instructions.Count < 3)
            {
                return false;
            }
            var condBegin = ConditionMatcher.FindBegin(end, instructions);

            return ConditionMatcher.Match(condBegin, end, 
                InstructionUtil.GetRange(condBegin, end, instructions));
        }

        public override bool MatchBegin(Instruction instruction)
        {
            // Verify if this can be more than 1.
            return instruction.Branchers.Count >= 1;
        }

        public override bool MatchEnd(Instruction instruction)
        {
            return instruction.OpCode == LuaOpcode.JMP;
        }

        ConditionMatcher ConditionMatcher;
    }

    public class IfMatcher : InstructionPatternMatcher
    {
        // if group
        // 1 TEST (condition)
        // 2 JMP 4  jump over body
        // 3 ... (body)
        // 4 ... (next block)
        public IfMatcher()
        {
            ConditionMatcher = new ConditionMatcher();
        }

        public override void Reset()
        {
            base.Reset();
            ConditionMatcher.Reset();
        }
        public override Instruction FindEnd(Instruction first, List<Instruction> instructions)
        {
            if(instructions.Count < 3)
            {
                return null;
            }
            var jmpInstr = ConditionMatcher.FindEnd(first, instructions);
            // var jmpInstr = InstructionUtil.GetNextInstruction(first);
            var jmpOrErr = InstructionConvertor<JmpInstruction>.Convert(jmpInstr);
            if(jmpOrErr.HasError())
            {
                return null;
            }
            var target = jmpOrErr.Value.Target;
            var bodyEnd = InstructionUtil.GetPreviousInstruction(target);
            Debug.Assert(instructions.Contains(bodyEnd));
            // Debug.Assert(bodyEnd != jmpOrErr.Value, "Emtpy if statement, just dont");
            return bodyEnd;
        }

        public override InstructionGroup GenerateGroup(Instruction first, Instruction end, List<Instruction> instructions)
        {
            var conditionEnd = ConditionMatcher.FindEnd(first, instructions);
            var conditionGroup = ConditionMatcher.GenerateGroup(first, conditionEnd,
                InstructionUtil.GetRange(first, conditionEnd, instructions));
            var lastJump = GroupConvertor<ConditionGroup>.Convert(conditionGroup).Value.FinalJump;
            var jmpGroup = new InstructionGroup(new List<Instruction> { end });

            var body = new List<Instruction>();
            // If we jump nowhere that means we have an empty if, it is stupid, but can happen.
            if (lastJump.sBx == 0)
            {
                return new IfGroup(conditionGroup, jmpGroup, body);
            }

            // Skip the condition, everything else should be part of the body.
            var beginBody = InstructionUtil.GetNextInstruction(lastJump);
            var endBody = end;
            if (end.OpCode != LuaOpcode.JMP)
            {
                body.AddRange(InstructionUtil.GetRange(beginBody, endBody, instructions));
                return new IfGroup(conditionGroup, body);
            }
            endBody = InstructionUtil.GetPreviousInstruction(endBody);
            body.AddRange(InstructionUtil.GetRange(beginBody, endBody, instructions));
            return new IfGroup(conditionGroup, jmpGroup, body);
        }

        public override bool Match(Instruction first, Instruction end, List<Instruction> instructions)
        {
            var conditionEnd = ConditionMatcher.FindEnd(first, instructions);
            return ConditionMatcher.Match(first, conditionEnd, 
                InstructionUtil.GetRange(first, conditionEnd, instructions));

        }

        public override bool MatchBegin(Instruction instruction)
        {
            return ConditionMatcher.MatchBegin(instruction);
        }

        public override bool MatchEnd(Instruction instruction)
        {
            // The end of an if is a normal instruciton, nothing to match.
            return true;
        }

        ConditionMatcher ConditionMatcher;
    }

    public class IfChainMatcher : InstructionPatternMatcher
    {
        public IfChainMatcher()
        {
            IfMatcher = new IfMatcher();
            IfBeginEndCache = new List<IfBeginEndPair>();
        }

        struct IfBeginEndPair
        {
            public IfBeginEndPair(Instruction begin, Instruction end)
            {
                Begin = begin;
                End = end;
            }
            public Instruction Begin;
            public Instruction End;
        }

        public override void Reset()
        {
            base.Reset();
            IfMatcher.Reset();
            IfChainCount = 0;
            HasElse = false;
            IfBeginEndCache.Clear();
        }
        public override Instruction FindEnd(Instruction first, List<Instruction> instructions)
        {
            var ifEnd = IfMatcher.FindEnd(first, instructions);
            if(ifEnd == null)
            {
                return null;
            }
            IfBeginEndCache.Add(new IfBeginEndPair(first, ifEnd));
            IfChainCount = 1;
            
            // if ifEnd is a jump
            // Check if there is a chain
            var next = InstructionUtil.GetNextInstruction(ifEnd);
            while (instructions.Contains(next) && InstructionUtil.IsCondition(next))
            {
                ifEnd = IfMatcher.FindEnd(next, instructions);
                if(ifEnd == null)
                {
                    break;
                }
                IfBeginEndCache.Add(new IfBeginEndPair(next, ifEnd));
                next = InstructionUtil.GetNextInstruction(ifEnd);
                ++IfChainCount;
            }
            // if ifEnd is a jmp, there is an else, otherwise not
            var jmpOrErr = InstructionConvertor<JmpInstruction>.Convert(ifEnd);
            if(jmpOrErr.HasError())
            {
                // There is no else, the end is
                return ifEnd;
            }
            // There is an else, find the end of else.
            HasElse = true;
            var jmp = jmpOrErr.Value;
            var end = jmp.Target;
            Debug.Assert(instructions.Contains(end), "Defuck");
            // Previous instruction is end of else
            return InstructionUtil.GetPreviousInstruction(end);
        }

        public override InstructionGroup GenerateGroup(Instruction first, Instruction end, List<Instruction> instructions)
        {
            List<InstructionGroup> ifGroups = new List<InstructionGroup>();
            foreach(var pair in IfBeginEndCache)
            {
                ifGroups.Add(IfMatcher.GenerateGroup(pair.Begin, pair.End, instructions));
            }
            if(!HasElse)
            {
                return new IfChainGroup(ifGroups);
            }
            // Generate else group.
            var lastIf = IfBeginEndCache.Last();
            var jmpOrErr = InstructionConvertor<JmpInstruction>.Convert(lastIf.End);
            Debug.Assert(!jmpOrErr.HasError(), "Defuck again");
            var jmpIndex = instructions.IndexOf(jmpOrErr.Value);
            var elseIndex = jmpIndex + 1;
            var elseEndIndex = instructions.IndexOf(end);
            var elseGroup = new InstructionGroup(instructions.GetRange(elseIndex, elseEndIndex - elseIndex + 1));
            return new IfChainGroup(ifGroups, elseGroup);
        }

        public override bool Match(Instruction first, Instruction end, List<Instruction> instructions)
        {
            // If we find an end, everything should be good.
            return IfBeginEndCache.Count > 0;
        }

        public override bool MatchBegin(Instruction instruction)
        {
            return IfMatcher.MatchBegin(instruction);
        }

        public override bool MatchEnd(Instruction instruction)
        {
            return IfMatcher.MatchEnd(instruction);
        }

        IfMatcher IfMatcher;
        int IfChainCount = 0;
        bool HasElse = false;
        List<IfBeginEndPair> IfBeginEndCache;
    }

    public class ConditionMatcher : InstructionPatternMatcher
    {
        public override Instruction FindEnd(Instruction first, List<Instruction> instructions)
        {
            var begin = first;
            var end = InstructionUtil.GetNextInstruction(begin);
            if(end.OpCode != LuaOpcode.JMP)
            {
                return null;
            }
            var lastValidEnd = end;
            while (InstructionUtil.IsCondition(begin))
            {
                lastValidEnd = end;
                
                if(end.OpCode != LuaOpcode.JMP)
                {
                    break;
                }
                begin = InstructionUtil.GetNextInstruction(end);
                // It is possible that there is a Get Global inside the condition chain.
                if (begin.OpCode == LuaOpcode.GETGLOBAL)
                {
                    begin = InstructionUtil.GetNextInstruction(begin);
                }
                end = InstructionUtil.GetNextInstruction(begin);
            }
            return lastValidEnd;
        }

        public Instruction FindBegin(Instruction last, List<Instruction> instructions)
        {
            var begin = InstructionUtil.GetPreviousInstruction(last);
            var end = last;
            if (end.OpCode != LuaOpcode.JMP)
            {
                return null;
            }
            var lastValidBegin = begin;
            while (InstructionUtil.IsCondition(begin))
            {
                lastValidBegin = begin;

                if (end.OpCode != LuaOpcode.JMP)
                {
                    break;
                }
                begin = InstructionUtil.GetPreviousInstruction(end);
                // It is possible that there is a Get Global inside the condition chain.
                if (begin.OpCode == LuaOpcode.GETGLOBAL)
                {
                    begin = InstructionUtil.GetPreviousInstruction(begin);
                }
                end = InstructionUtil.GetPreviousInstruction(begin);
            }
            return lastValidBegin;
        }

        public override InstructionGroup GenerateGroup(Instruction first, Instruction end, List<Instruction> instructions)
        {
            var jmp = InstructionConvertor<JmpInstruction>.Convert(end);
            Debug.Assert(!jmp.HasError());
            return new ConditionGroup(jmp.Value, instructions);
        }

        public override bool Match(Instruction first, Instruction end, List<Instruction> instructions)
        {
            return instructions.TrueForAll(instr => InstructionUtil.IsCondition(instr) ||
            instr.OpCode == LuaOpcode.JMP || instr.OpCode == LuaOpcode.GETGLOBAL);
        }

        public override bool MatchBegin(Instruction instruction)
        {
            return InstructionUtil.IsCondition(instruction);
        }

        public override bool MatchEnd(Instruction instruction)
        {
            // The end of an condition is the last jump.
            return instruction.OpCode == LuaOpcode.JMP;
        }
    }

    public class TestSetMatcher : InstructionPatternMatcher
    {
        // Searches for:
        // 4	TESTSET	3 0 0	
        // 5	JMP	3	to pc 9
        // 6	TESTSET	3 1 0	
        // 7	JMP	1	to pc 9
        // 8	MOVE	3 2

        // The first instruction can sometimes be a TEST
        // 4	TEST	0 0 0	
        // 5	JMP	2	to pc 8
        // 6	TESTSET	3 1 1	
        // 7	JMP	1	to pc 9
        // 8	MOVE	3 2

        // The last one can sometimes be a not
        // 4	TEST	0 0 0	
        // 5	JMP	2	to pc 8
        // 6	TESTSET	3 1 1	
        // 7	JMP	1	to pc 9
        // 8	NOT	3 2
        public override Instruction FindEnd(Instruction first, List<Instruction> instructions)
        {
            var instr = InstructionUtil.GetNextInstruction(first);
            while(instr.OpCode != LuaOpcode.MOVE && instr.OpCode != LuaOpcode.NOT)
            {
                if(instr.OpCode != LuaOpcode.TESTSET && instr.OpCode != LuaOpcode.JMP)
                {
                    return null;
                }
                if(!instructions.Contains(instr))
                {
                    return null;
                }
                instr = InstructionUtil.GetNextInstruction(instr);
            }
            return instr; 
        }

        public override InstructionGroup GenerateGroup(Instruction first, Instruction end, List<Instruction> instructions)
        {
            var conditionGroup = new InstructionGroup(instructions.GetRange(0, instructions.Count - 1));
            var endGroup = new InstructionGroup( new List<Instruction>() { end });
            return new TestSetGroup(conditionGroup, endGroup);
        }

        public override bool Match(Instruction first, Instruction end, List<Instruction> instructions)
        {
            var instr = first;
            while(instr != end)
            {
                // TESTSET is a list of TESTSET and JMP instructions ended by a move.
                if (instr.OpCode != LuaOpcode.TESTSET && instr.OpCode != LuaOpcode.JMP 
                    && instr.OpCode != LuaOpcode.MOVE && instr.OpCode != LuaOpcode.NOT && instr.OpCode != LuaOpcode.TEST)
                {
                    return false;
                }
                instr = InstructionUtil.GetNextInstruction(instr);
            }
            
            return end.OpCode == LuaOpcode.MOVE || end.OpCode == LuaOpcode.NOT;
        }

        public override bool MatchBegin(Instruction instruction)
        {
            return instruction.OpCode == LuaOpcode.TESTSET || instruction.OpCode == LuaOpcode.TEST;
        }

        public override bool MatchEnd(Instruction instruction)
        {
            return instruction.OpCode == LuaOpcode.MOVE || instruction.OpCode == LuaOpcode.NOT;
        }
    }

    public class InstructionGroupMaker
    {
        public InstructionGroupMaker()
        {
            mPatternMatchers = new List<InstructionPatternMatcher>();
            // Ordered based on how much can be matched wrong.
            mPatternMatchers.Add(new ForLoopMatcher());
            mPatternMatchers.Add(new TForLoopMatcher());
            mPatternMatchers.Add(new TestSetMatcher());
            mPatternMatchers.Add(new WhileMatcher());
            mPatternMatchers.Add(new RepeatMatcher());
            mPatternMatchers.Add(new IfChainMatcher());
        }

        public bool Run(List<Instruction> instructions, InstructionGroup parent)
        {
            var instrGroup = new InstructionGroup();
            for(int i = 0; i < instructions.Count; ++i)
            {
                var instruction = instructions[i];
                // Bool to indicate if the instruction was added to a group.
                var added = false;
                foreach(var matcher in mPatternMatchers)
                {
                    matcher.Reset();
                    // First try to find the first instruction
                    if(!matcher.MatchBegin(instruction))
                    {
                        continue;
                    }
                    // Find the end
                    var end = matcher.FindEnd(instruction, instructions);
                    if(end == null)
                    {
                        continue;
                    }
                    var firstIndex = instructions.IndexOf(instruction);
                    var lastIndex = instructions.IndexOf(end);
                    if(lastIndex <= firstIndex)
                    {
                        continue;
                    }
                    var range = instructions.GetRange(
                        firstIndex, lastIndex - firstIndex + 1);
                    if (!matcher.Match(instruction, end, range))
                    {
                        continue;
                    }
                    parent.AddChild(instrGroup);
                    instrGroup = new InstructionGroup();
                    var newGroup = matcher.GenerateGroup(instruction, end, range);
                    parent.AddChild(newGroup);
                    Run(newGroup.Instructions, newGroup);
                    
                    // If the group is an ifchain, we also have to parse those bodies
                    if(newGroup.GroupType == GroupTypes.IF_CHAIN_GROUP)
                    {
                        var ifChainGroup = GroupConvertor<IfChainGroup>.Convert(newGroup).Value;
                        foreach(var child in ifChainGroup.Childeren)
                        {
                            Run(child.Instructions, child);
                        }
                    }

                    added = true;
                    i = instructions.IndexOf(end);
                    // Break out of the matcher loop to make sure we don't find another group.
                    break;
                }
                // If the instruction is not added to a group add it to the standalone group.
                if(!added)
                {
                    instrGroup.Instructions.Add(instruction);
                }
            }
            parent.AddChild(instrGroup);
            return true;
        }

        private List<InstructionPatternMatcher> mPatternMatchers;
    }

}
