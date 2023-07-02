using LuaToolkit.Decompiler;
using LuaToolkit.Disassembler;
using LuaToolkit.Disassembler.ControlFlowAnalysis;
using LuaToolkit.Util;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;

namespace LuaToolkit.Ast
{
    public class StatementCache
    {
        public StatementList Get(Block block)
        {
            if (cache.TryGetValue(block, out var result))
            {
                return result;
            }
            return null;
        }

        public void Add(Block block, StatementList list)
        {
            if (cache.ContainsKey(block))
            {
                Debug.Assert(false, "Block is already in cache");
            }
            cache[block] = list;
        }

        Dictionary<Block, StatementList> cache =
            new Dictionary<Block, StatementList>();
    }

    public class VariableCache
    {
        public Variable Get(int index)
        {
            if (cache.TryGetValue(index, out var result))
            {
                return result;
            }
            return null;
        }

        public void Add(int index, Variable var)
        {
            if (cache.ContainsKey(index))
            {
                Debug.Assert(false, "Block is already in cache");
            }
            cache[index] = var;
        }

        Dictionary<int, Variable> cache =
            new Dictionary<int, Variable>();
    }

    public class ASTParser
    {
        static public StatementCache Cache = new StatementCache();

        static public VariableCache VariableCache = new VariableCache();

        static public FunctionDefinitionStatement Parse(Function function, InstructionGroup root)
        {
            var statements = new StatementList();
            foreach(var group in root.Childeren) {
                statements.Add(Parse(group));
            }
            return new FunctionDefinitionStatement(function.Name, statements);
        }

        static public Statement Parse(InstructionGroup group)
        {
            switch (group.GroupType)
            {
                case GroupTypes.INSTRUCTION_GROUP:
                    var statements = new StatementList();
                    if (group.Childeren.Count == 0)
                    {
                        foreach(var instruction in group.Instructions) {
                            statements.Add(Parse(instruction));
                        }
                    } else
                    {
                        foreach(var child in group.Childeren)
                        {
                            statements.Add(Parse(child));
                        }
                    }
                    return statements;
                case GroupTypes.WHILE_GROUP:
                    return Parse(GroupConvertor<WhileInstructionGroup>.Convert(group).Value);
                case GroupTypes.REPEAT_GROUP:
                    return Parse(GroupConvertor<RepeatGroup>.Convert(group).Value);
                case GroupTypes.FOR_GROUP:
                    return Parse(GroupConvertor<ForLoopGroup>.Convert(group).Value);
                case GroupTypes.TFOR_GROUP:
                    return Parse(GroupConvertor<TForLoopGroup>.Convert(group).Value);
                case GroupTypes.CONDITION_GROUP:
                    Debug.Assert(false, "Condition Group should always be parsed as part of another group");
                    break;
                case GroupTypes.IF_CHAIN_GROUP:
                    return Parse(GroupConvertor<IfChainGroup>.Convert(group).Value);
                case GroupTypes.TESTSET_GROUP:
                    return Parse(GroupConvertor<TestSetGroup>.Convert(group).Value);
                default:
                    Debug.Assert(false, "Missing Group: " + group.GroupType.ToString());
                    break;
            }
            return new StatementList();
        }

        static public Expression Parse(ConditionGroup group, Instruction bodyBegin, bool andEscapes = true)
        {
            Debug.Assert(InstructionUtil.IsCondition(group.Instructions[0]),
                "The first instr chould be a condition");
            Expression Prevexpr = null;
            bool isAnd = false;
            // The instruction after the condition, should be the begin of the body
            foreach(var instr in group.Instructions)
            {
                if(instr.OpCode == LuaOpcode.JMP)
                {
                    var jmp = InstructionConvertor<JmpInstruction>.Convert(instr).Value;
                    // Normally an and will jump out of the loop, jump over the body
                    // For an or, the or will jump to the function body.
                    isAnd = jmp.Target != bodyBegin;

                    // For some loops this is reverse
                    // For a repeat until, an and will jump to the body and an or
                    // will escape it.
                    if(!andEscapes)
                    {
                        isAnd = !isAnd;
                    }
                    continue;
                }
                var expr = ParseConditionExpression(instr);
                if (Prevexpr == null)
                {
                    Prevexpr = expr;
                    continue;
                }
                if (isAnd)
                {
                    Prevexpr = new AndExpression(Prevexpr, expr);
                } 
                else
                {
                    Prevexpr = new OrExpression(Prevexpr, expr);
                }
                
            }
            return Prevexpr;
        }

