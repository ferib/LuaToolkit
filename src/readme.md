# LuaToolkit

**LuaToolkit** is a C# library that is used for Lua 5.1.

*TODO: insert kewl logo*

## Features
- **Decoder/Encoder**: Encodes/Decodes Lua 5.1 Compiler Binarys.
- **Decompiler**: Decompiles lua bytecode to lua script code.
- ~~**Emulator**:~~ Abandoned, no intentions *(yet*) to finish.
- **Obfuscator:** Obfuscator that uses Bytelevel, Blocklevel and Scriptlevel.
- **Beautifier**: Simple code beautifie/highlighting.

### Decoder/Encoder
This Decoder/Encoder is used to turn Lua 5.1 Bytecode into C# classes or vice verse.
Very usefull to read/write compiled Lua binary's.

### Decompiler
The Decompiler is pretty basic and has no optimization or anything.
But I am gonna call this a feature instead of a bug, because our Obfuscator will benefit from the unoptimized code it outputs.

## Emulator
Nope, not today...

### Obfuscator
The obfuscator is designed to integrate all of the above features.

For example, our ``LOFlow`` plugin uses ``Decompiler.LuaScriptblock`` to tamper with the controlflow on 'Blocklevel' while it adds additional ``Core.LuaInstructions`` to generate new IF statements on bytecode level.
The ``LOPacker`` plugin on the other hand uses ``Decompiler.LuaScriptFunction``'s end result to convert the decompiled Lua script code and then packs it.

### Beautifier
This only exists to show pretty results in the console so we humans can better see whats going on.