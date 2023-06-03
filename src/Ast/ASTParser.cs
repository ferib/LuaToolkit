using LuaToolkit.Decompiler;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;

namespace LuaToolkit.Ast
{
    public class StatementCache
    {
        public StatementList Get(LuaScriptBlock block)
        {
            if (cache.TryGetValue(block, out var result))
            {
                return result;
            }
            return null;
        }

        public void Add(LuaScriptBlock block, StatementList list)
        {
            if (cache.ContainsKey(block))
            {
                Debug.Assert(false, "Block is already in cache");
            }
            cache[block] = list;
        }

        Dictionary<LuaScriptBlock, StatementList> cache =
            new Dictionary<LuaScriptBlock, StatementList>();
    }

    public class ASTParser
    {
        static public StatementCache Cache = new StatementCache();
        static public FunctionDefinitionStatement Parse(LuaScriptFunction function)
        {
            var name = function.Name;
            var statements = Parse(function.Blocks);
            return new FunctionDefinitionStatement(name, statements);
        }

        static public StatementList Parse(List<LuaScriptBlock> blocks)
        {
            var statements = new StatementList();
            for(int i = 0; i < blocks.Count; ++i)
            {
                statements.Add(Parse(blocks[i]));
            }
            return statements;
        }

        static public StatementList Parse(LuaScriptBlock block)
        {
            StatementList statements = Cache.Get(block);
            if(statements != null)
            {
                return statements;
            }
            statements = new StatementList();
            for (var line = 0; line < block.Lines.Count; ++line)
            {
                statements.Add(Parse(block.Lines[line]));
            }
            Cache.Add(block, statements);
            return statements;
        }

        static public Statement Parse(LuaScriptLine line)
        {
            switch (line.Instr.OpCode)
            {
                case LuaOpcode.LOADK:
                    return ParseLoad(line);
                case LuaOpcode.LOADBOOL:
                    return ParseLoadBool(line);
                case LuaOpcode.LOADNIL:
                    return ParseLoadNil(line);
                case LuaOpcode.MOVE:
                    return ParseMove(line);
                case LuaOpcode.GETGLOBAL:
                    return ParseGlobalGet(line);
                case LuaOpcode.SETGLOBAL:
                    return ParseGlobalSet(line);
                case LuaOpcode.EQ:
                case LuaOpcode.LT:
                case LuaOpcode.LE:
                case LuaOpcode.TEST:
                    return ParseCondition(line);
                case LuaOpcode.TESTSET:
                    return ParseTestSet(line);
                case LuaOpcode.RETURN:
                    return ParseReturn(line);
                case LuaOpcode.JMP:
                    var func = line.Block.ScriptFunction;
                    // Remove the block from the function, to avoid parsing it twice.
                    func.Blocks.Remove(line.JumpsTo);
                    return new JumpStatement(Parse(line.JumpsTo));
                case LuaOpcode.FORPREP:
                case LuaOpcode.FORLOOP:
                    return ParseFor(line);
                case LuaOpcode.ADD:
                case LuaOpcode.SUB:
                case LuaOpcode.MUL:
                case LuaOpcode.DIV:
                case LuaOpcode.POW:
                case LuaOpcode.MOD:
                    return ParseArithmetic(line);
                case LuaOpcode.UNM:
                case LuaOpcode.NOT:
                case LuaOpcode.LEN:
                    return ParseSingleExprArithmetic(line);
                case LuaOpcode.CONCAT:
                    return ParseConcat(line);
                case LuaOpcode.CALL:
                    return ParseCall(line);
                case LuaOpcode.TAILCALL:
                    return ParseTailCall(line);
                case LuaOpcode.CLOSURE:
                    return ParseClosure(line);
                case LuaOpcode.VARARG:
                    return ParseVarArg(line);
                // Tables
                case LuaOpcode.NEWTABLE:
                    return ParseNewTable(line);
                case LuaOpcode.SETTABLE:
                    return ParseSetTable(line);
                case LuaOpcode.GETTABLE:
                    return ParseGetTable(line);
                case LuaOpcode.SETLIST:
                    return ParseSetList(line);
                // Upvalues
                case LuaOpcode.GETUPVAL:
                    return ParseGetUpval(line);
                case LuaOpcode.SETUPVAL:
                    return ParseSetUpval(line);
                case LuaOpcode.CLOSE:
                    return new CloseStatement();
                default:
                    Debug.Assert(false, "Opcode '" + line.Instr.OpCode.ToString() + "' Not supported yet.");
                    return new EmptyStatement(); ;
            }
        }