        static public IfStatement Parse(IfGroup group)
        {
            var conditionGroup = GroupConvertor<ConditionGroup>.Convert(group.Condition);
            Debug.Assert(!conditionGroup.HasError());
            Expression condition = null;
            if(group.Instructions.Count == 0)
            {
                condition = Parse(conditionGroup.Value, 
                    InstructionUtil.GetNextInstruction(conditionGroup.Value.Instructions.Last()));
            } 
            else
            {
                condition = Parse(conditionGroup.Value, group.Instructions.First());
            }
            
            var body = new StatementList();
            if(group.Childeren.Count == 0)
            {
                foreach(var instr in group.Instructions)
                {
                    body.Add(Parse(instr));
                }
            } else
            {
                foreach (var child in group.Childeren)
                {
                    body.Add(Parse(child));
                }
            }
            
            return new IfStatement(condition, body);
        }

        static public Statement Parse(IfChainGroup group)
        {
            List<IfStatement> ifStatements = new List<IfStatement>();
            foreach (var child in group.Childeren)
            {
                var ifGroup = GroupConvertor<IfGroup>.Convert(child);
                if(ifGroup.HasError())
                {
                    // TODO
                    continue;
                }
                ifStatements.Add(Parse(ifGroup.Value));
            }
            if(group.ElseGroup == null)
            {
                return new ElseIfStatement(ifStatements);
            }
            var elseStatement = Parse(group.ElseGroup);
            return new ElseIfElseStatement(ifStatements, elseStatement);
        }

        static public ForStatement Parse(ForLoopGroup group)
        {
            var body = new StatementList();
            foreach(var child in group.Childeren)
            {
                body.Add(Parse(child));
            }

            // Parse ForPrep
            Debug.Assert(group.ForPrep.Instructions.Count == 1, 
                "ForPrep can only contain 1 instruction");
            var forPrepInstrOrErr = InstructionConvertor<ForPrepInstruction>.
                Convert(group.ForPrep.Instructions[0]);
            Debug.Assert(!forPrepInstrOrErr.HasError(),
                "ForPrepGroup should contain a ForPrepInstruction");

            var forPrepInstr = forPrepInstrOrErr.Value;

            
            bool init; // unused
            return new ForStatement(
                        /* Loop Variable*/CreateVariable(forPrepInstr, forPrepInstr.A + 3, out init),
                        /* Init Value */ CreateVariable(forPrepInstr, forPrepInstr.A, out init),
                        /* Limit */ CreateVariable(forPrepInstr, forPrepInstr.A + 1, out init),
                        /* Step */ CreateVariable(forPrepInstr, forPrepInstr.A + 2, out init), 
                        body);
        }

        static public TForStatement Parse(TForLoopGroup group)
        {
            var body = new StatementList();
            foreach (var child in group.Childeren)
            {
                body.Add(Parse(child));
            }
            // First part of the entry is a Call with the iterator function
            var callInstr = group.Entry.Instructions[0];
            var call = ParseCallExpr(callInstr);

            // Parse TFORLOOP
            // Maybe state and control also need to be parsed
            var tforInstr = group.TForLoop.Instructions[0];
            var tfor = InstructionConvertor<TForLoopInstruction>.Convert(tforInstr).Value;

            List<Expression> vars = new List<Expression>();
            bool init; // unused
            for(int i = tfor.A + 2; i < tfor.A + 2 + tfor.C; ++i)
            {
                vars.Add(CreateVariable(tfor, i, out init));
            }            

            return new TForStatement(vars, call, body);
        }

