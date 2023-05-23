using LuaToolkit.Decompiler;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
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
            foreach (var line in block.Lines)
            {
                statements.Add(Parse(line));
            }
            Cache.Add(block, statements);
            return statements;
        }

        static public Statement Parse(LuaScriptLine line)
        {
            switch (line.Instr.OpCode)
            {
                case LuaOpcode.LOADK:
                case LuaOpcode.LOADBOOL:
                     return ParseLoad(line);
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
                    return ParseCondition(line);
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
                    return ParseSingleExprArithmetic(line);
                case LuaOpcode.CALL:
                    return ParseCall(line);
                default:
                    Debug.Assert(false, "Opcode '" + line.Instr.OpCode.ToString() + "' Not supported yet.");
                    return new EmptyStatement(); ;
            }
        }

        // TODO Fix parsing of assigned value
        static public Statement ParseLoad(LuaScriptLine line)
        {
            var VarName = line.Op1;
            var Variable = new Variable(VarName);
            var constIndex = line.Instr.Bx;

            Constant constant;
            if(constIndex > line.Func.Constants.Count)
            {
                constant = new Constant(TypeCreator.CreateInt(line.Instr.Bx));
                
            } else
            {
                constant = CreateConstant(line.Func.Constants[constIndex]);
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

        static public Statement ParseBool(LuaScriptLine line)
        {
            var VarName = line.Op1;
            var Variable = new Variable(VarName);
            var Constant = new Constant(TypeCreator.CreateBool(line.Instr.B != 0));
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

        static public Expression ParseConditionExpression(LuaScriptLine line)
        {
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
            var index = LuaScriptLine.ToIndex(val, out bool isConsant);
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
                    // arithExpr = new MulExpression(lhs, rhs);
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
                Debug.Print("Move can only by used on vars: " + varOrErr.GetError());
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
                Debug.Print("Move can only by used on vars: " + varOrErr.GetError());
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
            string funcName = "var{" + line.Instr.A + "}";
            
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
    }
}
