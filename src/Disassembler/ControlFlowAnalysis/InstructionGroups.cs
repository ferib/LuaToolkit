using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Text;

namespace LuaToolkit.Disassembler.ControlFlowAnalysis
{
    public enum GroupTypes
    {
        INSTRUCTION_GROUP, WHILE_GROUP, REPEAT_GROUP, FOR_GROUP, TFOR_GROUP,
        CONDITION_GROUP, IF_GROUP, IF_CHAIN_GROUP, ELSE_GROUP
    }


    static public class GroupConvertor<T> where T : InstructionGroup
    {
        static public Expected<T> Convert(InstructionGroup group)
        {
            if (group == null)
            {
                return new Expected<T>("Cannot convert nullptr");
            }
            if (typeof(T) == group.GetType())
            {
                return group as T;
            }

            return new Expected<T>("Cannot convert " +
                group.GetType().ToString() + " to " + typeof(T).ToString());
        }
    }

    public class InstructionGroup
    {
        public InstructionGroup()
        {
            Instructions = new List<Instruction>();
            mChildren = new List<InstructionGroup>();
        }

        public InstructionGroup(List<Instruction> instructions) : this()
        {
            Instructions = instructions;
        }

        public virtual string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(Name).AppendLine(" Begin:");
            if(mChildren.Count > 0)
            {
                foreach(InstructionGroup child in mChildren)
                {
                    sb.Append(child.Dump());
                }
            } else
            {
                foreach (Instruction i in Instructions)
                {
                    sb.AppendLine(i.Dump());
                }
            }
            sb.Append(Name).AppendLine(" End");
            return sb.ToString();
        }

        public InstructionGroup Parent
        {
            get;
            set;
        }

        public void AddChild(InstructionGroup child)
        {
            mChildren.Add(child);
            child.Parent = this;
        }

        public List<InstructionGroup> Childeren
        {
            get
            {
                return mChildren;
            }
            private set { }
        }