        static public WhileStatement Parse(WhileInstructionGroup group)
        {
            var body = new StatementList();
            Instruction firstBodyInstr = group.Instructions.First();
            foreach (var child in group.Childeren)
            {
                body.Add(Parse(child));
            }
            var conditionGroup = GroupConvertor<ConditionGroup>.Convert(group.Condition);
            Debug.Assert(!conditionGroup.HasError());
            var condition = Parse(conditionGroup.Value, firstBodyInstr);
            //var condition = ParseCondition(group.Condition);

            return new WhileStatement(condition, body);
        }

        static public RepeatStatement Parse(RepeatGroup group)
        {
            var body = new StatementList();
            // body.Add(Parse(group.Entry));
            foreach (var child in group.Childeren)
            {
                body.Add(Parse(child));
            }
            var conditionGroup = GroupConvertor<ConditionGroup>.Convert(group.Condition);
            Debug.Assert(!conditionGroup.HasError());
            var condition = Parse(conditionGroup.Value, group.Instructions.First(), false);
            return new RepeatStatement(condition, body);
        }

        static public AssignStatement Parse(TestSetGroup group)
        {
            // TODO handle not
            // Better handling for everything
            var instructions = group.Condition.Instructions;
            Expression prevExpr = null;
            var endInstr = group.End.Instructions[0];
            bool init;
            var variable = CreateVariable(endInstr, endInstr.A, out init);
            bool isAnd = false;
            foreach(var instr in instructions)
            {
                if(instr.OpCode == LuaOpcode.JMP)
                {
                    continue;
                }
                if(prevExpr == null)
                {
                    prevExpr = ParseExpression(instr, instr.B);
                    // A TestSet chain can start with a TEST, in this case we should always have an and.
                    isAnd = instr.OpCode == LuaOpcode.TEST || instr.C == 0;
                    continue;
                }
                if (isAnd)
                {
                    prevExpr = new AndExpression(prevExpr, ParseExpression(instr, instr.B));
                } 
                else
                {
                    prevExpr = new OrExpression(prevExpr, ParseExpression(instr, instr.B));
                }
                isAnd = instr.C == 0;
            }
            if (isAnd)
            {
                prevExpr = new AndExpression(prevExpr, ParseExpression(endInstr, endInstr.B));
            }
            else
            {
                prevExpr = new OrExpression(prevExpr, ParseExpression(endInstr, endInstr.B));
            }

            var assign = new AssignStatement(variable, prevExpr);
            assign.Init = init;
            return assign;
        }

        static public Expression ParseCondition(InstructionGroup group)
        {

            Debug.Assert(InstructionUtil.IsCondition(group.Instructions[0]), 
                "The first instr chould be a condition");
            return ParseConditionExpression(group.Instructions[0]);
        }

        static public FunctionDefinitionStatement Parse(Function function)
        {
            var name = function.Name;
            var statements = Parse(function.Blocks);
            return new FunctionDefinitionStatement(name, statements);
        }

        static public StatementList Parse(List<Block> blocks)
        {
            var statements = new StatementList();
            for(int i = 0; i < blocks.Count; ++i)
            {
                statements.Add(Parse(blocks[i]));
            }
            return statements;
        }

        static public StatementList Parse(Block block)
        {
            StatementList statements = Cache.Get(block);
            if(statements != null)
            {
                return statements;
            }
            statements = new StatementList();
            for (var instr = 0; instr < block.Instructions.Count; ++instr)
            {
                statements.Add(Parse(block.Instructions[instr]));
            }
            Cache.Add(block, statements);
            return statements;
        }

