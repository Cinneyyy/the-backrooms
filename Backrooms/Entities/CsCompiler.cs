using System.Linq;
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Generic;

namespace Backrooms.Entities;

public static class CsCompiler
{
    public static readonly IEnumerable<MetadataReference> referenceAssemblies = AppDomain.CurrentDomain.GetAssemblies().Select(asm => MetadataReference.CreateFromFile(asm.Location));


    public static Assembly BuildAssembly(string[] sourceFiles, string assemblyName)
    {
        CompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        Compilation compilation = CSharpCompilation.Create(assemblyName)
            .WithOptions(options)
            .AddReferences(referenceAssemblies)
            .AddSyntaxTrees(sourceFiles.Select(f => CSharpSyntaxTree.ParseText(f)));

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);

        if(!emit.Success)
        {
            IEnumerable<Diagnostic> errors = emit.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error);

            if(errors.Count() is int errCount && errCount == 1)
                Out(Log.Entity, $"There has been an error when trying to compile dynamic assembly '{assemblyName}'");
            else
                Out(Log.Entity, $"There have been {errCount} errors when trying to compile dynamic assembly '{assemblyName}'");

            foreach(Diagnostic err in errors)
                Out(Log.Entity, $"{err.Id} ;; {err.GetMessage()}\n");

            return null;
        }

        return Assembly.Load(stream.ToArray());
    }
}