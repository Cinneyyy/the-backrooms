using System.Linq;
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.IO;
using Microsoft.CodeAnalysis.Emit;

namespace Backrooms;

public static class CsCompiler
{
    public static Assembly BuildAssembly(string[] sourceFiles, string assemblyName)
    {
        CompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        Compilation compilation = CSharpCompilation.Create(assemblyName)
            .WithOptions(options)
            .AddReferences(from ass in AppDomain.CurrentDomain.GetAssemblies()
                           where !ass.IsDynamic
                           select MetadataReference.CreateFromFile(ass.Location));

        foreach(string codeFile in sourceFiles)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(codeFile);
            compilation = compilation.AddSyntaxTrees(syntaxTree);
        }

        using MemoryStream stream = new();
        EmitResult emit = compilation.Emit(stream);

        if(!emit.Success)
        {
            foreach(Diagnostic err in from d in emit.Diagnostics
                                      where d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error
                                      select d)
                Out($"{err.Id} ;; {err.GetMessage()}", ConsoleColor.Red);

            return null;
        }

        stream.Seek(0, SeekOrigin.Begin);
        return Assembly.Load(stream.ToArray());
    }
}