        static public Statement Parse(Instruction instr)
        {
            switch (instr.OpCode)
            {
                case LuaOpcode.LOADK:
                    return ParseLoad(InstructionConvertor<LoadKInstruction>.Convert(instr).Value);
                case LuaOpcode.LOADBOOL:
                    return ParseLoadBool(instr);
                case LuaOpcode.LOADNIL:
                    return ParseLoadNil(instr);
                case LuaOpcode.MOVE:
                    return ParseMove(instr);
                case LuaOpcode.GETGLOBAL:
                    return ParseGlobalGet(InstructionConvertor<GetGlobalInstruction>.Convert(instr).Value);
                case LuaOpcode.SETGLOBAL:
                    return ParseGlobalSet(InstructionConvertor<SetGlobalInstruction>.Convert(instr).Value);
                case LuaOpcode.EQ:
                case LuaOpcode.LT:
                case LuaOpcode.LE:
                case LuaOpcode.TEST:
                    Debug.Assert(false, "This should not happen for now");
                    return ParseCondition(instr);
                case LuaOpcode.TESTSET:
                    Debug.Assert(false, "This should not happen for now");
                    return ParseTestSet(instr);
                case LuaOpcode.RETURN:
                    return ParseReturn(instr);
                case LuaOpcode.JMP:
                    Debug.Assert(false, "This should not happen for now");
                    return ParseJump(instr);
                case LuaOpcode.ADD:
                case LuaOpcode.SUB:
                case LuaOpcode.MUL:
                case LuaOpcode.DIV:
                case LuaOpcode.POW:
                case LuaOpcode.MOD:
                    return ParseArithmetic(instr);
                case LuaOpcode.UNM:
                case LuaOpcode.NOT:
                case LuaOpcode.LEN:
                    return ParseSingleExprArithmetic(instr);
                case LuaOpcode.CONCAT:
                    return ParseConcat(instr);
                case LuaOpcode.CALL:
                    return ParseCall(instr);
                case LuaOpcode.TAILCALL:
                    return ParseTailCall(instr);
                case LuaOpcode.CLOSURE:
                    return ParseClosure(instr);
                case LuaOpcode.VARARG:
                    return ParseVarArg(instr);
                // Tables
                case LuaOpcode.NEWTABLE:
                    return ParseNewTable(instr);
                case LuaOpcode.SETTABLE:
                    return ParseSetTable(instr);
                case LuaOpcode.GETTABLE:
                    return ParseGetTable(instr);
                case LuaOpcode.SETLIST:
                    return ParseSetList(instr);
                // Upvalues
                case LuaOpcode.GETUPVAL:
                    return ParseGetUpval(instr);
                case LuaOpcode.SETUPVAL:
                    return ParseSetUpval(instr);
                case LuaOpcode.CLOSE:
                    return new CloseStatement();
                case LuaOpcode.SELF:
                    return ParseSelf(instr);
                default:
                    Debug.Assert(false, "Opcode '" + instr.OpCode.ToString() + "' Not supported yet.");
                    return new EmptyStatement(); ;
            }
        }



        // TODO Fix parsing of assigned value
        static public Statement ParseLoad(LoadKInstruction instr)
        {
            bool init;
            var Variable = CreateVariable(instr, instr.A, out init);
            var assign = new AssignStatement(Variable, CreateConstant(instr.Constant));
            assign.Init = init;
            return assign;
        }

        static public Statement ParseLoadNil(Instruction instr)
        {
            var statements = new StatementList();
            for (int i = instr.A; i < instr.B + 1; ++i)
            {
                statements.Add(new AssignStatement(ParseExpression(instr, i) as Variable, 
                    new Constant(TypeCreator.CreateNil())));
            }
            return statements;
        }

        static public Statement ParseLoadBool(Instruction instr)
        {
            bool init;
            var variable = CreateVariable(instr, instr.A, out init);
            var constant = new Constant(TypeCreator.CreateBool(instr.B == 1));
            var assign = new AssignStatement(variable, constant);
            assign.Init = init;
            return assign;
        }