        // TODO Fix parsing of assigned value
        static public Statement ParseLoad(LuaScriptLine line)
        {
            var VarName = "var" + line.Instr.A;
            var Variable = new Variable(VarName);
            
            bool isConstant;
            var index = LuaScriptLine.ToIndex18(line.Instr.Bx, out isConstant);
            Constant constant;
            if(index > line.Func.Constants.Count - 1)
            {
                constant = new Constant(TypeCreator.CreateInt(line.Instr.Bx));
                
            } else
            {
                constant = CreateConstant(line.Func.Constants[index]);
            }
            
            return new AssignStatement(Variable, constant);
        }

        static public Statement ParseLoadNil(LuaScriptLine line)
        {
            var statements = new StatementList();
            for (int i = line.Instr.A; i < line.Instr.B + 1; ++i)
            {
                statements.Add(new AssignStatement(ParseExpression(line, i) as Variable, 
                    new Constant(TypeCreator.CreateNil())));
            }
            return statements;
        }

        static public Statement ParseLoadBool(LuaScriptLine line)
        {
            var VarName = "var" + line.Instr.A;
            var Variable = new Variable(VarName);
            var Constant = new Constant(TypeCreator.CreateBool(line.Instr.B == 1));
            return new AssignStatement(Variable, Constant);
        }

        static public Statement ParseCondition(LuaScriptLine line)
        {
            var ifBody = Parse(line.Block.JumpsNextBlock);
            // Remove the block here, that we don't parse it twice.
            line.Block.ScriptFunction.Blocks.Remove(line.Block.JumpsNextBlock);
            var expr = ParseConditionExpression(line);
            return new IfStatement(expr, ifBody);
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
        /// <param name="line"></param>
        /// <returns></returns>
        static public AssignStatement ParseTestSet(LuaScriptLine line)
        {
            // Probably does not work correctly, I think this can be chained.
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("TestSet not assigning to var: " + varOrErr.GetError());
                Debug.Assert(false);
            }

            var lhs = ParseExpression(line, line.Instr.B);
            var lineIndex = line.Block.Lines.IndexOf(line);
            // Test SET should always be followed by a JUMP
            // We don't need it to decompile, we will remove it.
            var jmpLine = line.Block.Lines[lineIndex+1];
            Debug.Assert(jmpLine.Instr.OpCode == LuaOpcode.JMP);

            // After the jump is a move which is the assignment of the second var.
            var moveBlock = line.Block.JumpsNextBlock;
            Debug.Assert(moveBlock.Lines.Count == 1, "There should only by 1 instruction in this move block (the move)");
            var moveLine = moveBlock.Lines[0];
            
            Debug.Assert(moveLine.Instr.OpCode == LuaOpcode.MOVE);

            var rhs = ParseExpression(moveLine, moveLine.Instr.B);

            // Delete everything that we don't need.
            line.Block.Lines.Remove(jmpLine);

            moveBlock.Lines.Remove(moveLine);
            if(moveBlock.Lines.Count == 0)
            {
                // We should always enter this if, but to be sure, lets not remove something that we did not decompile.
                line.Block.ScriptFunction.Blocks.Remove(moveBlock);
                line.Block.JumpsNextBlock = null;
                line.Block.JumpsNext = -1;
            }

            // If the TESTSET C == 1 we have an or if C == 0 we have an and.
            if(line.Instr.C == 1)
            {
                return new AssignStatement(varOrErr.Value, new OrExpression(lhs, rhs));
            } 
            else if (line.Instr.C == 0)
            {
                return new AssignStatement(varOrErr.Value, new AndExpression(lhs, rhs));
            }
            else
            {
                Debug.Assert(false, "Defuck is happening");
                return new AssignStatement(varOrErr.Value, new EmptyExpression());
            }
            
        }

