using LuaToolkit.Ast;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class TestAst
    {
        [Fact]
        public void TestAssignment()
        {
            var variable = new Variable("x", TypeCreator.CreateInt(10));
            var constant = new Constant(TypeCreator.CreateInt(5));
            var assignStatement = new AssignStatement(variable, constant);
            assignStatement.Execute();
            Assert.Equal(5, variable.Content.Int);

            var dump = assignStatement.Dump();
            
            Assert.Equal("x = 5", StringUtil.StripNewlines(dump));
        }

        [Fact]
        public void TestBinOps()
        {
            var var1 = new Variable("x", TypeCreator.CreateInt(10));
            var var2 = new Variable("y", TypeCreator.CreateInt(5)); 

            var equalExpr = new EqualExpression(var1, var2);
            var equalRes = equalExpr.Execute();
            Assert.False(equalRes.Bool);
           
            var inequalExpr = new InequalsExpression(var1, var2);
            var inequalRes = inequalExpr.Execute();
            Assert.True(inequalRes.Bool);

            var andExpr = new AndExpression(inequalExpr, equalExpr);
            Assert.False(andExpr.Execute().Bool);
            
            var orExpr = new OrExpression(inequalExpr, equalExpr);
            Assert.True(orExpr.Execute().Bool);

            var notExpr = new NotExpression(equalExpr);
            Assert.True(notExpr.Execute().Bool);

            var smallerExpr = new LessThanExpression(var1, var2);
            Assert.False(smallerExpr.Execute().Bool);

            var largerExpr = new BiggerThanExpression(var1, var2);
            Assert.True(largerExpr.Execute().Bool);

            var smallerEqualExpr1 = new LessOrEqualThanExpression(var1, var2);
            Assert.False(smallerEqualExpr1.Execute().Bool);
            var smallerEqualExpr2 = new LessOrEqualThanExpression(var1, var1);
            Assert.True(smallerEqualExpr2.Execute().Bool);

            var largerEqualExpr1 = new BiggerOrEqualThanExpression(var1, var2); 
            Assert.True(largerEqualExpr1.Execute().Bool);
            var largerEqualExpr2 = new BiggerOrEqualThanExpression(var1, var1);
            Assert.True(largerEqualExpr2.Execute().Bool);

        }

        [Fact]
        public void TestIf()
        {
            var expectedStr =   "if x != y then" + StringUtil.NewLineChar +
                                    "x = y" + StringUtil.NewLineChar +
                                "end" + StringUtil.NewLineChar;

            var var1 = new Variable("x", TypeCreator.CreateInt(10));
            var var2 = new Variable("y", TypeCreator.CreateInt(5));

            var inequalExpr = new InequalsExpression(var1, var2);

            Assert.True(inequalExpr.Execute().Bool);

            // If var1 and var2 are not equal, make them equal.
            var ifStm = new IfStatement(inequalExpr, 
                new AssignStatement(var1, var2));

            ifStm.Execute();

            // after executing the if they should be made equal.
            Assert.False(inequalExpr.Execute().Bool);

            var str = ifStm.Dump();
            Assert.Equal(str, expectedStr);
        }

        [Fact]
        public void TestIfElse()
        {
            var expectedStr =   "if x != y then" + StringUtil.NewLineChar +
                                    "x = y" + StringUtil.NewLineChar +
                                "else" + StringUtil.NewLineChar +
                                    "x = 10" + StringUtil.NewLineChar +
                                "end" + StringUtil.NewLineChar;

            var var1 = new Variable("x", TypeCreator.CreateInt(10));
            var var2 = new Variable("y", TypeCreator.CreateInt(5));

            var inequalExpr = new InequalsExpression(var1, var2);

            Assert.True(inequalExpr.Execute().Bool);

            // If var1 and var2 are not equal, make them equal.
            var ifElseStm = new IfElseStatement(inequalExpr,
                new AssignStatement(var1, var2), 
                new AssignStatement(var1, new Constant( TypeCreator.CreateInt(10))));

            ifElseStm.Execute();

            // after executing the if they should be made equal.
            Assert.False(inequalExpr.Execute().Bool);

            // The expression x != y is no longer true, else block will be executed.
            ifElseStm.Execute();

            // after executing the if they should be made equal.
            Assert.True(inequalExpr.Execute().Bool);

            var str = ifElseStm.Dump();
            Assert.Equal(str, expectedStr);
        }

        [Fact]
        public void TestElseIf()
        {
            var expectedStr =   "if x != y then" + StringUtil.NewLineChar +
                                    "x = y" + StringUtil.NewLineChar +
                                "else if x == y then" + StringUtil.NewLineChar +
                                    "x = 10" + StringUtil.NewLineChar +
                                "end" + StringUtil.NewLineChar;

            var var1 = new Variable("x", TypeCreator.CreateInt(10));
            var var2 = new Variable("y", TypeCreator.CreateInt(5));

            var inequalExpr = new InequalsExpression(var1, var2);
            var equalExpr = new EqualExpression(var1, var2);

            Assert.True(inequalExpr.Execute().Bool);
            Assert.False(equalExpr.Execute().Bool);

            var elseIfStatement = new ElseIfStatement(inequalExpr,
                new AssignStatement(var1, var2));
            elseIfStatement.AddStatement(equalExpr, 
                new AssignStatement(var1, new Constant(TypeCreator.CreateInt(10))));

            elseIfStatement.Execute();

            // after executing the if they should be made equal.
            Assert.False(inequalExpr.Execute().Bool);
            Assert.True(equalExpr.Execute().Bool);

            // The expression x != y is no longer true, else block will be executed.
            elseIfStatement.Execute();

            // after executing the if they should be made equal.
            Assert.True(inequalExpr.Execute().Bool);
            Assert.False(equalExpr.Execute().Bool);

            var str = elseIfStatement.Dump();
            Assert.Equal(str, expectedStr);
        }

        [Fact]
        public void TestElseIfElse()
        {
            var expectedStr =   "if x == 10 then" + StringUtil.NewLineChar +
                                    "x = y" + StringUtil.NewLineChar +
                                "else if x == y then" + StringUtil.NewLineChar +
                                    "x = 9" + StringUtil.NewLineChar +
                                "else" + StringUtil.NewLineChar +
                                    "x = 10" + StringUtil.NewLineChar +
                                "end" + StringUtil.NewLineChar;

            var var1 = new Variable("x", TypeCreator.CreateInt(10));
            var var2 = new Variable("y", TypeCreator.CreateInt(5));

            var equal10= new EqualExpression(var1, new Constant(TypeCreator.CreateInt(10)));
            var equalExpr = new EqualExpression(var1, var2);

            Assert.True(equal10.Execute().Bool);
            Assert.False(equalExpr.Execute().Bool);

            var elseIfElseStatement = new ElseIfElseStatement(equal10,
                new AssignStatement(var1, var2),
                new AssignStatement(var1, new Constant(TypeCreator.CreateInt(10))));
            elseIfElseStatement.AddStatement(equalExpr,
                new AssignStatement(var1, new Constant(TypeCreator.CreateInt(9))));

            elseIfElseStatement.Execute();

            // after executing the if they should be made equal.
            Assert.False(equal10.Execute().Bool);
            Assert.True(equalExpr.Execute().Bool);

            // The expression x == 5 is no longer true, next block will be executed.
            // x == y
            elseIfElseStatement.Execute();

            // after executing the elseif x = 9.
            Assert.False(equal10.Execute().Bool);
            Assert.False(equalExpr.Execute().Bool);

            // The expressions x == 5 and x == y are no longer true,
            // the else block will be executed.
            elseIfElseStatement.Execute();

            // after executing the else x = 5.
            Assert.True(equal10.Execute().Bool);
            Assert.False(equalExpr.Execute().Bool);

            var str = elseIfElseStatement.Dump();
            Assert.Equal(str, expectedStr);
        }
    }
}
