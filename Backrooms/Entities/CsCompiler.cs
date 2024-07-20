using System.Linq;
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Generic;

namespace Backrooms;

public static class CsCompiler
{
    public static Assembly BuildAssembly(string[] sourceFiles, string assemblyName)
    {
        CompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        Compilation compilation = CSharpCompilation.Create(assemblyName)
            .WithOptions(options)
            .AddReferences(
                AppDomain.CurrentDomain.GetAssemblies()
                .Where(asm => !asm.IsDynamic)
                .Select(asm => MetadataReference.CreateFromFile(asm.Location)))
            .AddSyntaxTrees(sourceFiles.Select(f => CSharpSyntaxTree.ParseText(f)));

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);

        if(!emit.Success)
        {
            IEnumerable<Diagnostic> errors = emit.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error);

            if(errors.Count() is int errCount && errCount == 1)
                Out($"There has been an error when trying to compile dynamic assembly '{assemblyName}'");
            else
                Out($"There have been {errCount} errors when trying to compile dynamic assembly '{assemblyName}'");
              

            foreach(Diagnostic err in errors)
                Out($"{err.Id} ;; {err.GetMessage()}\n", ConsoleColor.Red);

            return null;
        }

        return Assembly.Load(stream.ToArray());
    }
}