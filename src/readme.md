# LuaToolkit

**LuaToolkit** is a C# library that is used for Lua 5.1.

*TODO: insert kewl logo*

## Features
- **[Decoder/Encoder](https://github.com/ferib/LuaToolkit/tree/master/src/Disassembler)**: Encodes/Decodes Lua 5.1 Compiler Binarys.
- **[Decompiler](https://github.com/ferib/LuaToolkit/tree/master/src/Decompiler)**: Decompiles lua bytecode to lua script code.
- ~~**[Emulator](https://github.com/ferib/LuaToolkit/tree/master/src/Emulator)**:~~ Abandoned, no intentions *(yet*) to finish.
- **[Obfuscator](https://github.com/ferib/LuaToolkit/tree/master/src/Obfuscator)**: Obfuscator that uses Bytelevel, Blocklevel and Scriptlevel.
- **[Beautifier](https://github.com/ferib/LuaToolkit/tree/master/src/Beautifier)**: Simple code beautifie/highlighting.

### Decoder/Encoder
This Decoder/Encoder is used to turn Lua 5.1 Bytecode into C# classes or vice versa.
Very useful to read/write compiled Lua binary's.

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
        LuaCFile = luac new LuaCFile(File.ReadAllBytes(@"C:\test.luac");
	LuaDecoder decoder = new LuaDecoder(luac);

        Console.WriteLine("the compiled Lua file contains {deocder.File.Function.Functions.Count} functions.");
    }
}
```

### Decompiler
The Decompiler is pretty basic and has no optimization or anything.
But I am gonna call this a feature instead of a bug, because our Obfuscator will benefit from the unoptimized code it outputs.

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
        LuaCFile = luac new LuaCFile(File.ReadAllBytes(@"C:\test.luac");
	LuaDecoder decoder = new LuaDecoder(luac);
        LuaDecompiler decompiler = new LuaDecompiler(decoder);

        Console.WriteLine("Lua Decompiler output: \n{decompiler.LuaScript}");
    }
}
```


### Emulator
Nope, not today...

### Obfuscator
The obfuscator is designed to integrate all of the above features.

For example, our ``LOFlow`` plugin uses ``Decompiler.LuaScriptblock`` to tamper with the control-flow on 'Blocklevel' while it adds additional ``Core.LuaInstructions`` to generate new IF statements on bytecode level.
The ``LOPacker`` plugin on the other hand uses ``Decompiler.LuaScriptFunction``'s end result to convert the decompiled Lua script code and then packs it.

NOTE: Plugin system not yet finished.

### Beautifier
This only exists to show pretty results in the console so we humans can better see what's going on.

NOTE: not yet finished.
