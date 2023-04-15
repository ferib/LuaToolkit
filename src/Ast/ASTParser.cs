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
            // if Jumps to block has a jump it is an else
            // if(line.Block.JumpsNextBlock.JumpsToBlock != null)
            // {
            //    var elseBlock = Parse(line.Block.JumpsToBlock);
            //    return new IfElseStatment(expr, ifBody, elseBlock);
            //}
            //return new EmptyStatement();
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
    }
}
