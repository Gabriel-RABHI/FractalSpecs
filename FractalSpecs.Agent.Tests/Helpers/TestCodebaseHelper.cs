using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FractalSpecs.Agent.Tests.Helpers
{
    public class TestCodebaseHelper : IDisposable
    {
        public string RootPath { get; }
        public List<string> ReadPaths { get; }

        public TestCodebaseHelper()
        {
            RootPath = Path.Combine(Path.GetTempPath(), "FractalSpecs_Tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(RootPath);
            ReadPaths = new List<string> { RootPath };
        }

        public void CreateComplexCodebase()
        {
            // 1. Create source files
            var srcPath = Path.Combine(RootPath, "src");
            Directory.CreateDirectory(srcPath);

            var programCs = @"
using System;
namespace MyApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(""Hello World!"");
            var user = new Models.User();
            user.Name = ""GrepTest"";
        }
    }
}";
            File.WriteAllText(Path.Combine(srcPath, "Program.cs"), programCs.Trim());

            var modelsPath = Path.Combine(srcPath, "Models");
            Directory.CreateDirectory(modelsPath);

            var userCs = @"
namespace MyApp.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}";
            File.WriteAllText(Path.Combine(modelsPath, "User.cs"), userCs.Trim());

            var otherCs = @"
namespace MyApp.Models
{
    // GrepTest regex will target this
    public class Order
    {
        public int OrderId { get; set; }
    }
}";
            File.WriteAllText(Path.Combine(modelsPath, "Order.cs"), otherCs.Trim());

            var configJson = @"{
    ""AppSettings"": {
        ""TestKey"": ""GrepTestValue"",
        ""Enabled"": true
    }
}";
            File.WriteAllText(Path.Combine(srcPath, "config.json"), configJson);

            // 2. Create ignored bin/obj directories
            var binPath = Path.Combine(srcPath, "bin", "Debug", "net10.0");
            Directory.CreateDirectory(binPath);
            File.WriteAllText(Path.Combine(binPath, "app.dll"), "MZ... Dummy DLL Content with GrepTest inside so we can check if it gets ignored.");
            File.WriteAllText(Path.Combine(binPath, "app.pdb"), "Dummy PDB Content");

            // 3. Create .gitignore
            var gitignore = @"
bin/
obj/
*.log
";
            File.WriteAllText(Path.Combine(RootPath, ".gitignore"), gitignore.Trim());

            // 4. Create ignored log file
            File.WriteAllText(Path.Combine(RootPath, "app.log"), "2026-05-09 INFO: System started with GrepTest parameter.");

            // 5. Create .git directory
            var gitPath = Path.Combine(RootPath, ".git");
            Directory.CreateDirectory(gitPath);
            File.WriteAllText(Path.Combine(gitPath, "config"), "[core]\n\trepositoryformatversion = 0\n\tbare = false\nDummy GrepTest");
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(RootPath))
                {
                    Directory.Delete(RootPath, true);
                }
            }
            catch
            {
                // Ignore cleanup errors during tests
            }
        }
    }
}
