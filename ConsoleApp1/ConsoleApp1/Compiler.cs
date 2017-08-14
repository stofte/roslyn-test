namespace ConsoleApp1
{
    using System;
    using System.Reflection;
    using System.Linq;
    using System.IO;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Emit;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    class Compiler
    {
        public static Tuple<Assembly, MetadataReference> Build(string assmName, string source, MetadataReference schema = null)
        {
            var references = GetReferences();
            var currentAssembly = typeof(Program).GetTypeInfo().Assembly;
            var fileUri = "file:///";
            // pretty dumb test for windows platform
            if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEMP")))
            {
                fileUri = "file://";
            }
            var asmPath = Path.GetFullPath(currentAssembly.CodeBase.Substring(fileUri.Length));

            var compilerOptions = new CSharpCompilationOptions(outputKind: OutputKind.DynamicallyLinkedLibrary);
            var trees = new SyntaxTree[] {
                CSharpSyntaxTree.ParseText(source),
            };

            var compilation = CSharpCompilation.Create(assmName)
                .WithOptions(compilerOptions)
                .WithReferences(references.Concat(new[] {
                    MetadataReference.CreateFromFile(asmPath)
                }.Concat(schema != null ?
                    new[] { schema } : new MetadataReference[] { }
                )))
                .AddSyntaxTrees(trees);

            var stream = new MemoryStream();
            var compilationResult = compilation.Emit(stream, options: new EmitOptions());
            foreach (var diag in compilationResult.Diagnostics)
            {
                if (diag.Severity == DiagnosticSeverity.Error)
                {
                    Console.WriteLine("Error: {0}", diag.GetMessage());
                }
            }
            stream.Position = 0;
            var asm = LibraryLoader.LoadFromStream(stream);
            stream.Position = 0;
            var metaRef = MetadataReference.CreateFromStream(stream);
            return Tuple.Create(asm, metaRef as MetadataReference);
        }

        private static IEnumerable<MetadataReference> GetReferences()
        {
            var refs = new List<MetadataReference>();
            var json = File.ReadAllText(@"..\..\dlls.json");
            var data = JsonConvert.DeserializeObject<IEnumerable<string>>(json);

            var filter = new string[] { };
            foreach(var path in data)
            {
                if (filter.Any(f => path.Contains(f))) continue;
                refs.Add(MetadataReference.CreateFromFile(path));
            }

            var consoleAppPath = @"C:\src\roslyn-test\ConsoleApp1\ConsoleApp1\bin\Debug\netcoreapp1.1\ConsoleApp1.dll";
            refs.Add(MetadataReference.CreateFromFile(consoleAppPath));

            return refs;
        }
    }
}
