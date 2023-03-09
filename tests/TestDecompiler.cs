namespace Tests
{
    public class TestDecompiler
    {
        [Fact]
        public void TestTest()
        {
            // create dummy func and encode it
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            //luafunc.Instructions.Add(new LuaInstruction(LuaOpcode.RETURN) { B=1 });
            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
            });

            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // decode our dummy
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            Assert.True(test.Contains($"function {decoder.File.Function.Name}()\r\n"),
                "Decompiler gave unexpected Lua code");
        }
        [Fact]
        public void TestIfs()
        {
            // create dummy func and encode it
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            /*
             * function CRoot()
             *     return unk261632() and true or false
             * end
             *
             // Lua bytecode script 
             */
            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.GETGLOBAL)     { A=0, Bx=0 },      // var0 = _G["test"]
                new LuaInstruction(LuaOpcode.CALL)          { A=0, B=1, C=2 },  // var0 = var0()
                new LuaInstruction(LuaOpcode.TEST)          { A=0, B=0, C=0 },  // if var0 then
                new LuaInstruction(LuaOpcode.JMP)           { sBx=2 },          // goto JMP_2
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=0, B=1, C=0 },  // var0 = false
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=2 },       // return var0
                                                                                // --end
                                                                                // --JMP_2:
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=0, B=0, C=0 },  // var0 = false
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=2 },       // return var0
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new StringConstant("test\0"));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "").Replace("\r", "").Replace("\n", "");

            Assert.True(test.Contains($"var0=var0()"),
                "Decompiler failed on `CALL 0 1 2`");
        }


        // We want to make sure to cover all the control flow logic
        // correctly as that is what we care most about.
        //
        // TODO: multi-if
        #region mutliif

        // function test()
        // local a = 1
        // local b = 2
        //
        // if a > b then
        //     return true
        // end
        // return false
        //1	LOADK	0 -1	; 1
        //2	LOADK	1 -2	; 2
        //3	LT	0 1 0	
        //4	JMP	2	to pc 7
        //5	LOADBOOL	2 1 0	
        //6	RETURN	2 2	
        //7	LOADBOOL	2 0 0	
        //8	RETURN	2 2	
        //9	RETURN	0 1
        [Fact]
        public void TestIf2()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-1 },     // var0 = 1
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-2 },     // var1 = 2
                new LuaInstruction(LuaOpcode.LT)            { A=0, B=1, C=0 },  // if var0 > var1 then
                new LuaInstruction(LuaOpcode.JMP)           { sBx=2 },          // goto JMP_2
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=2, B=1, C=0 },  // var2 = true
                new LuaInstruction(LuaOpcode.RETURN)        { A=2, B=2 },       // return var2
                                                                                // JMP_2:
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=0, B=0, C=0 },  // var2 = false
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=2 },       // return var2
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new NumberConstant(2));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");
            //Console.WriteLine(test);

            Assert.True(test.Contains($"ifvar1<var0then"),
                "Decompiler failed parsing single if");
            Assert.True(test.Contains("end"),
                "Decompiler failed adding end for single if");
        }
        #endregion multiif

        string input = @"
local a = 1
local b = 2

if a > b then
	return true
elseif a == b then
	return nil
