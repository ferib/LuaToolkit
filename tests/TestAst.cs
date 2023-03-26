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
    }
}