        static public Statement ParseCondition(Instruction instr)
        {
            return new ConditionStatement(ParseConditionExpression(instr));
            // var ifBody = Parse(instr.Block.JumpsNextBlock);
            // Remove the block here, that we don't parse it twice.
            // instr.Block.ScriptFunction.Blocks.Remove(instr.Block.JumpsNextBlock);
            // var expr = 
            // return new IfStatement(expr, ifBody);
        }

        /// <summary>
        ///  TESTSET has the following patern
        ///     
        ///     LOADBOOL	0 1 0	
        ///     LOADBOOL	1 0 0	
        ///     TESTSET	    2 0 1	
        ///     JMP	
        ///     MOVE	    2 1
        ///     
        /// </summary>
        /// <param name="instr"></param>
        /// <returns></returns>
        static public AssignStatement ParseTestSet(Instruction instr)
        {
            // Probably does not work correctly, I think this can be chained.
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("TestSet not assigning to var: " + varOrErr.GetError());
                Debug.Assert(false);
            }

            var lhs = ParseExpression(instr, instr.B);
            var instrIndex = instr.Block.Instructions.IndexOf(instr);
            // Test SET should always be followed by a JUMP
            // We don't need it to decompile, we will remove it.
            var jmpinstr = instr.Block.Instructions[instrIndex+1];
            Debug.Assert(jmpinstr.OpCode == LuaOpcode.JMP);

            // After the jump is a move which is the assignment of the second var.
            //var moveBlock = instr.Block.JumpsNextBlock;
            // TODO
            Block moveBlock = null;
            Debug.Assert(moveBlock.Instructions.Count == 1, "There should only by 1 instruction in this move block (the move)");
            var moveinstr = moveBlock.Instructions[0];
            
            Debug.Assert(moveinstr.OpCode == LuaOpcode.MOVE);

            var rhs = ParseExpression(moveinstr, moveinstr.B);

            // Delete everything that we don't need.
            instr.Block.Instructions.Remove(jmpinstr);

            moveBlock.Instructions.Remove(moveinstr);
            if(moveBlock.Instructions.Count == 0)
            {
                // We should always enter this if, but to be sure, lets not remove something that we did not decompile.
                instr.Function.Blocks.Remove(moveBlock);
                // instr.Block.JumpsNextBlock = null;
                // instr.Block.JumpsNext = -1;
            }

            // If the TESTSET C == 1 we have an or if C == 0 we have an and.
            if(instr.C == 1)
            {
                return new AssignStatement(varOrErr.Value, new OrExpression(lhs, rhs));
            } 
            else if (instr.C == 0)
            {
                return new AssignStatement(varOrErr.Value, new AndExpression(lhs, rhs));
            }
            else
            {
                Debug.Assert(false, "Defuck is happening");
                return new AssignStatement(varOrErr.Value, new EmptyExpression());
            }
            
        }

        static public Expression ParseConditionExpression(Instruction instr)
        {
            // Test Expression compares the value (boolean) in A with the Value in C.
            // If they not match the next instruction is skipped.
            if(instr.OpCode == LuaOpcode.TEST)
            {
                var expr = ParseExpression(instr, instr.A);
                if(instr.C == 1)
                {
                    return new NotExpression(expr);
                }
                return new TestExpression(expr);
            }

            var leftExpr = ParseExpression(instr, instr.B);
            var rightExpr = ParseExpression(instr, instr.C);
            
            switch (instr.OpCode)
            {
                case LuaOpcode.EQ:
                    return new EqualExpression(leftExpr, rightExpr);
                case LuaOpcode.LE:
                    return new LessOrEqualThanExpression(leftExpr, rightExpr);
                case LuaOpcode.LT:
                    return new LessThanExpression(leftExpr, rightExpr);
                default:
                    return new EmptyExpression();
            }
        }