end
return false";
        [Fact]
        public void TestIfElseIf()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-1 },     // var0 = 1
                new LuaInstruction(LuaOpcode.LOADK)         { A=1, Bx=-2 },     // var1 = 2
                new LuaInstruction(LuaOpcode.LT)            { A=0, B=1, C=0 },  // if var0 < var1 then
                new LuaInstruction(LuaOpcode.JMP)           { sBx=3 },          // goto +3
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=2, B=1, C=0 },  // var2 = true
                new LuaInstruction(LuaOpcode.RETURN)        { A=2, B=2 },       // return var2
                new LuaInstruction(LuaOpcode.JMP)           { sBx=4 },          // goto +4
                                                                                // _L0:
                new LuaInstruction(LuaOpcode.EQ)            { A=0, B=0, C=1 },  // if var0==var1 then
                new LuaInstruction(LuaOpcode.JMP)           { sBx=2 },          // goto +2
                new LuaInstruction(LuaOpcode.LOADNIL)       { A=2, B=2 },       // var2=nil
                new LuaInstruction(LuaOpcode.RETURN)        { A=2, B=2 },       // return var2
                                                                                // _L1:
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=2, B=0, C=0 },  // var2 = false
                new LuaInstruction(LuaOpcode.RETURN)        { A=2, B=2 },       // return var2
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new NumberConstant(2));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
            
            Assert.True(test.Contains("ifvar1<var0then"),
                "Decompiled failed to locate start of if statement");
            Assert.True(test.Contains("elseifvar0==var1then"),
                "Decompiled failed to locate start of if-else statement");
            Assert.True(test.Contains("returnvar2end"),
                "Decompiled failed to locate start of if-else statement"); 
        }
        [Fact]
        public void TestEmptyIf()
        {
            string inputstr = @"
if dummy() then
end

print(""123"")
return true";
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.GETGLOBAL)     { A=0, Bx=-1 },     // var0 = _G["dummy"]
                new LuaInstruction(LuaOpcode.CALL)          { A=0, B=1, C=2 },  // var1 = var0()
                new LuaInstruction(LuaOpcode.TEST)          { A=0, B=0, C=0 },  // 
                new LuaInstruction(LuaOpcode.JMP)           { sBx=0 },          // goto +0
                                                                                // _L0:
                new LuaInstruction(LuaOpcode.GETGLOBAL)     { A=2, Bx=-2 },     // var2 = _G["print"]
                new LuaInstruction(LuaOpcode.LOADK)         { A=1, Bx=-3 },     // var1 = "123"
                new LuaInstruction(LuaOpcode.CALL)          { A=0, B=1, C=0 },  // var0(var1)
                                                                                // _L0:
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=0, B=1, C=0 },  // var3 = false
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=2 },       // return var2
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new StringConstant("dummy\0"));
            luacin.Function.Constants.Add(new StringConstant("123\0"));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

            Assert.True(test.Contains("ifvar0thenend"),
                "Decompiler failed to close empty if statement");
            //Assert.True(test.Contains("elseifvar0==var1then"),
            //    "Decompiled failed to locate start of if-else statement");
            //Assert.True(test.Contains("returnvar2end"),
            //    "Decompiled failed to locate start of if-else statement");
        }
        [Fact]
        public void TestIfIf()
        {
            string inputstr = @"local var0 = 1
local var1 = 2
if var1 < var0 then
  local var2 = 2
  if var2 > var0 then
  	return true
  end
end
";
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-1 },     // var0 = 1
                new LuaInstruction(LuaOpcode.LOADK)         { A=1, Bx=-2 },     // var1 = 2
                new LuaInstruction(LuaOpcode.LT)            { A=0, B=1, C=0 },  // if var0 < var1 then
                new LuaInstruction(LuaOpcode.JMP)           { sBx=5 },          // goto +5
                new LuaInstruction(LuaOpcode.LOADK)         { A=2, Bx=-2 },     // var2 = 2
                new LuaInstruction(LuaOpcode.LT)            { A=0, B=0, C=2 },  // if var0 < var2 then
                new LuaInstruction(LuaOpcode.JMP)           { sBx=2 },          // goto +2
                                                                                // _L0:
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=3, B=1, C=0 },  // var3 = false
                                                                                // _L1:
                new LuaInstruction(LuaOpcode.RETURN)        { A=3, B=2 },       // return var3
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new NumberConstant(2));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

            Assert.True(test.Contains("ifvar1<var0andvar0<var12then"),
                "Decompilation failed on multi-if statement");
            //Assert.True(test.Contains("elseifvar0==var1then"),
            //    "Decompiled failed to locate start of if-else statement");
            //Assert.True(test.Contains("returnvar2end"),
            //    "Decompiled failed to locate start of if-else statement");
        }
        [Fact]
        public void TestIfIf2()
        {
            string inputstr = @"local var0 = 1
local var1 = 2
if var1 < var0 then
  local var2 = 2
  if var2 > var0 then
  	return true
  end
  var2 = 1
end
";
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-1 },     // var0 = 1
                new LuaInstruction(LuaOpcode.LOADK)         { A=1, Bx=-2 },     // var1 = 2
                new LuaInstruction(LuaOpcode.LT)            { A=0, B=1, C=0 },  // if var0 < var1 then
                new LuaInstruction(LuaOpcode.JMP)           { sBx=6 },          // goto +6
                new LuaInstruction(LuaOpcode.LOADK)         { A=2, Bx=-2 },     // var2 = 2
                new LuaInstruction(LuaOpcode.LT)            { A=0, B=0, C=2 },  // if var0 < var2 then
                new LuaInstruction(LuaOpcode.JMP)           { sBx=2 },          // goto +2
                                                                                // _L0:
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=3, B=1, C=0 },  // var3 = false
                                                                                // _L1:
                new LuaInstruction(LuaOpcode.RETURN)        { A=3, B=2 },       // return var3
                new LuaInstruction(LuaOpcode.LOADK)         { A=2, Bx=-1 },     // var2 = 1
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new NumberConstant(2));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

            Assert.True(test.Contains("ifvar1<var0then"),
                "Decompiled failed to locate start of first if-statement");
            Assert.True(test.Contains("returnvar3endend"),
                "Decompiled failed to end double if-statements");
        }
        [Fact]
        public void TestIfIf3()
        {
            string inputstr = @"local var0 = 1
local var1 = 2
if var1 < var0 then
  local var2 = 2
  if var2 > var0 then
  	return true
  end
  return true
end
";
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-1 },     // var0 = 1
                new LuaInstruction(LuaOpcode.LOADK)         { A=1, Bx=-2 },     // var1 = 2
                new LuaInstruction(LuaOpcode.LT)            { A=0, B=1, C=0 },  // if var0 < var1 then
                new LuaInstruction(LuaOpcode.JMP)           { sBx=7 },          // goto +7
                new LuaInstruction(LuaOpcode.LOADK)         { A=2, Bx=-2 },     // var2 = 2
                new LuaInstruction(LuaOpcode.LT)            { A=0, B=0, C=2 },  // if var0 < var2 then
                new LuaInstruction(LuaOpcode.JMP)           { sBx=2 },          // goto +2
                                                                                // _L0:
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=3, B=1, C=0 },  // var3 = false     
                new LuaInstruction(LuaOpcode.RETURN)        { A=3, B=2 },       // return var3
                                                                                // _L1:
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=3, B=1, C=0 },  // var3 = false    
                new LuaInstruction(LuaOpcode.RETURN)        { A=3, B=2 },       // return
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new NumberConstant(2));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

            Assert.True(test.Contains("ifvar1<var0then"),
                "Decompiled failed to locate start of first if-statement");
            Assert.True(test.Contains("returnvar3endend"),
                "Decompiled failed to end double if-statements");

            // check for too many
            Assert.False(test.Contains("returnvar3endendend"),
                "Decompiled failed, too many 'end' on double if-statements");
        }
        [Fact]
        public void TestIfInIfIf()
        {
            string inputstr = @"
if START then
  if A == 1 then
    return 1
  end
  if B == 1 then
  	return 2
  end
end
";
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.GETGLOBAL)     { A=0, Bx=-1 },     // _G["START"]
                new LuaInstruction(LuaOpcode.TEST)          { A=0, B=0, C=0 },  // 
                new LuaInstruction(LuaOpcode.JMP)           { sBx=10 },         // +10 
                new LuaInstruction(LuaOpcode.GETGLOBAL)     { A=0, Bx=-2 },     // var0 = _G["A"]
                new LuaInstruction(LuaOpcode.EQ)            { A=0, B=0, C=3 }, // if var0 == 1 then 
                new LuaInstruction(LuaOpcode.JMP)           { sBx=2 },          // +2 
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-3 },      // var0 = 2 
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=2 },       // 
                new LuaInstruction(LuaOpcode.GETGLOBAL)     { A=0, Bx=-4 },      // var0 = _G
                new LuaInstruction(LuaOpcode.EQ)            { A=0, B=0, C=3 }, // if var0 == 1 then 
                new LuaInstruction(LuaOpcode.JMP)           { sBx=2 },          // +2 
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-5 },      // var0 = 2
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=2 },       // return var0
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // 
            });
            luacin.Function.Constants.Add(new StringConstant("START"));
            luacin.Function.Constants.Add(new StringConstant("A"));
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new StringConstant("B"));
            luacin.Function.Constants.Add(new NumberConstant(2));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

            //Assert.True(test.Contains("ifvar1<var0andvar0<var12then"),
            Assert.True(test.Contains("returnvar0endendend"),
                "Decompilation failed on multi-if statement");
            //Assert.True(test.Contains("elseifvar0==var1then"),
            //    "Decompiled failed to locate start of if-else statement");
            //Assert.True(test.Contains("returnvar2end"),
            //    "Decompiled failed to locate start of if-else statement");
        }
        [Fact]
        public void TestIfElse()
        {

            //1   LOADK   0 - 1; 1
            //2   LOADK   1 - 2; 2
            //3   LT  0 1 0
            //4   JMP 3   to pc 8
            //5   LOADBOOL    2 1 0
            //6   RETURN  2 2
            //7   JMP 2   to pc 10
            //8   LOADNIL 2 2
            //9   RETURN  2 2
            //10  LOADBOOL    2 0 0
            //11  RETURN  2 2
            //12  RETURN  0 1'
            string tessct = @"local var0 = 1
local var1 = 2
if var1 < var0 then
  return true
else
  return nil
end
return false";

            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-1 },     // var0 = 1
                new LuaInstruction(LuaOpcode.LOADK)         { A=1, Bx=-2 },     // var1 = 2
                new LuaInstruction(LuaOpcode.LT)            { A=0, B=1, C=0 },  // if var0 < var1 then
                new LuaInstruction(LuaOpcode.JMP)           { sBx=3 },          // goto +3
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=2, B=1, C=0 },  // var2 = true
                new LuaInstruction(LuaOpcode.RETURN)        { A=2, B=2 },       // return var2
                                                                                //
                new LuaInstruction(LuaOpcode.JMP)           { sBx=2 },          // goto +2
                new LuaInstruction(LuaOpcode.LOADNIL)       { A=2, B=2 },       // var2=nil
                new LuaInstruction(LuaOpcode.RETURN)        { A=2, B=2 },       // return var2
                                                                                // :
                new LuaInstruction(LuaOpcode.LOADBOOL)      { A=2, B=0, C=0 },  // var2 = false
                new LuaInstruction(LuaOpcode.RETURN)        { A=2, B=2 },       // return var2
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new NumberConstant(2));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");
            
            Assert.True(test.Contains("ifvar1<var0then"),
                "Decompiled failed to locate start of if statement");
            Assert.True(test.Contains("elsevar2=nil;"),
                "Decompiled failed to locate else statement");
            Assert.True(test.Contains("returnvar2end"),
                "Decompiled failed to locate start of if-else statement"); 
        }

        // TODO: while
        #region while
        string input2 = @"