        static public Expression ParseConditionExpression(LuaScriptLine line)
        {
            // Test Expression compares the value (boolean) in A with the Value in C.
            // If they not match the next instruction is skipped.
            if(line.Instr.OpCode == LuaOpcode.TEST)
            {
                var expr = ParseExpression(line, line.Instr.A);
                if(line.Instr.C == 1)
                {
                    return new NotExpression(expr);
                }
                return new TestExpression(expr);
            }

            var leftExpr = ParseExpression(line, line.Instr.B);
            var rightExpr = ParseExpression(line, line.Instr.C);
            
            switch (line.Instr.OpCode)
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

        static public Expression ParseExpression(LuaScriptLine line, int val)
        {
            var index = LuaScriptLine.ToIndex9(val, out bool isConsant);
            if (isConsant)
            {
                var constant = line.Func.Constants[index];
                return CreateConstant(constant);
            }
            if (line.Func.ScriptFunction.GetUsedLocals().Contains(val))
                return new Variable("var" + index);
            else
            {
                line.Func.ScriptFunction.GetUsedLocals().Add(val) ;
                return new Variable("var" + index);
            }
        }

        static public Constant CreateConstant(LuaConstant constant)
        {
            switch (constant.Type)
            {
                case LuaType.Bool:
                    var boolConst = constant as BoolConstant;
                    return new Constant(TypeCreator.CreateBool(boolConst.Value));
                case LuaType.Number:
                    var intConst = constant as NumberConstant;
                    return new Constant(TypeCreator.CreateDouble(intConst.Value));
                case LuaType.String:
                    var stringConst = constant as StringConstant;
                    return new Constant(TypeCreator.CreateString(stringConst.Value));
                default:
                    Debug.Assert(false, "Type not supported");
                    return new Constant(new AstType());
            }
        }

        static public ReturnStatement ParseReturn(LuaScriptLine line)
        {
            if(line.Instr.B == 1)
            {
                return new ReturnStatement();
            }
            List<Expression> exprs = new List<Expression>();
            if (line.Instr.B > 1)
            {
                
                for (int i = 0; i < line.Instr.B - 1; i++)
                {
                   exprs.Add(ParseExpression(line, line.Instr.A + i));
                }

            } else
            {
                for (int i = line.Instr.A; i < line.Func.MaxStackSize; i++)
                {
                    exprs.Add(ParseExpression(line, line.Instr.A + i));
                }
            }
            return new ReturnStatement(exprs);

        }

        static public ForStatment ParseFor(LuaScriptLine line)
        {
            return new ForStatment();
        }

        static public AssignStatement ParseArithmetic(LuaScriptLine line)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("Arithmetic not assigning to var: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            Expression lhs = ParseExpression(line, line.Instr.B);
            Expression rhs = ParseExpression(line, line.Instr.C);
            Expression arithExpr = null;
            switch (line.Instr.OpCode)
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

        static public AssignStatement ParseSingleExprArithmetic(LuaScriptLine line)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("Arithmetic not assigning to var: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            Expression expr = ParseExpression(line, line.Instr.B);
            Expression arithExpr = null;
            switch (line.Instr.OpCode)
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
        static public AssignStatement ParseMove(LuaScriptLine line)
        {
            var lhsVarOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (lhsVarOrErr.HasError())
            {
                Debug.Print("Move can only by used on vars: " + lhsVarOrErr.GetError());
                Debug.Assert(false);
            }

            var rhsVarOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.B));
            if (rhsVarOrErr.HasError())
            {
                Debug.Print("Move can only by used on vars: " + rhsVarOrErr.GetError());
                Debug.Assert(false);
            }
            return new AssignStatement(lhsVarOrErr.Value, rhsVarOrErr.Value);
        }

        static public Global ParseGlobal(LuaScriptLine line, int val)
        {
            if(val < line.Func.Constants.Count)
            {
                return new Global(CreateConstant(line.Func.Constants[val]));
            }
            // SHould not be needed, can be removed if constants are fixed.
            return new Global(new Constant(TypeCreator.CreateInt(val)));
        }

        static public AssignStatement ParseGlobalGet(LuaScriptLine line)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("GlobalGet can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var globalExpr = ParseGlobal(line, line.Instr.Bx);
            return new AssignStatement(varOrErr.Value, globalExpr);
        }

        static public AssignStatement ParseGlobalSet(LuaScriptLine line)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetGlobal can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var globalExpr = ParseGlobal(line, line.Instr.Bx);
            return new AssignStatement(globalExpr, varOrErr.Value);
        }

        static public AssignStatement ParseCall(LuaScriptLine line)
        {
            // TODO: there is something off here?
            // Function returns
            List<Variable> vars = new List<Variable>();
            if (line.Instr.C == 0)
            {
                // top set to last_result+1
            }
            else if (line.Instr.C == 1)
            {
                // no return values saved
            }
            else // 2 or more, multiple returns
            {   
                for (int i = line.Instr.A; i < line.Instr.A + line.Instr.C - 1; i++)
                {
                    vars.Add(new Variable("var" + i));
                }
            }

            // Function Name
            //this.Op2 = $"var{Instr.A}"; // func name only (used lateron)
            string funcName = "var" + line.Instr.A;
            
            // Function Args
            var arguments = new List<string>();
            if (line.Instr.B == 0)
            {
                for (int i = line.Instr.A; i < line.Instr.B; i++)
                {
                    arguments.Add($"var{i + 1}");
                }
            }
            else
            {
                for (int i = line.Instr.A; i < line.Instr.A + line.Instr.B - 1; i++)
                {
                    arguments.Add($"var{i + 1}");
                }
            }
            var callExpr = new CallExpression(funcName, arguments);

            return new AssignStatement(vars, callExpr);
        }

