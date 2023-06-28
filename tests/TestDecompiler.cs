using LuaToolkit.Disassembler;
using NumberConstant = LuaToolkit.Disassembler.NumberByteConstant;
using StringConstant = LuaToolkit.Disassembler.StringByteConstant;

namespace Tests
{
    public class TestDecompiler
    {
        [Fact]
        public void TestTest()
        {
            // create dummy func and encode it
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            //luafunc.Instructions.Add(new ReturnInstruction(0) { B=1 });
            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new ReturnInstruction(1)        { A=0, B=1 },       // return
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
            luacin.Function = new Function() { ArgsCount = 0 };

            /*
             * function CRoot()
             *     return unk261632() and true or false
             * end
             *
             // Lua bytecode script 
             */
            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new GetGlobalInstruction(1)     { A=0, Bx=0 },      // var0 = _G["test"]
                new CallInstruction(2)          { A=0, B=1, C=2 },  // var0 = var0()
                new TestInstruction(3)          { A=0, B=0, C=0 },  // if var0 then
                new JmpInstruction(4)           { sBx=2 },          // goto JMP_2
                new LoadBoolInstruction(5)      { A=0, B=1, C=0 },  // var0 = false
                new ReturnInstruction(6)        { A=0, B=2 },       // return var0
                                                                                // --end
                                                                                // --JMP_2:
                new LoadBoolInstruction(7)      { A=0, B=0, C=0 },  // var0 = false
                new ReturnInstruction(8)        { A=0, B=2 },       // return var0
                new ReturnInstruction(9)        { A=0, B=1 },       // return
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
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)         { A=0, Bx=-1 },     // var0 = 1
                new LoadKInstruction(2)         { A=0, Bx=-2 },     // var1 = 2
                new LtInstruction(3)            { A=0, B=1, C=0 },  // if var0 > var1 then
                new JmpInstruction(4)           { sBx=2 },          // goto JMP_2
                new LoadBoolInstruction(5)      { A=2, B=1, C=0 },  // var2 = true
                new ReturnInstruction(6)        { A=2, B=2 },       // return var2
                                                                                // JMP_2:
                new LoadBoolInstruction(7)      { A=0, B=0, C=0 },  // var2 = false
                new ReturnInstruction(8)        { A=0, B=2 },       // return var2
                new ReturnInstruction(9)        { A=0, B=1 },       // return
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