        static public Variable CreateVariable(Instruction instr, int val, out bool initialisation)
        {
            var variable = VariableCache.Get(val);
            initialisation = false;
            if(variable == null)
            {
                var name = "var" + val;
                if(instr.Function.Locals.Count > val)
                {
                    name = instr.Function.Locals[val].Name;
                }
                variable = new Variable(name);
                VariableCache.Add(val, variable);
                initialisation = true;
            } 
            return variable;
        }

        static public Expression ParseExpression(Instruction instr, int val)
        {
            if(InstructionUtil.IsConstant(val))
            {
                var index = InstructionUtil.RegToConstIndex(val);
                return CreateConstant(InstructionUtil.GetConstant(instr, index));
            }
            bool init; // unused
            return CreateVariable(instr, val, out init);
        }

        static public Constant CreateConstant(ByteConstant constant)
        {
            switch (constant.Type)
            {
                case LuaType.Bool:
                    var boolConst = constant as BoolByteConstant;
                    return new Constant(TypeCreator.CreateBool(boolConst.Value));
                case LuaType.Number:
                    var intConst = constant as NumberByteConstant;
                    return new Constant(TypeCreator.CreateDouble(intConst.Value));
                case LuaType.String:
                    var stringConst = constant as StringByteConstant;
                    return new Constant(TypeCreator.CreateString(stringConst.Value));
                default:
                    Debug.Assert(false, "Type not supported");
                    return new Constant(new AstType());
            }
        }

        static public ReturnStatement ParseReturn(Instruction instr)
        {
            if(instr.B == 1)
            {
                return new ReturnStatement();
            }
            List<Expression> exprs = new List<Expression>();
            if (instr.B > 1)
            {
                
                for (int i = 0; i < instr.B - 1; i++)
                {
                   exprs.Add(ParseExpression(instr, instr.A + i));
                }

            } else
            {
                for (int i = instr.A; i < instr.Function.MaxStackSize; i++)
                {
                    exprs.Add(ParseExpression(instr, instr.A + i));
                }
            }
            return new ReturnStatement(exprs);

        }

        static public AssignStatement ParseArithmetic(Instruction instr)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("Arithmetic not assigning to var: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            Expression lhs = ParseExpression(instr, instr.B);
            Expression rhs = ParseExpression(instr, instr.C);
            Expression arithExpr = null;
            switch (instr.OpCode)
            {
                case LuaOpcode.ADD:
                    arithExpr = new AddExpression(lhs, rhs);
                    break;
                case LuaOpcode.SUB:
                    arithExpr = new SubExpression(lhs, rhs);
                    break;
                case LuaOpcode.MUL:
                    arithExpr = new MulExpression(lhs, rhs);
                    break;
                case LuaOpcode.DIV:
                    arithExpr = new DivExpression(lhs, rhs);
                    break;
                case LuaOpcode.MOD:
                    arithExpr = new ModExpression(lhs, rhs);
                    break;
                case LuaOpcode.POW:
                    arithExpr = new PowExpression(lhs, rhs);
                    break;
            }
            return new AssignStatement(varOrErr.Value, arithExpr);
        }