        static public ReturnStatement ParseTailCall(LuaScriptLine line)
        {
            string funcName = "var" + line.Instr.A;
            // Function Args
            var arguments = new List<string>();
            if (line.Instr.B == 0)
            {
                for (int i = line.Instr.A; i < line.Instr.B; i++)
                {
                    arguments.Add($"var{i + 1}");
                }
            }
            else
            {
                for (int i = line.Instr.A; i < line.Instr.A + line.Instr.B - 1; i++)
                {
                    arguments.Add($"var{i + 1}");
                }
            }

            // Remove next return.
            var index = line.Block.Lines.IndexOf(line);
            var returnStm = line.Block.Lines[index + 1];
            Debug.Assert(returnStm.Instr.OpCode == LuaOpcode.RETURN, "Tail call should always be followed by a return");
            line.Block.Lines.Remove(line);

            return new ReturnStatement(new CallExpression(funcName, arguments));
        }

        static public AssignStatement ParseClosure(LuaScriptLine line)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("Closure can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            if(line.Func.Functions.Count < line.Instr.Bx)
            {
                return new AssignStatement(varOrErr.Value, 
                    new Constant(TypeCreator.CreateString("[MissingFunction]")));
            }
            var callee = line.Func.Functions[line.Instr.Bx];
            return new AssignStatement(varOrErr.Value, 
                new Constant(TypeCreator.CreateString(callee.Name)));
        }

        static public AssignStatement ParseVarArg(LuaScriptLine line)
        {
            // Todo set function is vararg
            var vars = new List<Variable>();
            for(int i = line.Instr.A; i < line.Instr.A + line.Instr.B - 1; ++i)
            {
                vars.Add(new Variable("var" + i));
            }
            return new AssignStatement(vars, new VarArg());
        }

        static public StatementList ParseSelf(LuaScriptLine line)
        {
            // A = element
            // B = ref to table
            // C = methode itself
            StatementList statements = new StatementList();
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A + 1));
            if (varOrErr.HasError())
            {
                Debug.Print("SELF can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var rhs = ParseExpression(line, line.Instr.B);
            // Store the table itself (Instr.B) in Instr.A + 1
            statements.Add(new AssignStatement(varOrErr.Value, rhs));

            var var2OrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (var2OrErr.HasError())
            {
                Debug.Print("SELF can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var tableIndex = ParseExpression(line, line.Instr.C);
            var tableGet = new GetTableExpression(new Variable("var" + line.Instr.B), tableIndex);
            statements.Add(new AssignStatement(var2OrErr.Value, tableGet));
            return statements;
        }

        static public AssignStatement ParseNewTable(LuaScriptLine line) {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("NewTable can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            int tableSize = line.Instr.B;
            var newTableExpr = new NewTableExpression(tableSize);

            return new AssignStatement(varOrErr.Value, newTableExpr);
        }

        static public AssignStatement ParseSetTable(LuaScriptLine line)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetTable can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var tableIndex = ParseExpression(line, line.Instr.B);
            var rhs = ParseExpression(line, line.Instr.C);

            return new AssignStatement(new SetTableExpression(varOrErr.Value, tableIndex), rhs);
        }

        static public AssignStatement ParseGetTable(LuaScriptLine line)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("GetTable can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var tableVarOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.B));
            if (tableVarOrErr.HasError())
            {
                Debug.Print("A table is always a var: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var tableIndex = ParseExpression(line, line.Instr.C);

            return new AssignStatement(varOrErr.Value, new GetTableExpression(tableVarOrErr.Value, tableIndex));
        }

        static public AssignStatement ParseSetList(LuaScriptLine line)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetList can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            List<Expression> epxrList = new List<Expression>();
            for(int i = 1; i <= line.Instr.B; ++i)
            {
                var expr = ParseExpression(line, line.Instr.A + i);
                epxrList.Add(expr);
            }

            return new AssignStatement(varOrErr.Value, new SetListExpression(epxrList));
        }

        static public Upvalue ParseUpvalue(LuaScriptLine line, int val)
        {
            var upvalName = line.Func.DebugUpvalues[val];
            return new Upvalue(upvalName);
        }

        static public AssignStatement ParseGetUpval(LuaScriptLine line)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetList can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var upval = ParseUpvalue(line, line.Instr.B);
            return new AssignStatement(varOrErr.Value, upval);
        }

        static public AssignStatement ParseSetUpval(LuaScriptLine line)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetList can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var upval = ParseUpvalue(line, line.Instr.B);
            return new AssignStatement(upval, varOrErr.Value);
        }

        static public AssignStatement ParseConcat(LuaScriptLine line)
        {
            var varOrErr = ExpressionCovertor<Variable>.Convert(ParseExpression(line, line.Instr.A));
            if (varOrErr.HasError())
            {
                Debug.Print("SetList can only by used on vars: " + varOrErr.GetError());
                Debug.Assert(false);
            }
            var exprs = new List<Expression>();
            for (int i = line.Instr.B; i <= line.Instr.C; ++i)
            {
                exprs.Add(ParseExpression(line, i)); 
            }

            return new AssignStatement(varOrErr.Value, new ConcatExpression(exprs));
        }
    }
}
