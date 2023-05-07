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
        public void TestArithmetic()
        {
            var expectedAddStr = "x = y + z" + StringUtil.NewLineChar;
            var var1 = new Variable("x", TypeCreator.CreateInt(0));
            var var2 = new Variable("y", TypeCreator.CreateInt(10));
            var var3 = new Variable("z", TypeCreator.CreateInt(5));
            var assign = new AssignStatement(var1, new AddExpression(var2, var3));
            assign.Execute();
            
            Assert.Equal(15, var1.Content.Int);
            Assert.Equal(expectedAddStr, assign.Dump());

            var expectedSubStr = "x = y - z" + StringUtil.NewLineChar;
            assign = new AssignStatement(var1, new SubExpression(var2, var3));
            assign.Execute();

            Assert.Equal(5, var1.Content.Int);
            Assert.Equal(expectedSubStr, assign.Dump());

            var expectedMulStr = "x = y * z" + StringUtil.NewLineChar;
            assign = new AssignStatement(var1, new MulExpression(var2, var3));
            assign.Execute();

            Assert.Equal(50, var1.Content.Int);
            Assert.Equal(expectedMulStr, assign.Dump());

            var expectedDivStr = "x = y / z" + StringUtil.NewLineChar;
            assign = new AssignStatement(var1, new DivExpression(var2, var3));
            assign.Execute();

            Assert.Equal(2, var1.Content.Int);
            Assert.Equal(expectedDivStr, assign.Dump());

            var expectedPowStr = "x = y ^ z" + StringUtil.NewLineChar;
            assign = new AssignStatement(var1, new PowExpression(var2, var3));
            assign.Execute();

            Assert.Equal(100000, var1.Content.Int);
            Assert.Equal(expectedPowStr, assign.Dump());

            var expectedNegStr = "x = -y" + StringUtil.NewLineChar;
            assign = new AssignStatement(var1, new NegationExpression(var2));
            assign.Execute();

            Assert.Equal(-10, var1.Content.Int);
            Assert.Equal(expectedNegStr, assign.Dump());
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

        [Fact]
        public void TestForLoop()
        {
            var expectedStr = "for x, y, z do" + StringUtil.NewLineChar +
                                    "a = 1" + StringUtil.NewLineChar +
                              "end" + StringUtil.NewLineChar;

            var var1 = new Variable("x", TypeCreator.CreateInt(1));
            var var2 = new Variable("y", TypeCreator.CreateInt(10));
            var var3 = new Variable("z", TypeCreator.CreateInt(2));

            var var4 = new Variable("a", TypeCreator.CreateInt(0));
            var assignStatment = new  AssignStatement(var4, new Constant(TypeCreator.CreateInt(1)));

            var forLoop = new ForStatment(var1, var2, var3, assignStatment);

            Assert.True(var4.Content.Int == 0);
            forLoop.Execute();
            Assert.True(var4.Content.Int == 1);

            var str = forLoop.Dump();
            Assert.Equal(str, expectedStr);
        }
    }
}