        private List<InstructionGroup> mChildren;
        public List<Instruction> Instructions;
        public string Name = "Instruction Group";
        public GroupTypes GroupType = GroupTypes.INSTRUCTION_GROUP;
    }

    public class WhileInstructionGroup : InstructionGroup
    {
        public WhileInstructionGroup(InstructionGroup condition, 
            InstructionGroup jmpInstruction) : base()
        {
            Condition = condition;
            Condition.Parent = this;
            Condition.Name = "Condition Group";
            Condition.GroupType = GroupTypes.CONDITION_GROUP;
            Jmp = jmpInstruction;
            Jmp.Parent = this;
            Jmp.Name = "Jmp Group";
            Name = "Body Group";
            GroupType = GroupTypes.WHILE_GROUP;
        }

        public WhileInstructionGroup(InstructionGroup condition,
            InstructionGroup jmpInstruction,
            List<Instruction> instructions) : this(condition, jmpInstruction) 
        {
            Instructions = instructions;
        }

        public override string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("While Group Begin: ");
            sb.Append(Condition.Dump());
            sb.Append(base.Dump());
            sb.Append(Jmp.Dump());
            sb.AppendLine("While Group End");
            return sb.ToString();
        }

        public InstructionGroup Condition;
        public InstructionGroup Jmp;
    }

    public class ForLoopGroup : InstructionGroup
    {
        public ForLoopGroup(InstructionGroup forPrep,
                InstructionGroup forLoop) : base()
        {
            ForPrep = forPrep;
            ForPrep.Parent = this;
            ForPrep.Name = "ForPrep Group";
            ForLoop = forLoop;
            ForLoop.Parent = this;
            ForLoop.Name = "ForLoop Group";
            Name = "Body Group";
            GroupType = GroupTypes.FOR_GROUP;
        }

        public ForLoopGroup(InstructionGroup forPrep,
            InstructionGroup forLoop,
            List<Instruction> instructions) : this(forPrep, forLoop)
        {
            Instructions = instructions;
        }

        public override string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("For Group Begin: ");
            sb.Append(ForPrep.Dump());
            sb.Append(base.Dump());
            sb.Append(ForLoop.Dump());
            sb.AppendLine("For Group End");
            return sb.ToString();
        }

        public InstructionGroup ForPrep;
        public InstructionGroup ForLoop;
    }

    public class TForLoopGroup : InstructionGroup
    {
        public TForLoopGroup(InstructionGroup entry,
                InstructionGroup tForLoop) : base()
        {
            Entry = entry;
            Entry.Parent = this;
            Entry.Name = "Entry Group";
            TForLoop = tForLoop;
            TForLoop.Parent = this;
            TForLoop.Name = "TForLoop Group";
            Name = "Body Group";
            GroupType = GroupTypes.TFOR_GROUP;
        }

        public TForLoopGroup(InstructionGroup entry,
                InstructionGroup tForLoop,
            List<Instruction> instructions) : this(entry, tForLoop)
        {
            Instructions = instructions;
        }

        public override string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("TFor Group Begin: ");
            sb.Append(Entry.Dump());
            sb.Append(base.Dump());
            sb.Append(TForLoop.Dump());
            sb.AppendLine("TFor Group End");
            return sb.ToString();
        }

        public InstructionGroup Entry;
        public InstructionGroup TForLoop;
    }

    public class RepeatGroup : InstructionGroup
    {
        public RepeatGroup(InstructionGroup entry, InstructionGroup condition) : base()
        {
            Entry = entry;
            Entry.Parent = this;
            Entry.Name = "Entry Group";
            Condition = condition;
            Condition.Parent = this;
            Condition.Name = "Condition Group";
            Condition.GroupType = GroupTypes.CONDITION_GROUP;
            Name = "Body Group";
            GroupType = GroupTypes.REPEAT_GROUP;
        }

        public RepeatGroup(InstructionGroup entry, InstructionGroup condition,
            List<Instruction> instructions) : this(entry, condition)
        {
            Instructions = instructions;
        }

        public override string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Repeat Group Begin: ");
            sb.Append(Entry.Dump());
            sb.Append(base.Dump());
            sb.Append(Condition.Dump());
            sb.AppendLine("Repeat Group End");
            return sb.ToString();
        }

        public InstructionGroup Entry;
        public InstructionGroup Condition;
    }

    public class IfGroup : InstructionGroup
    {
        // if group
        // 1 TEST (condition)
        // 2 JMP 4  jump over body
        // 3 ... (body)
        // 4 ... (next block)
        public IfGroup(InstructionGroup condition) : base()
        {
            Condition = condition;
            Condition.Parent = this;
            Condition.Name = "Condition Group";
            Condition.GroupType = GroupTypes.CONDITION_GROUP;
            Name = "Body Group";
            GroupType = GroupTypes.IF_GROUP;
        }

        public IfGroup(InstructionGroup condition, InstructionGroup jmp) : this(condition)
        {
            Jmp = jmp;
            Jmp.Name = "Jump Group";
            Jmp.Parent = this;
        }

        public IfGroup(InstructionGroup condition,
            List<Instruction> instructions) : this(condition)
        {
            Instructions = instructions;
        }

        public IfGroup(InstructionGroup condition, InstructionGroup jmp,
            List<Instruction> instructions) : this(condition, jmp)
        {
            Instructions = instructions;
        }

        public override string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("If Group Begin: ");
            sb.Append(Condition.Dump());
            sb.Append(base.Dump());
            sb.AppendLine("If Group End");
            return sb.ToString();
        }

        // if-else group
        // 1 TEST (condition)
        // 2 JMP 5 jump to else
        // 3 ...  (body)
        // 4 JMP 6 jump over else
        // 5 ...  (else body)
        // 6 ...  (next block)

        // if-elseif
        // 1 TEST (if condition)
        // 2 JMP 5  jump to elseif
        // 3 ...  (if-body)
        // 4 JMP 8 jump to end
        // 5 TEST (else if condition)
        // 6 JMP 8 jump to end
        // 7 ...  (else if body)
        // 8 ...  next block

        // if-elseif-else
        // 1 TEST (if condition)
        // 2 JMP 5  jump to elseif
        // 3 ...  (if-body)
        // 4 JMP 10 jump to end
        // 5 TEST (else if condition)
        // 6 JMP 9 jump to else
        // 7 ...  (else if body)
        // 8 JMP 10 jump over else
        // 9 ...  (else body)
        // 10 ...  next block

        public InstructionGroup Condition;
        public InstructionGroup Jmp;
    }

    public class IfChainGroup : InstructionGroup
    {
        public IfChainGroup(List<InstructionGroup> ifChain) : base()
        {
            int index = 1;
            foreach(InstructionGroup ifGroup in ifChain)
            {
                Childeren.Add(ifGroup);
                ifGroup.Parent = this;
                ifGroup.Name = "If Group_" + index;
                ++index;
            }
            Name = "If Chain Group";
            GroupType = GroupTypes.IF_CHAIN_GROUP;
        }

        public IfChainGroup(List<InstructionGroup> ifChain, 
            InstructionGroup elseGroup) : this(ifChain)
        {
            ElseGroup = elseGroup;
            ElseGroup.Name = "Else Group";
            ElseGroup.Parent = this;
        }

        public override string Dump()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("IfChain Begin: ");
            sb.Append(base.Dump());
            if (ElseGroup != null)
            {
                sb.Append(ElseGroup.Dump());
            }
            sb.AppendLine("IfChain End");
            return sb.ToString();
        }


        public InstructionGroup ElseGroup;
    }
}
