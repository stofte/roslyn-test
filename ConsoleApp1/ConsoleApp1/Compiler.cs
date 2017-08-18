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
    using System.Text.RegularExpressions;

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
            var jsonFileName = $"{Program.ApplicationFolder}/dlls.json";
            var redistFileName = $"{Program.ApplicationFolder}/dlls-redist.json";
            if (File.Exists(redistFileName))
            {
                Console.WriteLine("Using dlls-redist files");
                jsonFileName = redistFileName;
            }
            else
            {
                Console.WriteLine("Using repository files");
            }
            var json = File.ReadAllText(jsonFileName);
            var data = JsonConvert.DeserializeObject<IEnumerable<string>>(json);
            var currentRuntime = Program.IsWindows ? "win" : "unix";
            var otherRuntime = Program.IsWindows ? "unix" : "win";
            foreach(var path in data)
            {
                string fullPath = path;
                if (!Path.IsPathRooted(path))
                {
                    fullPath = Path.Combine(Program.ApplicationFolder, path);
                }
                // check file presence, and if the path indicates platformness
                if (!File.Exists(fullPath) || !IsPlatformApplicable(fullPath, currentRuntime, otherRuntime)) continue;
                try {
                    var metaRef = MetadataReference.CreateFromFile(fullPath);
                    refs.Add(metaRef);
                } catch (Exception exn) {
                    Console.WriteLine("Failed reference: {0}", fullPath);
                    Console.WriteLine("Reason: {0}", exn);
                }
            }

            var consoleAppPath = Program.ApplicationImagePath;
            refs.Add(MetadataReference.CreateFromFile(consoleAppPath));

            return refs;
        }

        private static bool IsPlatformApplicable(string path, string currentRuntime, string otherRuntime)
        {
            var pathSep = Path.DirectorySeparatorChar;
            var sep = Path.DirectorySeparatorChar == '\\' ? $"\\{Path.DirectorySeparatorChar}" : $"{Path.DirectorySeparatorChar}";
            var rt = new Regex(string.Format("runtimes{0}{1}", sep, currentRuntime));
            var ort = new Regex(string.Format("runtimes{0}{1}", sep, otherRuntime));
            // either we find the current runtime in the path, and not the other,
            // or we find neither. throw if we find both. and dont link native dlls (sqlite, etc)
            var isApplicable = rt.IsMatch(path) && !ort.IsMatch(path) || (!rt.IsMatch(path) && !ort.IsMatch(path));
            var isNative = isApplicable && path.Contains($"{Path.DirectorySeparatorChar}native{Path.DirectorySeparatorChar}");
            var isConfused = rt.IsMatch(path) && ort.IsMatch(path);
            if (isConfused) throw new InvalidOperationException("confused about: " + path);
            return isApplicable && !isNative; // dont link native dlls
        }
    }
}
