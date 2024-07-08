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
            .AddReferences(from asm in AppDomain.CurrentDomain.GetAssemblies()
                           where !asm.IsDynamic
                           select MetadataReference.CreateFromFile(asm.Location));

        compilation = compilation.AddSyntaxTrees(from f in sourceFiles
                                                 select CSharpSyntaxTree.ParseText(f));

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);

        if(!emit.Success)
        {
            IEnumerable<Diagnostic> errors = from d in emit.Diagnostics
                                             where d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error
                                             select d;

            if(errors.Count() == 1)
                Out($"There has been an error when trying to compile dynamic assembly '{assemblyName}'");
            else
                Out($"There have been {errors.Count()} errors when trying to compile dynamic assembly '{assemblyName}'");
              

            foreach(Diagnostic err in errors)
                Out($"{err.Id} ;; {err.GetMessage()}\n", ConsoleColor.Red);

            return null;
        }

        return Assembly.Load(stream.ToArray());
    }
}