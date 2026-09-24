// Linux stand-in for MrPurple6411's AssemblyPublicizer.exe, which V+ expects you to run on
// Windows. Makes every type, field and method public and writes <name>_publicized.dll, so the
// project's HintPaths resolve. Reference-only output; it is never shipped or loaded by the game.
using System;
using System.IO;
using Mono.Cecil;

class Program {
  static void Main(string[] args) {
    string outDir = args[0];
    Directory.CreateDirectory(outDir);
    var resolver = new DefaultAssemblyResolver();
    resolver.AddSearchDirectory(Path.GetDirectoryName(Path.GetFullPath(args[1])));

    for (int i = 1; i < args.Length; i++) {
      string src = args[i];
      var asm = AssemblyDefinition.ReadAssembly(src,
          new ReaderParameters { AssemblyResolver = resolver, ReadWrite = false });
      int types = 0, fields = 0, methods = 0;

      foreach (var type in AllTypes(asm.MainModule)) {
        if (!type.IsPublic && !type.IsNestedPublic) {
          if (type.IsNested) type.IsNestedPublic = true; else type.IsPublic = true;
          types++;
        }
        foreach (var f in type.Fields)
          // Leave the compiler-generated backing fields of events alone: making them public
          // changes the surface the C# compiler sees for event access.
          if (!f.IsPublic && !f.IsSpecialName) { f.IsPublic = true; fields++; }
        foreach (var m in type.Methods)
          if (!m.IsPublic) { m.IsPublic = true; methods++; }
      }

      // Only the FILE name changes; the assembly identity stays as-is. These assemblies
      // reference each other by identity (assembly_valheim uses Vector2i from assembly_utils),
      // so renaming the identity leaves those references pointing at assemblies that are not
      // in the reference set and every use of a shared type fails to compile.
      string name = Path.GetFileNameWithoutExtension(src) + "_publicized.dll";
      string dst = Path.Combine(outDir, name);
      asm.Write(dst);
      Console.WriteLine($"{Path.GetFileName(src)} -> {name}  (+{types} types, +{fields} fields, +{methods} methods)");
    }
  }

  static System.Collections.Generic.IEnumerable<TypeDefinition> AllTypes(ModuleDefinition m) {
    foreach (var t in m.Types) foreach (var x in Walk(t)) yield return x;
  }
  static System.Collections.Generic.IEnumerable<TypeDefinition> Walk(TypeDefinition t) {
    yield return t;
    foreach (var n in t.NestedTypes) foreach (var x in Walk(n)) yield return x;
  }
}
