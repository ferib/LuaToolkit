using LuaToolkit.Decompiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Ast
{
    public class ASTParser
    {
        Statement Parse(LuaScriptFunction function)
        {
            var name = function.Name;
            var statements = Parse(function.Blocks);
            return new FunctionDefinitionStatement(name, statements);
        }

        StatementList Parse(List<LuaScriptBlock> blocks)
        {
            var statements = new StatementList();
            foreach(var block in blocks)
            {
                statements.Add(Parse(block));
            }
            return statements;
        }

        StatementList Parse(LuaScriptBlock block)
        {
            var statements = new StatementList();
            foreach (var line in block.Lines)
            {
                statements.Add(Parse(line));
            }
            return statements;
        }

        Statement Parse(LuaScriptLine line)
        {
            switch (line.Instr.OpCode)
            {
                case LuaOpcode.LOADK:
                    return ParseLoad(line);

                default:
                    Debug.Assert(false, "Opcode '" + line.Instr.OpCode.ToString() + "' Not supported yet.");
                    return new EmptyStatement(); ;
            }
        } 

        Statement ParseLoad(LuaScriptLine line)
        {
            var VarName = line.Op1;
            var Variable = new Variable(VarName);
            var Constant = new Constant(TypeCreator.CreateString(line.Op3));
            return new AssignStatement(Variable, Constant);
        }
    }
}
