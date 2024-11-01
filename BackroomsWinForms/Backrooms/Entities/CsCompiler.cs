using System.Linq;
using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using System.IO;
using Microsoft.CodeAnalysis.Emit;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace Backrooms.Entities;

public static class CsCompiler
{
#if PUBLISH_SINGLE_FILE
    public static readonly IEnumerable<MetadataReference> referenceAssemblies = fuck off; // TODO
#else
    public static readonly IEnumerable<MetadataReference> referenceAssemblies = AppDomain.CurrentDomain.GetAssemblies()
        .Select(static asm => MetadataReference.CreateFromFile(asm.Location));
#endif


    public static string CalculateChecksumFile(string[] strings)
    {
        StringBuilder sb = new();

        foreach(string str in strings)
            str
            .Pipe(Encoding.Default.GetBytes)
            .Pipe(SHA256.HashData)
            .Pipe(BitConverter.ToString)
            .Replace("-", "")
            .ToLowerInvariant()
            .Pipe(sb.AppendLine);

        return sb.ToString();
    }

    public static Assembly BuildAssembly(string[] sourceFiles, string assemblyName, string outputDir, out bool usedCachedDll)
    {
        outputDir = Path.TrimEndingDirectorySeparator(outputDir);

        string
            checksum = null,
            dllPath = $"{outputDir}/{assemblyName}.dll",
            checksumPath = $"{outputDir}/{assemblyName}.sha256";


        if(File.Exists(dllPath) && (checksum = CalculateChecksumFile(sourceFiles)) == File.ReadAllText(checksumPath))
        {
            usedCachedDll = true;
            return Assembly.LoadFrom(dllPath);
        }

        CompilationOptions options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

        Compilation compilation = CSharpCompilation.Create(assemblyName)
            .WithOptions(options)
            .AddReferences(referenceAssemblies)
            .AddSyntaxTrees(sourceFiles.Select(f => CSharpSyntaxTree.ParseText(f)));

        EmitResult emit = compilation.Emit($"{outputDir}/{assemblyName}.dll");
        File.WriteAllText($"{outputDir}/{assemblyName}.sha256", checksum ?? CalculateChecksumFile(sourceFiles));

        if(!emit.Success)
        {
            IEnumerable<Diagnostic> errors = emit.Diagnostics.Where(d => d.IsWarningAsError || d.Severity == DiagnosticSeverity.Error);

            if(errors.Count() is int errCount && errCount == 1)
                Out(Log.Entity, $"There has been an error when trying to compile dynamic assembly '{assemblyName}'");
            else
                Out(Log.Entity, $"There have been {errCount} errors when trying to compile dynamic assembly '{assemblyName}'");

            foreach(Diagnostic err in errors)
                Out(Log.Entity, $"{err.Id} ;; {err.GetMessage()}\n");

            usedCachedDll = false;
            return null;
        }

        using MemoryStream memStream = new();
        compilation.Emit(memStream);

        usedCachedDll = false;
        return Assembly.Load(memStream.ToArray());
    }
}