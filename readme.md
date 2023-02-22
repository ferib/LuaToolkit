# LuaToolkit

**[LuaToolkit](https://github.com/ferib/LuaToolkit/tree/master/src/)** is a library for C# that aims for Obfuscation.

The toolset not only comes with a customizable Obfuscator that supports a plugin framework. It also has features such as Decompiling and Disassembling of compiled Lua binary's.

For now, the LuaToolkit is **only** supporting **Lua version 5.1**.

## Research
This project is part of my series of articles listed below:
- [Lua Devirtualization Part 1: Introduction](https://ferib.dev/blog.php?l=post/Lua_Devirtualization_Part_1_Introduction)
- [Lua Devirtualization Part 2: Decompiling Lua](https://ferib.dev/blog.php?l=post/Lua_Devirtualization_Part_2_Decompiling_Lua)

## Examples
You can find a few examples in the ``demo/`` directory, one of the most visually interesting ones is the [graph](https://github.com/ferib/LuaToolkit/demo/graph) example that renders an interactive visual representation of the Lua function it Decompiled.
![grmGraph](https://github.com/ferib/LuaToolkit/blob/master/img/graph_frmGraph.png)

## Source
The source for the core library can be found in the ``src/`` directory.
