using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.CSharp;
using NuGet;

namespace Builder.RHttpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var file = "./Server.cs";/*args[0];*/
            var dir = Path.GetDirectoryName(file);
            var modulesDir = Path.Combine(dir, "ServerLibs");
            Directory.CreateDirectory(modulesDir);
            var dlls = RestoreMissingNugets(modulesDir);
            CompileAndStart(file, file.Replace(".cs", ".exe"), dlls);
            Console.ReadKey();
        }

        private static IEnumerable<string> RestoreMissingNugets(string dllSavePath)
        {
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

            //Initialize the package manager
            string tempPath = Path.Combine(dllSavePath, "TEMP");
            Directory.CreateDirectory(tempPath);
            var files = ReadPackages("./packages.config");
            var existing = Directory.EnumerateDirectories(dllSavePath);
            files = files.Where(f => existing.All(d => new DirectoryInfo(d).Name != f.Name + "." + f.Version)).ToArray();
            PackageManager packageManager = new PackageManager(repo, tempPath);
            foreach (var package in files)
            {
                packageManager.InstallPackage(package.Name, SemanticVersion.Parse(package.Version));
            }
            var dirs = Directory.EnumerateDirectories(tempPath);
            foreach (var dir in dirs)
            {
                var dirName = new DirectoryInfo(dir).Name;
                var inside = GetLibDlls(dir);
                var folder = Path.Combine(dllSavePath, dirName);
                Directory.CreateDirectory(folder);
                foreach (var i in inside)
                {
                    var filename = Path.Combine(folder, Path.GetFileName(i));
                    File.Move(i, filename);
                }
            }
            Directory.Delete(tempPath, true);
            GetLibDlls(dllSavePath).Select(t => t.L);
            

            //Download and unzip the package
            //packageManager.
        }

        private static List<string> GetLibDlls(string path)
        {
            List<string> ret = new List<string>();
            var dirs = Directory.EnumerateDirectories(path);
            foreach (var dir in dirs)
            {
                ret.AddRange(GetLibDlls(dir));
            }

            var files = Directory.EnumerateFiles(path).Where(f => f.EndsWith(".dll", StringComparison.OrdinalIgnoreCase));
            foreach (var file in files)
            {
                ret.Add(file);
            }
            return ret;
        }

        private static Package[] ReadPackages(string packagesDotConfig)
        {
            var list = new List<Package>();
            if (File.Exists(packagesDotConfig))
            {
                XmlTextReader reader = new XmlTextReader(packagesDotConfig);
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element && reader.Name == "package")
                    {
                        var bla = new List<string>();
                        while (reader.MoveToNextAttribute()) // Read the attributes.
                        {
                            bla.Add(reader.Value);
                        }
                        list.Add(new Package(bla[0], bla[1], bla[2]));
                    }
                }
            }
            return list.ToArray();
        }

        private static void CompileAndStart(string csFile, string executableName, IEnumerable<string> dlls)
        {

            var csc = new CSharpCodeProvider();

            string[] references = { "System.dll", "System.Linq.dll", "System.Core.dll", "mscorlib.dll" };
            var parameters = new CompilerParameters
            {
                GenerateInMemory = false,
                TreatWarningsAsErrors = false,
                GenerateExecutable = true,
                CompilerOptions = "/optimize",
                OutputAssembly = executableName,
            };
            
            parameters.ReferencedAssemblies.AddRange(references);
            parameters.ReferencedAssemblies.AddRange(dlls.ToArray());
            CompilerResults compile = csc.CompileAssemblyFromSource(parameters, File.ReadAllText(csFile));

            if (compile.Errors.HasErrors)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Compile error:");
                foreach (CompilerError ce in compile.Errors)
                {
                    sb.AppendLine(ce.ToString());
                }
                Console.WriteLine(sb.ToString());
            }
            else
            {
                Console.WriteLine("Compiled and saved");
            }
        }
    }

    public class Package
    {
        public Package(string name, string version, string framework)
        {
            Name = name;
            Version = version;
            Framework = framework;
        }

        public string Name { get; }
        public string Version { get; }
        public string Framework { get; }
    }
}