        static public AssignStatement ParseSingleExprArithmetic(Instruction instr)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("Arithmetic not assigning to var: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            Expression expr = ParseExpression(instr, instr.B);
            Expression arithExpr = null;
            switch (instr.OpCode)
            {
                case LuaOpcode.UNM:
                    arithExpr = new NegationExpression(expr);
                    break;
                case LuaOpcode.NOT:
                    arithExpr = new NotExpression(expr);
                    break;
                case LuaOpcode.LEN:
                    arithExpr = new LenExpression(expr);
                    break;

            }
            return new AssignStatement(varOrErr.Value, arithExpr);
        }
        static public AssignStatement ParseMove(Instruction instr)
        {
            var lhsVarOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (lhsVarOrErr.HasError())
            {
                Debug.Print("Move can only be used on vars: " + lhsVarOrErr.GetError());
                Debug.Assert(false);
            }

            var rhsVarOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.B));
            if (rhsVarOrErr.HasError())
            {
                Debug.Print("Move can only be used on vars: " + rhsVarOrErr.GetError());
                Debug.Assert(false);
            }
            return new AssignStatement(lhsVarOrErr.Value, rhsVarOrErr.Value);
        }

        static public Global ParseGlobal(Instruction instr, int val)
        {
            if(val < instr.Function.Constants.Count)
            {
                return new Global(CreateConstant(instr.Function.Constants[val]));
            }
            // SHould not be needed, can be removed if constants are fixed.
            return new Global(new Constant(TypeCreator.CreateInt(val)));
        }

        static public AssignStatement ParseGlobalGet(GetGlobalInstruction instr)
        {
            bool init;
            var variable = CreateVariable(instr, instr.A, out init);
            var globalExpr = ParseGlobal(instr, instr.ConstIndex);
            var assign = new AssignStatement(variable, globalExpr);
            assign.Init = init;
            return assign;
        }

        static public AssignStatement ParseGlobalSet(SetGlobalInstruction instr)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetGlobal can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var globalExpr = ParseGlobal(instr, instr.ConstIndex);
            return new AssignStatement(globalExpr, varOrErr.Value);
        }

        static public CallExpression ParseCallExpr(Instruction instr)
        {
            // Function Name
            string funcName = "var" + instr.A;

            // Function Args
            // Does not work properly
            var arguments = new List<string>();
            if (instr.B == 0)
            {
                for (int i = instr.A; i < instr.B; i++)
                {
                    arguments.Add($"var{i + 1}");
                }
            }
            else
            {
                for (int i = instr.A; i < instr.A + instr.B - 1; i++)
                {
                    arguments.Add($"var{i + 1}");
                }
            }
            return new CallExpression(funcName, arguments);
        }

        static public Statement ParseCall(Instruction instr)
        {
            // TODO: there is something off here?
            // Function returns
            List<Variable> vars = new List<Variable>();
            if (instr.C == 0)
            {
                // top set to last_result+1
            }
            else if (instr.C == 1)
            {
                // no return values saved
            }
            else // 2 or more, multiple returns
            {   
                for (int i = instr.A; i < instr.A + instr.C - 1; i++)
                {
                    bool init;
                    vars.Add(CreateVariable(instr, i, out init));
                }
            }
            if(vars.Count == 0)
            {
                return new ExpressionStatement(ParseCallExpr(instr));
            }
            return new AssignStatement(vars, ParseCallExpr(instr));
        }

        static public ReturnStatement ParseTailCall(Instruction instr)
        {
            string funcName = "var" + instr.A;
            // Function Args
            var arguments = new List<string>();
            if (instr.B == 0)
            {
                for (int i = instr.A; i < instr.B; i++)
                {
                    arguments.Add($"var{i + 1}");
                }
            }
            else
            {
                for (int i = instr.A; i < instr.A + instr.B - 1; i++)
                {
                    arguments.Add($"var{i + 1}");
                }
            }

            // Remove next return.
            var index = instr.Block.Instructions.IndexOf(instr);
            var returnStm = instr.Block.Instructions[index + 1];
            Debug.Assert(returnStm.OpCode == LuaOpcode.RETURN, "Tail call should always be followed by a return");
            instr.Block.Instructions.Remove(instr);

            return new ReturnStatement(new CallExpression(funcName, arguments));
        }

        static public AssignStatement ParseClosure(Instruction instr)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("Closure can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            if(instr.Function.Functions.Count < instr.Bx)
            {
                return new AssignStatement(varOrErr.Value, 
                    new Constant(TypeCreator.CreateString("[MissingFunction]")));
            }
            var callee = instr.Function.Functions[instr.Bx];
            return new AssignStatement(varOrErr.Value, 
                new Constant(TypeCreator.CreateString(callee.Name)));
        }

        static public AssignStatement ParseVarArg(Instruction instr)
        {
            // Todo set function is vararg
            var vars = new List<Variable>();
            for(int i = instr.A; i < instr.A + instr.B - 1; ++i)
            {
                bool init;
                vars.Add(CreateVariable(instr, i, out init));
            }
            return new AssignStatement(vars, new VarArg());
        }

        static public StatementList ParseSelf(Instruction instr)
        {
            // A = element
            // B = ref to table
            // C = methode itself
            StatementList statements = new StatementList();
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A + 1));
            if (varOrErr.HasError())
            {
                Debug.Print("SELF can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var rhs = ParseExpression(instr, instr.B);
            // Store the table itself (Instr.B) in Instr.A + 1
            statements.Add(new AssignStatement(varOrErr.Value, rhs));

            var var2OrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (var2OrErr.HasError())
            {
                Debug.Print("SELF can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            // TODO Can be a var or a const index
            var tableIndex = InstructionUtil.RegToConstIndex(instr.C);
            bool init;
            var tableGet = new GetTableExpression(CreateVariable(instr, instr.B, out init), 
                CreateConstant(InstructionUtil.GetConstant(instr, tableIndex)));
            statements.Add(new AssignStatement(var2OrErr.Value, tableGet));
            return statements;
        }

        static public AssignStatement ParseNewTable(Instruction instr) {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("NewTable can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            int tableSize = instr.B;
            var newTableExpr = new NewTableExpression(tableSize);

            return new AssignStatement(varOrErr.Value, newTableExpr);
        }

        static public AssignStatement ParseSetTable(Instruction instr)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetTable can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var tableIndex = ParseExpression(instr, instr.B);
            var rhs = ParseExpression(instr, instr.C);

            return new AssignStatement(new SetTableExpression(varOrErr.Value, tableIndex), rhs);
        }

        static public AssignStatement ParseGetTable(Instruction instr)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("GetTable can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var tableVarOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.B));
            if (tableVarOrErr.HasError())
            {
                Debug.Print("A table is always a var: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var tableIndex = ParseExpression(instr, instr.C);

            return new AssignStatement(varOrErr.Value, new GetTableExpression(tableVarOrErr.Value, tableIndex));
        }

        static public AssignStatement ParseSetList(Instruction instr)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetList can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            List<Expression> epxrList = new List<Expression>();
            for(int i = 1; i <= instr.B; ++i)
            {
                var expr = ParseExpression(instr, instr.A + i);
                epxrList.Add(expr);
            }

            return new AssignStatement(varOrErr.Value, new SetListExpression(epxrList));
        }

        static public Upvalue ParseUpvalue(Instruction instr, int val)
        {
            var upvalName = instr.Function.Upvals[val];
            return new Upvalue(upvalName);
        }

        static public AssignStatement ParseGetUpval(Instruction instr)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetList can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var upval = ParseUpvalue(instr, instr.B);
            return new AssignStatement(varOrErr.Value, upval);
        }

        static public AssignStatement ParseSetUpval(Instruction instr)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetList can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var upval = ParseUpvalue(instr, instr.B);
            return new AssignStatement(upval, varOrErr.Value);
        }

        static public AssignStatement ParseConcat(Instruction instr)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(instr, instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetList can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var exprs = new List<Expression>();
            for (int i = instr.B; i <= instr.C; ++i)
            {
                exprs.Add(ParseExpression(instr, i)); 
            }

            return new AssignStatement(varOrErr.Value, new ConcatExpression(exprs));
        }

        static public JumpStatement ParseJump(Instruction instr)
        {
            // var func = instr.Block.ScriptFunction;
            // Remove the block from the function, to avoid parsing it twice.
            // func.Blocks.Remove(instr.JumpsTo);
            // TODO FIX
            return new JumpStatement(null, instr.sBx > 0);
            // return new JumpStatement(Parse(instr.JumpsTo), instr.sBx > 0);
        }
    }
}