local a = 0
local b = 10
while a < b do
    a = a + 1
    if a == 5 then
        break
    end    
end";
        //1	LOADK	0 -1	; 0
        //2	LOADK	1 -2	; 10
        //3	LT	0 0 1	
        //4	JMP	5	to pc 10
        //5	ADD	0 0 -3	; - 1
        //6	EQ	0 0 -4	; - 5
        //7	JMP	-5	to pc 3
        //8	JMP	1	to pc 10
        //9	JMP	-7	to pc 3
        //10	RETURN	0 1
        #endregion

        // TODO: do/until
        #region dountil
        string input3 = @"
local a = 0
local b = 10
repeat
   a = a + 1  
until (a == b)
";
        //1	LOADK	0 -1	; 0
        //2	LOADK	1 -2	; 10
        //3	ADD	0 0 -3	; - 1
        //4	EQ	0 0 1	
        //5	JMP	-3	to pc 3
        //6	RETURN	0 1
        // NOTE: statement + jump negative = 'until', JMP target is 'repeat'
        [Fact]
        public void TestRepeat()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-1 },     // var0 = 0
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-2 },     // var1 = 10
                                                                                // repeat
                new LuaInstruction(LuaOpcode.ADD)           { A=0, B=0, C=-3 }, // var0 = var0 + 1
                new LuaInstruction(LuaOpcode.EQ)            { A=0, B=0, C=1 },  // until var0 == var1
                new LuaInstruction(LuaOpcode.JMP)           { sBx=-3 },          // JMP -3
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new NumberConstant(0));
            luacin.Function.Constants.Add(new NumberConstant(10));
            luacin.Function.Constants.Add(new NumberConstant(-1));


            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");
            //Console.WriteLine(test);

            Assert.True(test.Contains($"ifvar1<var0then"),
                "Decompiler failed parsing single if");
            Assert.True(test.Contains("end"),
                "Decompiler failed adding end for single if");
        }
        #endregion
        //

        [Fact]
        public void TestIfFor()
        {
            // create dummy func and encode it
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            string inputstr = @"local var0 = 1
if true then
  for i=1, 100 do
	var0 = i
  end
end
return var0";

/* 
function CRoot()
	var0 = "unk262143"
	var1 = _G["unk262142"]
	if var0 == var0 then
		var1 = "unk262143"
		var2 = "unk262142"
		var3 = "unk262143"
	end -- _0
	for var4=var1, var2, var3 do
		var0 = var4
	end -- -1
	return var0
end -- x
*/

            // NOTE: This one is cucked rly bad?
            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-1 },     // var0 = 1
                new LuaInstruction(LuaOpcode.GETGLOBAL)     { A=1, Bx=-2 },     // var0 = _G["GG"]
                new LuaInstruction(LuaOpcode.EQ)            { A=0, B=0, C=0 },  // if 
                new LuaInstruction(LuaOpcode.JMP)           { sBx=6 },          // goto +6 

                new LuaInstruction(LuaOpcode.LOADK)         { A=1, Bx=-1 },     // var1 = 1
                new LuaInstruction(LuaOpcode.LOADK)         { A=2, Bx=-2 },     // var2 = 100
                new LuaInstruction(LuaOpcode.LOADK)         { A=3, Bx=-1 },     // var2 = 1
                new LuaInstruction(LuaOpcode.FORPREP)       { A=1, B=1 },       // for 
                                                                                // _L2:
                new LuaInstruction(LuaOpcode.MOVE)          { A=0, B=4 },       //
                new LuaInstruction(LuaOpcode.FORLOOP)       { A=1, Bx=-2 },     // var2 = var4()
                                                                                // _L1:
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=2 },       // goto loop
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new StringConstant("GG\0"));
            luacin.Function.Constants.Add(new NumberConstant(100));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(true);
            // test = test.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

            Assert.True(test.Contains($"endreturnvar0endend"),
                "Decompiler failed to find end of shared if + for statement");

            // check too many
            Assert.False(test.Contains($"endreturnvar0endendend"),
               "Decompiler failed, too many end on shared if + for statement");

        }
        [Fact]
        public void TestFor()
        {
            // create dummy func and encode it
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new LuaFunction() { ArgsCount = 0 };

            // -- Lua bytecode script 
            // function CRoot()
            //     for i=1, 100 do
            //         print(i)
            //     end
            // end
            //
            luacin.Function.Instructions.AddRange(new LuaInstruction[]
            {
                new LuaInstruction(LuaOpcode.LOADK)         { A=0, Bx=-1 },     // var0 = 1
                new LuaInstruction(LuaOpcode.LOADK)         { A=1, Bx=-2 },     // var1 = 100
                new LuaInstruction(LuaOpcode.LOADK)         { A=2, Bx=-1 },     // var2 = 1
                new LuaInstruction(LuaOpcode.FORPREP)       { A=0, B=3 },       // for 
                                                                                // loop:
                new LuaInstruction(LuaOpcode.GETGLOBAL)     { A=4, Bx=-3 },     // var4 = _G["print"]
                new LuaInstruction(LuaOpcode.MOVE)          { A=5, B=3 },       // var5 = var3
                new LuaInstruction(LuaOpcode.CALL)          { A=4, B=2, C=1 },  // var2 = var4()
                new LuaInstruction(LuaOpcode.FORLOOP)       { A=0, Bx=-4 },     // goto loop
                new LuaInstruction(LuaOpcode.RETURN)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new NumberConstant(100));
            luacin.Function.Constants.Add(new StringConstant("print\0"));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(true);
            test = test.Replace(" ", "").Replace("\t", "");

            Assert.True(test.Contains($"forvar3=var0,var1,var2do"),
                "Decompiler failed on FORPREP");
            Assert.True(test.Contains($"end"),
                "Decompiler failed closing FORLOOP");
            
        }

        [Fact]
        public void test2()
        {
            LuaCFile f = new LuaCFile(File.ReadAllBytes("C:\\Users\\sande\\Downloads\\dumped_lua.luac"));
            LuaDecoder d = new LuaDecoder(f);
            LuaDecompiler dec = new LuaDecompiler(d);
            string test = dec.Decompile();

        }
    }
}