        [Fact]
        public void TestIfOr()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadBoolInstruction(1) { A=0, B=1 },
                new LoadBoolInstruction(2) { A=1, B=0 },
                new TestInstruction(3) { A=0, B=0, C=0 }, // if start
                new JmpInstruction(4) { sBx=2 },
                new TestInstruction(5) { A=1, B=0, C=1 },
                new JmpInstruction(6) { sBx=2 },
                new LoadBoolInstruction(7) { A=0, B=0 },
                new ReturnInstruction(8) { A=0, B=0 }, // if end
                new ReturnInstruction(9) { A=0, B=1 }
            });

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains($"ifvar0ornotvar1then"),
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
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)         { A=0, Bx=-1 },     // var0 = 1
                new LoadKInstruction(2)         { A=1, Bx=-2 },     // var1 = 2
                new LtInstruction(3)            { A=0, B=1, C=0 },  // if var0 < var1 then
                new JmpInstruction(4)           { sBx=3 },          // goto +3
                new LoadBoolInstruction(5)      { A=2, B=1, C=0 },  // var2 = true
                new ReturnInstruction(6)        { A=2, B=2 },       // return var2
                new JmpInstruction(7)           { sBx=4 },          // goto +4
                                                                                // _L0:
                new EqInstruction(8)            { A=0, B=0, C=1 },  // if var0==var1 then
                new JmpInstruction(9)           { sBx=2 },          // goto +2
                new LoadNilInstruction(10)       { A=2, B=2 },       // var2=nil
                new ReturnInstruction(11)        { A=2, B=2 },       // return var2
                                                                                // _L1:
                new LoadBoolInstruction(12)      { A=2, B=0, C=0 },  // var2 = false
                new ReturnInstruction(13)        { A=2, B=2 },       // return var2
                new ReturnInstruction(14)        { A=0, B=1 },       // return
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
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new GetGlobalInstruction(1)     { A=0, Bx=-1 },     // var0 = _G["dummy"]
                new CallInstruction(2)          { A=0, B=1, C=2 },  // var1 = var0()
                new TestInstruction(3)          { A=0, B=0, C=0 },  // 
                new JmpInstruction(4)           { sBx=0 },          // goto +0
                                                                                // _L0:
                new GetGlobalInstruction(5)     { A=2, Bx=-2 },     // var2 = _G["print"]
                new LoadKInstruction(6)         { A=1, Bx=-3 },     // var1 = "123"
                new CallInstruction(7)          { A=0, B=1, C=0 },  // var0(var1)
                                                                                // _L0:
                new LoadBoolInstruction(8)      { A=0, B=1, C=0 },  // var3 = false
                new ReturnInstruction(9)        { A=0, B=2 },       // return var2
                new ReturnInstruction(10)        { A=0, B=1 },       // return
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
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)         { A=0, Bx=-1 },     // var0 = 1
                new LoadKInstruction(2)         { A=1, Bx=-2 },     // var1 = 2
                new LtInstruction(3)            { A=0, B=1, C=0 },  // if var0 < var1 then
                new JmpInstruction(4)           { sBx=5 },          // goto +5
                new LoadKInstruction(5)         { A=2, Bx=-2 },     // var2 = 2
                new LtInstruction(6)            { A=0, B=0, C=2 },  // if var0 < var2 then
                new JmpInstruction(7)           { sBx=2 },          // goto +2
                                                                                // _L0:
                new LoadBoolInstruction(8)      { A=3, B=1, C=0 },  // var3 = false
                                                                                // _L1:
                new ReturnInstruction(9)        { A=3, B=2 },       // return var3
                new ReturnInstruction(10)        { A=0, B=1 },       // return
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
                "Decompilation failed on multi-if statement");
            Assert.True(test.Contains("ifvar0<var2then"),
                "Decompiled failed to locate start of if-else statement");
            Assert.True(test.Contains("returnvar3endend"),
                "Decompiled failed to locate start of if-else statement");
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
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)         { A=0, Bx=-1 },     // var0 = 1
                new LoadKInstruction(2)         { A=1, Bx=-2 },     // var1 = 2
                new LtInstruction(3)            { A=0, B=1, C=0 },  // if var0 < var1 then
                new JmpInstruction(4)           { sBx=6 },          // goto +6
                new LoadKInstruction(5)         { A=2, Bx=-2 },     // var2 = 2
                new LtInstruction(6)            { A=0, B=0, C=2 },  // if var0 < var2 then
                new JmpInstruction(7)           { sBx=2 },          // goto +2
                                                                                // _L0:
                new LoadBoolInstruction(8)      { A=3, B=1, C=0 },  // var3 = false
                                                                                // _L1:
                new ReturnInstruction(9)        { A=3, B=2 },       // return var3
                new LoadKInstruction(10)         { A=2, Bx=-1 },     // var2 = 1
                new ReturnInstruction(11)        { A=0, B=1 },       // return
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
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)         { A=0, Bx=-1 },     // var0 = 1
                new LoadKInstruction(2)         { A=1, Bx=-2 },     // var1 = 2
                new LtInstruction(3)            { A=0, B=1, C=0 },  // if var0 < var1 then
                new JmpInstruction(4)           { sBx=7 },          // goto +7
                new LoadKInstruction(5)         { A=2, Bx=-2 },     // var2 = 2
                new LtInstruction(6)            { A=0, B=0, C=2 },  // if var0 < var2 then
                new JmpInstruction(7)           { sBx=2 },          // goto +2
                                                                                // _L0:
                new LoadBoolInstruction(8)      { A=3, B=1, C=0 },  // var3 = false     
                new ReturnInstruction(9)        { A=3, B=2 },       // return var3
                                                                                // _L1:
                new LoadBoolInstruction(10)      { A=3, B=1, C=0 },  // var3 = false    
                new ReturnInstruction(11)        { A=3, B=2 },       // return
                new ReturnInstruction(12)        { A=0, B=1 },       // return
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
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new GetGlobalInstruction(1)     { A=0, Bx=-1 },     // _G["START"]
                new TestInstruction(2)          { A=0, B=0, C=0 },  // 
                new JmpInstruction(3)           { sBx=10 },         // +10 
                new GetGlobalInstruction(4)     { A=0, Bx=-2 },     // var0 = _G["A"]
                new EqInstruction(5)            { A=0, B=0, C=-3 }, // if var0 == 1 then 
                new JmpInstruction(6)           { sBx=2 },          // +2 
                new LoadKInstruction(7)         { A=0, Bx=-3 },      // var0 = 2 
                new ReturnInstruction(8)        { A=0, B=2 },       // 
                new GetGlobalInstruction(9)     { A=0, Bx=-4 },      // var0 = _G
                new EqInstruction(10)            { A=0, B=0, C=-3 }, // if var0 == 1 then 
                new JmpInstruction(11)           { sBx=2 },          // +2 
                new LoadKInstruction(12)         { A=0, Bx=-5 },      // var0 = 2
                new ReturnInstruction(13)        { A=0, B=2 },       // return var0
                new ReturnInstruction(14)        { A=0, B=1 },       // 
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
            Assert.True(test.Contains("returnvar0endend"),
                "Decompilation failed on multi-if statement");
            Assert.True(test.Contains("ifvar0==1then"),
                "Decompiled failed to locate start of if-else statement");
            Assert.True(test.Contains("returnvar0end"),
                "Decompiled failed to locate start of if-else statement");
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
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)         { A=0, Bx=-1 },     // var0 = 1
                new LoadKInstruction(2)         { A=1, Bx=-2 },     // var1 = 2
                new LtInstruction(3)            { A=0, B=1, C=0 },  // if var0 < var1 then
                new JmpInstruction(4)           { sBx=3 },          // goto +3
                new LoadBoolInstruction(5)      { A=2, B=1, C=0 },  // var2 = true
                new ReturnInstruction(6)        { A=2, B=2 },       // return var2
                                                                                //
                new JmpInstruction(7)           { sBx=2 },          // goto +2
                new LoadNilInstruction(8)       { A=2, B=2 },       // var2=nil
                new ReturnInstruction(9)        { A=2, B=2 },       // return var2
                                                                                // :
                new LoadBoolInstruction(10)      { A=2, B=0, C=0 },  // var2 = false
                new ReturnInstruction(11)        { A=2, B=2 },       // return var2
                new ReturnInstruction(12)        { A=0, B=1 },       // return
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
            Assert.True(test.Contains("elsevar2=nil"),
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
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)         { A=0, Bx=-1 },     // var0 = 0
                new LoadKInstruction(2)         { A=0, Bx=-2 },     // var1 = 10
                                                                                // repeat
                new AddInstruction(3)           { A=0, B=0, C=-3 }, // var0 = var0 + 1
                new EqInstruction(4)            { A=0, B=0, C=1 },  // until var0 == var1
                new JmpInstruction(5)           { sBx=-3 },          // JMP -3
                new ReturnInstruction(6)        { A=0, B=1 },       // return
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

            Assert.True(test.Contains($"repeat"),
                "Decompiler failed parsing repeat");
            Assert.True(test.Contains("untilvar0==var1"),
                "Decompiler failed adding end repeat");
        }

        [Fact]
        public void TestRepeatAnd()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadBoolInstruction(1)         { A=0, B=0, C=0 },   // var0 = false
                new LoadBoolInstruction(2)         { A=1, B=0, C=0 },   // var1 = false
                                                                        // repeat body
                new AddInstruction(3)           { A=0, B=0, C=-3 },     // var0 = var0 + 1
                new TestInstruction(4)          { A=0, B=0, C=0 },      // condition begin
                new JmpInstruction(5)           { sBx=-3 },             // 
                new TestInstruction(6)          { A=1, B=0, C=0 },      // 
                new JmpInstruction(7)           { sBx=-5 },             // condition end
                new ReturnInstruction(8)        { A=0, B=1 },           // return
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

            Assert.True(test.Contains($"repeat"),
                "Decompiler failed parsing repeat");
            Assert.True(test.Contains("untilvar0andvar1"),
                "Decompiler failed adding end repeat");
        }
        #endregion
        //

        [Fact]
        public void TestIfFor()
        {
            // create dummy func and encode it
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

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
            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)         { A=0, Bx=-1 },     // var0 = 1
                new GetGlobalInstruction(2)     { A=1, Bx=-2 },     // var0 = _G["GG"]
                new EqInstruction(3)            { A=0, B=0, C=0 },  // if 
                new JmpInstruction(4)           { sBx=6 },          // goto +6 

                new LoadKInstruction(5)         { A=1, Bx=-1 },     // var1 = 1
                new LoadKInstruction(6)         { A=2, Bx=-2 },     // var2 = 100
                new LoadKInstruction(7)         { A=3, Bx=-1 },     // var2 = 1
                new ForPrepInstruction(8)       { A=1, sBx=1 },       // for 
                                                                                // _L2:
                new MoveInstruction(9)          { A=0, B=4 },       //
                new ForLoopInstruction(10)       { A=1, sBx=-2 },     // var2 = var4()
                                                                                // _L1:
                new ReturnInstruction(11)        { A=0, B=2 },       // goto loop
                new ReturnInstruction(12)        { A=0, B=1 },       // return
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new StringConstant("GG"));
            luacin.Function.Constants.Add(new NumberConstant(100));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(true);
            test = test.Replace(" ", "").Replace("\t", "").Replace("\n", "").Replace("\r", "");

            Assert.True(test.Contains($"endendreturnvar0returnend"),
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
            luacin.Function = new Function() { ArgsCount = 0 };

            // -- Lua bytecode script 
            // function CRoot()
            //     for i=1, 100 do
            //         print(i)
            //     end
            // end
            //
            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)         { A=0, Bx=-1 },     // var0 = 0
                new LoadKInstruction(2)         { A=1, Bx=-2 },     // var1 = 1
                new LoadKInstruction(3)         { A=2, Bx=-1 },     // var2 = 2
                new MoveInstruction(4)          { A=3, B= 0 },     // var3 = var0
                new MoveInstruction(5)          { A=4, B= 1 },     // var4 = var1
                new MoveInstruction(6)          { A=5, B= 2 },     // var5 = var2
                new ForPrepInstruction(7)       { A=3, sBx=1 },       // for 
                                                                                // loop:
                new LoadKInstruction(8)         { A=7, Bx=-4 },
                new ForLoopInstruction(9)       { A=3, sBx=-2 },     // goto loop
                new ReturnInstruction(10)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new NumberConstant(0));
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new NumberConstant(2));
            luacin.Function.Constants.Add(new NumberConstant(3));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(true);
            test = test.Replace(" ", "").Replace("\t", "");

            Assert.True(test.Contains($"forvar6=var3,var4,var5do"),
                "Decompiler failed on FORPREP");
            Assert.True(test.Contains($"end"),
                "Decompiler failed closing FORLOOP");
            
        }

        [Fact]
        public void TestTFor()
        {
            // create dummy func and encode it
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            // -- Lua bytecode script 
            // function CRoot()
            //     for i=1, 100 do
            //         print(i)
            //     end
            // end
            //
            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)          { A=0, Bx=-1 },      // var0 = 0
                new LoadKInstruction(2)          { A=1, Bx=-1 },      // var1 = 0
                new GetGlobalInstruction(3)      { A=2, Bx=-2 },      // var2 = 0
                new MoveInstruction(4)           { A=3, B=0 },        // var3 = var0
                new CallInstruction(5)           { A=2, B=2, C=4 },   // var2()
                new JmpInstruction(6)            { sBx=3 },           // Jump 10 
                new LoadKInstruction(7)          { A=8, Bx=-1 },      // var8 = 0 (loop body)
                new LoadKInstruction(8)          { A=5, Bx=-1 },      // var5 = 0 (loop body)
                new LoadKInstruction(9)          { A=6, Bx=-1 },      // var6 = . (loop body)
                new TForLoopInstruction(10)      { A=2, C=3 },        // 
                new JmpInstruction(11)           { sBx=-5 },          // Jump 5 
                new ReturnInstruction(12)        { A=0, B=1 },        // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new NumberConstant(0));
            luacin.Function.Constants.Add(new NumberConstant(1));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(true);
            test = test.Replace(" ", "").Replace("\t", "");

            Assert.True(test.Contains($"forvar4,var5,var6invar2(var3)do"),
                "Decompiler failed on TFORLOOP");
        }

        [Fact]
        public void TestAdd()
        {
            // var0 = 1
            // var1 = 6 + var0
            // return var1
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)         { A=0, Bx=-1 },     // var0 = 1
                new AddInstruction(2)           { A=1, B=-2, C=0 }, // var1 = var0 + 6
                new ReturnInstruction(3)        { A=1, B=2 },       // return var2
                new ReturnInstruction(4)        { A=0, B=1 },       // return
                                                                                // --end
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new NumberConstant(6));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");
            //Console.WriteLine(test);

            Assert.True(test.Contains($"var1=6+var0"),
                "Decompiler failed parsing add");
        }
        [Fact]
        public void TestGlobal()
        {
            // var0 = _G[1]
            // var0 = _G[2]
            // var1 = 3
            // _G[1] = var1
            // _G[2] = var2

            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new GetGlobalInstruction(1)     { A=0, Bx=-1 },     // var0 = _G[1]
                new GetGlobalInstruction(2)     { A=0, Bx=-2 },     // var0 = _G[2]
                new LoadKInstruction(3)         { A=1, Bx=-3 },     // var1 = 3
                new SetGlobalInstruction(4)     { A=1, Bx=-1 },     // _G[1] = var1
                new SetGlobalInstruction(5)     { A=1, Bx=-2 },     // _G[2] = var2
                new ReturnInstruction(6)        { A=0, B=2 },       // return var0
                new ReturnInstruction(7)        { A=0, B=1 },       // 
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new NumberConstant(2));
            luacin.Function.Constants.Add(new NumberConstant(3));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains($"var0=_G["),
                "Decompiler failed parsing global");
        }

        [Fact]
        public void TestCall()
        {
            // new CallInstruction(0) { A = 0, B = 1, C = 2 },  // var0 = var0()
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new CallInstruction(1)          { A = 0, B = 1, C = 2 },  // var0 = var0()
                new ReturnInstruction(2)        { A=0, B=2 },       // return var0
                new ReturnInstruction(3)        { A=0, B=1 },       // 
            });

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("var0=var0()"),
                "Decompiler failed parsing function call");
        }

        [Fact]
        public void TestTESTInstr()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadBoolInstruction(1)          { A = 0, B = 1, C = 0 },  // var0 = 1
                new TestInstruction(2)              { A = 0, B = 0, C = 0 },  // if var0 then
                new JmpInstruction(3)               { sBx = 1 },              // Jump out of if body
                new ReturnInstruction(4)        { A=0, B=2 },       // return var0
                new ReturnInstruction(5)        { A=0, B=1 },       // 
            });

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("ifvar0then"),
                "Decompiler failed parsing TEST");
        }

        [Fact]
        public void TestTESTInstr_Not()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadBoolInstruction(1)          { A = 0, B = 1, C = 0 },  // var0 = 1
                new TestInstruction(2)              { A = 0, B = 0, C = 1 },  // if var0 then
                new JmpInstruction(3)               { sBx = 1 },              // Jump out of if body
                new ReturnInstruction(4)        { A=0, B=2 },       // return var0
                new ReturnInstruction(5)        { A=0, B=1 },       // 
            });

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("ifnotvar0then"),
                "Decompiler failed parsing TEST");
        }


        [Fact]
        public void TestTESTSET()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadBoolInstruction(1)          { A = 0, B = 1, C = 0 },  // var0 = 1
                new LoadBoolInstruction(2)          { A = 1, B = 0, C = 0 },  // var1 = 0
                new TestSetInstruction(3)           { A = 2, B = 0, C = 1 },  // var2 = var0 or var1
                new JmpInstruction(4)               { sBx = 1 },              // Jump out of if body
                new MoveInstruction(5)              { A = 2, B = 1, C = 0 },  // var2 = var1
                new ReturnInstruction(6)        { A=0, B=2 },       // return var0
                new ReturnInstruction(7)        { A=0, B=1 },       // 
            });

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("var2=var0orvar1"),
                "Decompiler failed parsing TESTSET");
        }

        [Fact]
        public void TestTESTSET_Not()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadBoolInstruction(1)          { A = 0, B = 1, C = 0 },  // var0 = 1
                new LoadBoolInstruction(2)          { A = 1, B = 0, C = 0 },  // var1 = 0
                new TestSetInstruction(3)           { A = 2, B = 0, C = 0 },  // var2 = var0 or var1
                new JmpInstruction(4)               { sBx = 1 },              // Jump out of if body
                new MoveInstruction(5)              { A = 2, B = 1, C = 0 },  // var2 = var1
                new ReturnInstruction(6)        { A=0, B=2 },       // return var0
                new ReturnInstruction(7)        { A=0, B=1 },       // 
            });

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("var2=var0andvar1"),
                "Decompiler failed parsing TESTSET");
        }

        [Fact]
        public void TestTable()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new NewTableInstruction(1)          { A = 0, B = 0, C = 0 },  // var0 = {}
                new LoadKInstruction(2)             { A = 1, Bx = -1 },  // var1 = 1
                new LoadKInstruction(3)             { A = 2, Bx = -2 },  // var2 = 5
                new LoadKInstruction(4)             { A = 2, Bx = -5 },  // var2 = 5
                new SetTableInstruction(5)          { A = 0, B = 1, C = 2 },   // var0[var1] = var2
                new GetTableInstruction(6)          { A = 3, B = 0, C = 2 },  // var3 = var0[var2]
                new ReturnInstruction(7)            { A=0, B=1 },       // 
            });
            luacin.Function.Constants.Add(new NumberConstant(1));
            luacin.Function.Constants.Add(new NumberConstant(5));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("var0={}"),
                "Decompiler failed parsing NEWTABLE");
            Assert.True(test.Contains("var0[var1]=var2"),
                "Decompiler failed parsing SETTABLE");
            Assert.True(test.Contains("var3=var0[var2]"),
                "Decompiler failed parsing GETTABLE");
        }

        [Fact]
        public void TestSetList()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                
                new LoadKInstruction(1)             { A = 0, Bx = -1 },  // var0 = 1
                new LoadKInstruction(2)             { A = 1, Bx = -1 },  // var1 = 1
                new NewTableInstruction(3)          { A = 2, B = 2, C = 0 },   // var2 = {}
                new MoveInstruction(4)              { A = 3, B = 0, C = 0 },   // var3 = var0
                new MoveInstruction(5)              { A = 4, B = 1, C = 0 },   // var4 = var1
                new SetListInstruction(6)           { A = 2, B = 2, C = 1 },   // var2 = { var3, var4 }
                new ReturnInstruction(7)            { A=0, B=1 },       // 
            });
            luacin.Function.Constants.Add(new NumberConstant(1));


            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("var2={var3,var4}"),
                "Decompiler failed parsing SETLIST");
        }

        [Fact]
        public void TestUpvals()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new GetUpvalInstruction(1)          { A = 0, B = 0, C = 0 },  // var0 = upval
                new LoadKInstruction(2)             { A = 1, Bx = -1 },  // var1 = 2
                new SetUpvalInstruction(3)          { A = 1, B = 0, C = 0 },   // upval = var1
                new ReturnInstruction(4)            { A=0, B=1 },       // 
            });
            luacin.Function.Constants.Add(new NumberConstant(2));
            luacin.Function.Upvals.Add("upval");


            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("var0=upval"),
                "Decompiler failed parsing GETUPVAL");
            Assert.True(test.Contains("upval=var1"),
                "Decompiler failed parsing SETUPVAL");
        }

        [Fact]
        public void TestVarArg()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new VarArgInstruction(1)            { A = 1, B = 3 },  // var1, var2 = ..
                new ReturnInstruction(2)            { A=0, B=1 },       // 
            });


            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("var1,var2=..."),
                "Decompiler failed parsing VARARG");
            Assert.True(test.Contains("CRoot(...)"),
                "Decompiler failed parsing VARARG");
        }

        [Fact]
        public void TestTailCall()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new TailCallInstruction(1)            { A = 0, B = 1, C = 0 },  // return var0()
                new ReturnInstruction(2)            { A=0, B=0 },       // 
                new ReturnInstruction(3)            { A=0, B=1 },       // 
            });


            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("returnvar0()"),
                "Decompiler failed parsing TailCall");
        }

        [Fact]
        public void TestClosure ()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new ClosureInstruction(1)            { A = 0, Bx = 0 },  // var0 = 
                new ReturnInstruction(2)            { A=0, B=0 },       // 
                new ReturnInstruction(3)            { A=0, B=1 },       // 
            });

            var callee = new Function() { ArgsCount = 0 };
            callee.Name = "Callee";
            callee.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)            { A = 1, Bx = -1 },  // var0 = 
                new ReturnInstruction(2)            { A=0, B=0 },       // 
                new ReturnInstruction(3)            { A=0, B=1 },       // 
            });
            callee.Constants.Add(new NumberConstant(1));

            luacin.Function.Functions.Add(callee);


            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("var0=\"Callee\""),
                "Decompiler failed parsing TailCall");
        }

        [Fact]
        public void TestSelf()
        {
            //	local var0
            //  var0:func()

            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new SelfInstruction(1)            { A = 1, B = 0, C = -1 }, 
                new CallInstruction(2)            { A = 1, B = 2, C = 1 },       // 
                new ReturnInstruction(3)          { A=0, B=1 },       // 
            });

            luacin.Function.Constants.Add(new StringConstant("func"));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            // Test fails because constants don't work.
            Assert.True(test.Contains("var1=var0[\"func\"]"),
                "Decompiler failed parsing SELF");
        }

        [Fact]
        public void TestLen()
        {
            //	local var0 = "test"
            //  local var1 = #var0

            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)            { A = 0, Bx = -1 }, // local var0 = "test"
                new LenInstruction(2)              { A = 1, B = 0 },   // local var1 = #var0 
                new ReturnInstruction(3)           { A=0, B=1 },       // 
            });

            luacin.Function.Constants.Add(new StringConstant("test"));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("var0=\"test\""),
                "Decompiler failed parsing LOADK with string");
            Assert.True(test.Contains("var1=#var0"),
                "Decompiler failed parsing LEN");
        }

        [Fact]
        public void TestConcat()
        {
            //	local var0 = "test"
            //  local var1 = "2"
            //  local var4 = var2 .. var3

            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)            { A = 0, Bx = -1 }, // local var0 = "test"
                new LoadKInstruction(2)            { A = 1, Bx = -2 }, // local var1 = "2"
                new MoveInstruction(3)             { A = 2, B = 0 },  // local var2 = var0
                new MoveInstruction(4)             { A = 3, B = 1 },  // local var3 = var1
                new ConcatInstruction(5)           { A = 2, B = 2, C = 3 },   // local var1 = #var0 
                new ReturnInstruction(6)           { A=0, B=1 },       // 
            });

            luacin.Function.Constants.Add(new StringConstant("test"));
            luacin.Function.Constants.Add(new StringConstant("2"));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("var2=var2..var3"),
                "Decompiler failed parsing CONCAT");
        }

        [Fact]
        public void TestWhile()
        {
            //	local var0 = true
            //  while var0 do
            //      local var1 = false
            //  end

            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadBoolInstruction(1)            { A = 0, B = 1, C = 0},  // local var0 = true
                new TestInstruction(2)                { A = 0, B = 0, C = 0 }, // if var0
                new JmpInstruction(3)                 { sBx = 2 },             // JMP out of loop
                new LoadBoolInstruction(4)            { A = 0, B = 0, C = 0 }, // var0 = false
                new JmpInstruction(5)                 { sBx = -4 },            // JMP Repeat loop 
                new ReturnInstruction(6)              { A=0, B=1 },       // 
            });

            luacin.Function.Constants.Add(new StringConstant("test"));
            luacin.Function.Constants.Add(new StringConstant("2"));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("whilevar0do"),
                "Decompiler failed parsing While loop");
        }

        [Fact]
        public void TestWhileAndCondition()
        {
            //	local var0 = true
            //  while var0 do
            //      local var1 = false
            //  end

            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadBoolInstruction(1)            { A = 0, B = 1, C = 0},  // local var0 = true
                new LoadBoolInstruction(2)            { A = 1, B = 1, C = 0},  // local var1 = true
                new TestInstruction(3)                { A = 0, B = 0, C = 0 }, // if var0
                new JmpInstruction(4)                 { sBx = 4 },             // JMP out of loop
                new TestInstruction(5)                { A = 1, B = 0, C = 0 }, // if var1
                new JmpInstruction(6)                 { sBx = 2 },             // JMP out of loop
                new LoadBoolInstruction(7)            { A = 2, B = 0, C = 0 }, // var2 = false
                new JmpInstruction(8)                 { sBx = -6 },            // while end 
                new ReturnInstruction(9)              { A=0, B=1 },       // 
            });

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("whilevar0andvar1do"),
                "Decompiler failed parsing While with and loop");
        }
        [Fact]
        public void TestWhileOrCondition()
        {
            //	local var0 = true
            //  while var0 do
            //      local var1 = false
            //  end

            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadBoolInstruction(1)            { A = 0, B = 1, C = 0},  // local var0 = true
                new LoadBoolInstruction(2)            { A = 1, B = 1, C = 0},  // local var1 = true
                new TestInstruction(3)                { A = 0, B = 0, C = 0 }, // if var0
                new JmpInstruction(4)                 { sBx = 2 },             // JMP out of loop
                new TestInstruction(5)                { A = 1, B = 0, C = 0 }, // if var1
                new JmpInstruction(6)                 { sBx = 2 },             // JMP out of loop
                new LoadBoolInstruction(7)            { A = 2, B = 0, C = 0 }, // var2 = false
                new JmpInstruction(8)                 { sBx = -6 },            // while end 
                new ReturnInstruction(9)              { A=0, B=1 },       // 
            });

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("whilevar0orvar1do"),
                "Decompiler failed parsing While loop");
        }

        [Fact]
        public void TestLoadK()
        {
            LuaCFile luacin = new LuaCFile(new byte[0]);
            luacin.Function = new Function() { ArgsCount = 0 };

            luacin.Function.Instructions.AddRange(new Instruction[]
            {
                new LoadKInstruction(1)               { A = 0, Bx = -1},       // local var0 = true
                new LoadKInstruction(1)               { A = 1, Bx = -2},       // local var1 = true
                new LoadKInstruction(1)               { A = 2, Bx = -3},       // local var2 = true
                new LoadKInstruction(1)               { A = 3, Bx = -4},       // local var3 = true
            });
            luacin.Function.Constants.Add(new NumberConstant(20));
            luacin.Function.Constants.Add(new NumberConstant(-5));
            luacin.Function.Constants.Add(new StringConstant("test"));
            luacin.Function.Constants.Add(new StringConstant("LongerStringThatShouldAlsoWork"));

            // Encode test
            LuaEncoder luaEncoder_x = new LuaEncoder(luacin);
            byte[] filebuffer = luaEncoder_x.SaveFile();

            // Decode and decompile
            LuaDecoder decoder = new LuaDecoder(new LuaCFile(filebuffer));
            LuaDecompiler decompiler = new LuaDecompiler(decoder);

            string test = decompiler.Decompile(false);
            test = test.Replace(" ", "");

            Assert.True(test.Contains("var0=20"),
                "Decompiler failed parsing LoadK");
            Assert.True(test.Contains("var1=-5"),
                "Decompiler failed parsing LoadK");
            Assert.True(test.Contains("var2=\"test\""),
                "Decompiler failed parsing LoadK");
            Assert.True(test.Contains("var3=\"LongerStringThatShouldAlsoWork\""),
                "Decompiler failed parsing LoadK");
        }


        [Fact]
        public void test2()
        {
            LuaCFile f = new LuaCFile(File.ReadAllBytes("C:\\Users\\ruben\\Downloads\\output.luac"));
            LuaDecoder d = new LuaDecoder(f);
            LuaDecompiler dec = new LuaDecompiler(d);
            string test = dec.Decompile();
        }
    }
}