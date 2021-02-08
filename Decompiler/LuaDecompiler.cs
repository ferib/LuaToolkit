using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using LuaSharpVM.Core;
using LuaSharpVM.Disassembler;
using LuaSharpVM.Models;

namespace LuaSharpVM.Decompiler
{
    public class LuaDecompiler
    {
        private uint FunctionsCount;
        private string Result;
        private byte[] Buffer;

        public LuaDecompiler(byte[] Buffer)
        {
            this.Buffer = Buffer;
        }

		public void Write(LuaFunction function, int indentLevel = 0)
		{
			// top level function
			if (function.FirstLineNr == 0 && function.LastLineNr == 0)
			{
				WriteChildFunctions(function);
				WriteInstructions(function);
			}
			else
			{
				string indents = new string('\t', indentLevel);

				string functionHeader = indents + "function func" + FunctionsCount + "(";

				for (int i = 0; i < function.ArgsCount; ++i)
				{
					functionHeader += "arg" + i + (i + 1 != function.ArgsCount ? ", " : ")");
				}

				this.Result += functionHeader;
				//writer.Write(functionHeader);
				++FunctionsCount;

				//WriteConstants(function, indentLevel + 1);

				WriteChildFunctions(function, indentLevel + 1);

				WriteInstructions(function, indentLevel + 1);
			}
		}

		private void WriteConstants(LuaFunction function, int indentLevel = 0)
		{
			uint constCount = 0;

			string indents = new string('\t', indentLevel);

			foreach (var c in function.Constants)
			{
				this.Result += "{indents}const{constCount} = {c.ToString()}";
				++constCount;
			}
		}

		private void WriteChildFunctions(LuaFunction function, int indentLevel = 0)
		{
			foreach (var f in function.Functions)
			{
				Write(f, indentLevel + 1);
			}
		}

