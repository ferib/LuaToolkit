using LuaToolkit.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace LuaToolkit.Ast.Passes
{

    // Patterns
    // for e in list do:
    // 1 LOADNIL // Setup
    // 2 JMP 4 // JMP to forloop setup
    // 3 ... // for body
    // 4 TFORLOOP ; Test if for loop should continue and set vars
    // 5 JMP 3 ; jump to for body
    //
    // for var=e1, e2, e2 do
    // 1 FORPREP
    // 2 ... // for body
    // 3 FORLOOP
    //
    // while expr do
    // 1 TEST
    // 2 JMP 5 // End while loop
    // 3 ... //while body
    // 4 JMP 1 // Loop again
    //
    // repeat until
    // 1 ... // repeat body
    // 2 TEST // Test if loop should end
    // 3 JMP 1 // loop again
    //
    // if
    // 1 TEST // Check if
    // 2 JMP 4 // Jump out of if
    // 3 ...  // if body
    // 4      // if end
    //
    // if else
    // 1 TEST // Check if
    // 2 JMP 5 // Jump to else
    // 3 ...  // if body
    // 4 JMP 6 // skip else
    // 5 ...  // else body
    // 6      // if end
    //
    // if elsif else
    // 1 TEST // Check if
    // 2 JMP 5 // Jump to elseif
    // 3 ...  // if body
    // 4 JMP 10 // skip to end
    // 5 TEST   // Check if
    // 6 JMP 9 // Jump to else
    // 7 ...  // elseif body
    // 8 JMP 10 // skip else
    // 9 ...  // else body
    // 10      // if end

    public class ParseJmpPass : BaseFunctionPass
    {
        public override bool RunOnFunction(FunctionDefinitionStatement function)
        {
            var str = function.Dump();
            for(int i = 0; i < function.StatementList.Statements.Count; ++i)
            {
                var block = function.StatementList.Statements[i];
                var res = Convertor<StatementList>.Convert(block);
                if(res.HasError())
                {
                    Debug.Assert(false, "Every block should be a statement list");
                    continue;
                }
                RunOnBlock(res.Value);
            }

            return true;
        }

        public bool RunOnBlock(StatementList block)
        {
            var ifOrErr = GetIfStatement(block);
            if(ifOrErr.HasError())
            {
                return false;
            }
            var ifIndex = block.Statements.IndexOf(ifOrErr.Value);

            // After the ifstatement there is always a jump.
            if(ifIndex >= block.Statements.Count-1)
            {
                return false;
            }
            var jumpStatementOrErr = Convertor<JumpStatement>.Convert(block.Statements[ifIndex+1]);
            if(jumpStatementOrErr.HasError())
            {
                return false;
            }
            
            
            // If the ifbody ends with a jmp, the chain continues.
            var ifBody = Convertor<StatementList>.Convert(ifOrErr.Value.Statement);
            var ifBodyJmp = GetJmpStatement(ifBody.Value);
            if(ifBodyJmp.HasError())
            {
                // If body does not end with a jmp, if chain stops here.
                // The jump in the if is the next block, to skip the if body
                var blockindex = block.Parent.Statements.IndexOf(block);
                block.Parent.Insert(blockindex + 1, jumpStatementOrErr.Value.Statement);
                block.Statements.Remove(jumpStatementOrErr.Value);
                return true;
            }
            // Parse if chain.
            List<IfStatement> ifChain = new List<IfStatement>();
            Statement currentIf = ifOrErr.Value;
            Statement elseOrNextBlock = null;
            while (currentIf != null && currentIf.Type == STATEMENT_TYPE.IF)
            {
                ifChain.Add(Convertor<IfStatement>.Convert(currentIf).Value);
                var nextJmpOrErr = Convertor<JumpStatement>.Convert(currentIf.GetNextStatement());
                if (nextJmpOrErr.HasError())
                {
                    break;
                }
                var nextJmp = nextJmpOrErr.Value;
                var jmpNextBlock = Convertor<StatementList>.Convert(nextJmp.Statement).Value;
                var tempIf = GetIfStatement(jmpNextBlock).Value;
                // Remove the jump from the if body
                currentIf.Parent.Statements.Remove(nextJmp);
                currentIf = tempIf;
                // If the last if is null, this means we have an else block.
                if(currentIf == null)
                {
                    elseOrNextBlock = jmpNextBlock;
                }
                // if there is not next if, currentIf is null.
            }
            bool hasElse = true;
            Statement nextBlock = null;
            foreach(var ifStatement in ifChain)
            {
                var ifStBody = Convertor<StatementList>.Convert(ifStatement.Statement).Value;
                // Get the last jmp of the if body.
                var jmpOrErr = GetJmpStatement(ifStBody);

                if(jmpOrErr.HasError())
                {
                    // If the last if does not end with a jmp, that means there is no else
                    if(ifChain.IndexOf(ifStatement) == ifChain.Count-1)
                    {
                        hasElse = false;
                        break;
                    }
                }
                Debug.Assert(!jmpOrErr.HasError(), "Every if has to be followed by a jmp");
                var jmp = jmpOrErr.Value;
                if(nextBlock == null )
                {
                    nextBlock = jmp.Statement;
                }
                Debug.Assert(nextBlock == jmp.Statement, "Every if in the if chain has to jump to the same block");
                // Remove all the jmps from the if statement, this is the else.
                ifStBody.Statements.Remove(jmp);

            }
            if(hasElse)
            {
                var elseIfList = new ElseIfElseStatement(ifChain, elseOrNextBlock);
                block.Statements.Insert(ifIndex, elseIfList);
                block.Statements.Remove(ifOrErr.Value);
                block.Statements.Insert(ifIndex + 1, nextBlock);
                return true;
            } else
            {
                Debug.Assert(elseOrNextBlock == nextBlock, 
                    "If there is no else block, the potential else should be the next");
                var elseIfList = new ElseIfStatement(ifChain);
                block.Statements.Insert(ifIndex, elseIfList);
                block.Statements.Remove(ifOrErr.Value);
                block.Statements.Insert(ifIndex+1, nextBlock);
                return true;
            }
        }

        public Expected<IfStatement> GetIfStatement(StatementList block)
        {
            foreach(var statement in block.Statements)
            {
                var ifOrErr = Convertor<IfStatement>.Convert(statement);
                if(ifOrErr.HasError())
                {
                    continue;
                }
                return ifOrErr;
            }
            return new Expected<IfStatement>("Block does not contain if");
        }

        public Expected<JumpStatement> GetJmpStatement(StatementList block)
        {
            foreach (var statement in block.Statements)
            {
                var ifOrErr = Convertor<JumpStatement>.Convert(statement);
                if (ifOrErr.HasError())
                {
                    continue;
                }
                return ifOrErr;
            }
            return new Expected<JumpStatement>("Block does not contain jmp");
        }
    }
}


