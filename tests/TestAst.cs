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
    }
}
