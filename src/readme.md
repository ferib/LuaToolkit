# LuaToolkit

**LuaToolkit** is a C# library for Encoding, Decoding, Disassembling and Decompiling Lua Bytecode.

Currently, the only target is Lua 5.1

*TODO: insert kewl logo*


# ⚠️ Deprecated

The current project was written for the sake of educational purposes only.
This project was originally written for my [Lua Devirtualization Part 2: Decompiling Lua](https://ferib.dev/blog.php?l=post/Lua_Devirtualization_Part_2_Decompiling_Lua) article as I felt a sudden need to re-invent the wheel as I researched this topic.

However, I noticed there is still some interest in the project _(in fact, this is my current highest upvoted repo)_, therefore I might revisit this topic and re-write some things _(LuaToolkit V2 soon™️)_.


## Features
- **[Decoder/Encoder](./Disassembler)**: Encodes/Decodes Lua 5.1 Compiler Binarys.
- **[Decompiler](./Decompiler)**: Decompiles lua bytecode to lua script code.
- (⚠️**Deprecated**) ~~**[Emulator](./Emulator)**: Abandoned, no intentions to finish.~~
- (⚠️**Deprecated**) ~~**[Obfuscator](./Obfuscator)**: Obfuscator that uses Bytelevel, Blocklevel and Scriptlevel.~~
- (⚠️**Deprecated**) ~~**[Beautifier](./Beautifier)**: Simple code beautifie/highlighting.~~


## Decoder/Encoder
The Decoder and/or Encoder are used to turn Lua 5.1 Bytecode into C# classes, or vice versa.

Example:
```cs
using System;
using System.IO;
using LuaToolkit.Core;
using LuaToolkit.Disassembler;

namespace Example 
{
    class Program
    {
        static void Main(string[] args)
	    {
            LuaCFile luac = new LuaCFile(File.ReadAllBytes(@"C:\test.luac"));
            LuaDecoder decoder = new LuaDecoder(luac);
            Console.WriteLine($"the compiled Lua file contains {decoder.File.Function.Functions.Count} functions.");
	    }
    }
}
```

## Decompiler
The Decompiler is unfinished, buggy, and not well-tested. The current output is closer to disassembly then it is to meaningful Lua pseudo code.

Example:
```cs
using System;
using System.IO;
using LuaToolkit.Core;
using LuaToolkit.Disassembler;
using LuaToolkit.Decompiler;

namespace Example 
{
    class Program 
    {
        static void Main(string[] args)
        {
            LuaCFile luac = new LuaCFile(File.ReadAllBytes(@"C:\test.luac"));
            LuaDecoder decoder = new LuaDecoder(luac);
            LuaDecompiler decompiler = new LuaDecompiler(decoder);
            Console.WriteLine($"Lua Decompiler output: \n{decompiler.LuaScript}");
        }
    }
}
```