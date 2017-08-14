namespace ConsoleApp1
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;

    public interface IGenerated
    {
        int DoIt();
    }

    public class Foo
    {
        public static void Bar()
        {
            Console.WriteLine("hello from main");
        }
    }

    class Program
    {
        public static string BaseDirectory;
        public static string ApplicationImagePath;

        static void Main(string[] args)
        {
            ApplicationImagePath = Assembly.GetEntryAssembly().Location;
            BaseDirectory = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(ApplicationImagePath), "..", "..", "..", "..", ".."));
            Console.WriteLine("ImagePath: {0}", ApplicationImagePath);
            var sqlitePath = Path.Combine(BaseDirectory, "world.sqlite");
            var connStr = string.Format("Data Source = {0}", sqlitePath);
            var schemaSrc = SchemaSource.Get(connStr, "SqliteWorld");
            var schema = Compiler.Build("schema", schemaSrc);
            var query = Compiler.Build("query", _source, schema.Item2);
            var programType = query.Item1.GetTypes().Single(t => t.Name == "Generated");
            var programInstance = (IGenerated)Activator.CreateInstance(programType);
            Console.WriteLine("Instance returned {0}", programInstance.DoIt());
            Console.ReadKey();
        }

        static string _source = @"
namespace SomeNs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.ComponentModel.DataAnnotations.Schema;
    using ConsoleApp1;

    public class Generated : IGenerated
    {
        public int DoIt()
        {
            Foo.Bar();
            using (var context = new SqliteWorld.Ctx())
            {
                var count = context.city.Where(x => x.Name.StartsWith(""Ca"")).Count();
                Console.WriteLine(""Cities starting with 'Ca': {0}"", count);
                return count;
            }
        }
    }
}";
    }
}