		private void WriteInstructions(LuaFunction function, int indentLevel = 0)
		{
			string indents = new string('\t', indentLevel);

			foreach (var i in function.Instructions)
			{
				switch (i.OpCode)
				{
					case LuaOpcode.MOVE:
						//writer.WriteLine("{2}var{0} = var{1}", i.A, i.B, indents);
						break;

					case LuaOpcode.LOADK:
						this.Result += $"{indents}var{GetConstant(i.Bx, function)} = {i.A}";
						//writer.WriteLine("{2}var{0} = {1}", i.A, GetConstant(i.Bx, function), indents);
						break;

					case LuaOpcode.LOADBOOL:
						//writer.WriteLine("{2}var{0} = {1}", i.A, (i.B != 0 ? "true" : "false"), indents);
						break;

					case LuaOpcode.LOADNIL:
						//for (int x = i.A; x < i.B + 1; ++x)
						//	writer.WriteLine("{1}var{0} = nil", x, indents);
						break;

					case LuaOpcode.GETUPVAL:
						//writer.WriteLine("{2}var{0} = upvalue[{1}]", i.A, i.B, indents);
						break;

					case LuaOpcode.GETGLOBAL:
						//writer.WriteLine("{2}var{0} = _G[{1}]", i.A, GetConstant(i.Bx, function), indents);
						break;

					//case LuaOpcode.GETTABLE:
					//	writer.WriteLine("{3}var{0} = var{1}[{2}]", i.A, i.B, WriteIndex(i.C, function), indents);
					//	break;

					//case LuaOpcode.SETGLOBAL:
					//	writer.WriteLine("{2}_G[{0}] = var{1}", GetConstant(i.Bx, function), i.A, indents);
					//	break;

					//case LuaOpcode.SETUPVAL:
					//	writer.WriteLine("{2}upvalue[{0}] = var{1}", i.B, i.A, indents);
					//	break;

					//case LuaOpcode.SETTABLE:
					//	writer.WriteLine("{3}var{0}[{1}] = {2}", i.A, WriteIndex(i.B, function), WriteIndex(i.C, function), indents);
					//	break;

					//case LuaOpcode.NEWTABLE:
					//	writer.WriteLine("{1}var{0} = {{}}", i.A, indents);
					//	break;

					//case LuaOpcode.SELF:
					//	writer.WriteLine("{2}var{0} = var{1}", i.A + 1, i.B, indents);
					//	writer.WriteLine("{3}var{0} = var{1}[{2}]", i.A, i.B, WriteIndex(i.C, function), indents);
					//	break;

					//case LuaOpcode.ADD:
					//	writer.WriteLine("{3}var{0} = var{1} + var{2}", i.A, i.B, i.C, indents);
					//	break;

					//case LuaOpcode.SUB:
					//	writer.WriteLine("{3}var{0} = var{1} - var{2}", i.A, i.B, i.C, indents);
					//	break;

					//case LuaOpcode.MUL:
					//	writer.WriteLine("{3}var{0} = var{1} * var{2}", i.A, i.B, i.C, indents);
					//	break;

					//case LuaOpcode.DIV:
					//	writer.WriteLine("{3}var{0} = var{1} / var{2}", i.A, i.B, i.C, indents);
					//	break;

					//case LuaOpcode.MOD:
					//	writer.WriteLine("{3}var{0} = var{1} % var{2}", i.A, i.B, i.C, indents);
					//	break;

					//case LuaOpcode.POW:
					//	writer.WriteLine("{3}var{0} = var{1} ^ var{2}", i.A, i.B, i.C, indents);
					//	break;

					//case LuaOpcode.UNM:
					//	writer.WriteLine("{2}var{0} = -var{1}", i.A, i.B, indents);
					//	break;

					//case LuaOpcode.NOT:
					//	writer.WriteLine("{2}var{0} = not var{1}", i.A, i.B, indents);
					//	break;

					//case LuaOpcode.LEN:
					//	writer.WriteLine("{2}var{0} = #var{1}", i.A, i.B, indents);
					//	break;

					//case LuaOpcode.CONCAT:
					//	writer.Write("{1}var{0} = ", i.A, indents);

					//	for (int x = i.B; x < i.C; ++x)
					//		writer.Write("var{0} .. ", x);

					//	writer.WriteLine("var{0}", i.C);
					//	break;

					//case LuaOpcode.JMP:
					//	throw new NotImplementedException("Jmp");

					//case LuaOpcode.EQ:
					//	writer.WriteLine("{3}if ({0} == {1}) ~= {2} then", WriteIndex(i.B, function), WriteIndex(i.C, function), i.A, indents);
					//	break;

					//case LuaOpcode.LT:
					//	writer.WriteLine("{3}if ({0} < {1}) ~= {2} then", WriteIndex(i.B, function), WriteIndex(i.C, function), i.A, indents);
					//	break;

					//case LuaOpcode.LE:
					//	writer.WriteLine("{3}if ({0} <= {1}) ~= {2} then", WriteIndex(i.B, function), WriteIndex(i.C, function), i.A, indents);
					//	break;

					//case LuaOpcode.TEST:
					//	writer.WriteLine("{2}if not var{0} <=> {1} then", i.A, i.C, indents);
					//	break;

					case LuaOpcode.TESTSET:
						this.Result += $"{indents}if var{i.B} <=> {i.C} then\n";
						this.Result += $"{indents}\tvar{i.A} = var{i.B}\n";
						this.Result += $"end\n";
						//writer.WriteLine("{2}if var{0} <=> {1} then", i.B, i.C, indents);
						//writer.WriteLine("{2}\tvar{0} = var{1}", i.A, i.B, indents);
						//writer.WriteLine("end");
						break;

					case LuaOpcode.CALL:
						StringBuilder sb = new StringBuilder();

						if (i.C != 0)
						{
							sb.Append(indents);
							var indentLen = sb.Length;

							// return values
							for (int x = i.A; x < i.A + i.C - 2; ++x)
								sb.AppendFormat("var{0}, ", x);

							if (sb.Length - indentLen > 2)
							{
								sb.Remove(sb.Length - 2, 2);
								sb.Append(" = ");
							}
						}
						else
						{
							//throw new NotImplementedException("i.C == 0");
							this.Result += "i.C == 0\n";
						}

						// function
						sb.AppendFormat("var{0}(", i.A);

						if (i.B != 0)
						{
							var preArgsLen = sb.Length;

							// arguments
							for (int x = i.A; x < i.A + i.B - 1; ++x)
								sb.AppendFormat("var{0}, ", x + 1);

							if (sb.Length - preArgsLen > 2)
								sb.Remove(sb.Length - 2, 2);

							sb.Append(')');
						}
						else
						{
							//throw new NotImplementedException("i.B == 0");
							this.Result += "i.B == 0\n";
						}

						this.Result += sb.ToString() + "\n";
						break;

					case LuaOpcode.TAILCALL:
						this.Result += "TAILCALL\n"; // TODO: implement
						break;
					case LuaOpcode.RETURN:
						this.Result += "return\n";
						break;

					case LuaOpcode.FORLOOP:
						this.Result += "FORLOOP\n"; // TODO: implement
						break;
					case LuaOpcode.FORPREP:
						this.Result += "FORPREP\n"; // TODO: implement
						break;
					case LuaOpcode.TFORLOOP:
						this.Result += "TFORLOOP\n"; // TODO: implement
						break;
					case LuaOpcode.SETLIST:
						this.Result += "SETLIST\n"; // TODO: implement
						break;
					case LuaOpcode.CLOSE:
						this.Result += "CLOSE\n"; // TODO: implement
						break;
					case LuaOpcode.CLOSURE:
						this.Result += "CLOSURE\n"; // TODO: implement
						break;
					case LuaOpcode.VARARG:
						this.Result += "VARARG\n"; // TODO: implement
						break;
				}
			}
		}

		private string GetConstant(int idx, LuaFunction function)
		{
			return function.Constants[idx].ToString();
		}

		private int ToIndex(int value, out bool isConstant)
		{
			// this is the logic from lua's source code (lopcodes.h)
			if (isConstant = (value & 1 << 8) != 0)
				return value & ~(1 << 8);
			else
				return value;
		}

		private string WriteIndex(int value, LuaFunction function)
		{
			bool constant;
			int idx = ToIndex(value, out constant);

			if (constant)
				return function.Constants[idx].ToString();
			else
				return "var" + idx;
		}

	}
}
