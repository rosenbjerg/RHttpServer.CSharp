using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;
using Microsoft.CSharp;
using NuGet;

namespace Builder.RHttpServer
{
    class Program
    {
        private static readonly List<ServerProcess> RunningProcesses = new List<ServerProcess>();

        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                if (args.Length > 1)
                {
                    switch (args[0])
                    {
                        case "build":
                            if (Directory.Exists(args[1]))
                            {
                                PrepareAndCompile(args[1]);
                            }
                            break;
                        case "start":
                            if (File.Exists(args[1]) && args[1].EndsWith(".exe"))
                            {
                                StartServer(args[1]);
                            }
                            break;
                    }
                }
                else if (args[0] == "stopall")
                {
                    LoadRunningProcessList();
                    var procs = new List<ServerProcess>(RunningProcesses);
                    foreach (var process in procs)
                    {
                        KillProcess(process);
                    }
                    SaveRunningProcessList();
                }
            }
        }

        private static void PrepareAndCompile(string inputDir)
        {
            var folder = inputDir;
            var topFolder = Path.GetDirectoryName(Path.GetFullPath(folder));
            var csFiles = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories).ToArray();
            var packageConfig = Directory.EnumerateFiles(folder, "packages.config", SearchOption.AllDirectories).FirstOrDefault();

            var modulesFolder = Path.Combine(topFolder, "ServerPackages");
            Directory.CreateDirectory(modulesFolder);
            if (!string.IsNullOrWhiteSpace(packageConfig))
            {
                RestoreMissingNugets(packageConfig, modulesFolder);
            }
            CreateSystemLibraries(modulesFolder);
            var dlls = Directory.EnumerateFiles(modulesFolder, "*.dll", SearchOption.AllDirectories).ToList();
            dlls = MakePathsRelativeTo(dlls, topFolder);
            csFiles = MakePathsRelativeTo(csFiles.ToList(), topFolder).ToArray();
            Compile(csFiles, "Server.exe", dlls);
        }

        private static List<string> MakePathsRelativeTo(List<string> dlls, string topFolder)
        {
            for (int index = 0; index < dlls.Count; index++)
            {
                var dll = dlls[index].Replace(topFolder + "\\", "./").Replace("\\", "/");
                dlls[index] = dll;
            }
            return dlls;
        }

        private static void CreateSystemLibraries(string modulesFolder)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var internalResources = assembly.GetManifestResourceNames();
            
            var dir = Path.Combine(modulesFolder, "SystemLibs");
            Directory.CreateDirectory(dir);

            for (int index = 0; index < internalResources.Length; index++)
            {
                var res = internalResources[index];
                using (Stream stream = assembly.GetManifestResourceStream(res))
                {
                    FileStream fileStream = File.Create(Path.Combine(dir, res.Replace("Builder.RHttpServer.", "")), (int) stream.Length);
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Close();
                }
            }
        }

        private static void RestoreMissingNugets(string packagePath, string dllSavePath)
        {
            IPackageRepository repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

            //Initialize the package manager
            string tempPath = Path.Combine(dllSavePath, "TEMP");
            Directory.CreateDirectory(tempPath);
            var files = ReadPackages(packagePath);
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
                var inside = Directory.EnumerateFiles(dir, "*.dll", SearchOption.AllDirectories);
                var folder = Path.Combine(dllSavePath, dirName);
                Directory.CreateDirectory(folder);
                foreach (var i in inside)
                {
                    var filename = Path.Combine(folder, Path.GetFileName(i));
                    File.Move(i, filename);
                }
            }
            Directory.Delete(tempPath, true);
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

        private static void Compile(string[] csFiles, string executableName, IEnumerable<string> dlls)
        {
            var csc = new CSharpCodeProvider();

            //string[] references =
            //{
            //    "System.dll", "System.Linq.dll", "System.Core.dll", "mscorlib.dll"
            //};
            var parameters = new CompilerParameters
            {
                GenerateInMemory = false,
                TreatWarningsAsErrors = false,
                GenerateExecutable = true,
                CompilerOptions = "/optimize",
                OutputAssembly = executableName

            };

            //parameters.ReferencedAssemblies.AddRange(references);
            parameters.ReferencedAssemblies.AddRange(dlls.ToArray());
            CompilerResults compile = csc.CompileAssemblyFromSource(parameters, csFiles.Select(File.ReadAllText).ToArray());

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

        private static void StartServer(string serverExecutablePath)
        {
            if (File.Exists(serverExecutablePath))
            {

                Process proc = new Process
                {
                    StartInfo =
                    {
                        FileName = serverExecutablePath,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden
                    }
                };

                proc.Start();
                var sProc = new ServerProcess(proc.Id, serverExecutablePath, proc.ProcessName);
                RunningProcesses.Add(sProc);
                SaveRunningProcessList();
            }
        }

        private static void KillProcess(ServerProcess sProc)
        {
            if (Process.GetProcesses().Any(p => p.Id == sProc.Id))
            {
                Process p = Process.GetProcessById(sProc.Id);
                p.Kill();
            }
            RunningProcesses.Remove(sProc);
        }

        private static void SaveRunningProcessList()
        {
            var hiddenFileName = "./.sproclist.bin";
            FileStream stream = File.Create(hiddenFileName);
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, RunningProcesses);
            stream.Close();
            File.SetAttributes(hiddenFileName,
                FileAttributes.Archive /*|*/
                //FileAttributes.Hidden
            );
        }

        private static void LoadRunningProcessList(string path = "./.sproclist.bin")
        {
            if (!File.Exists(path)) return;
            using (var stream = File.OpenRead(path))
            {
                var formatter = new BinaryFormatter();
                var v = (List<ServerProcess>)formatter.Deserialize(stream);
                RunningProcesses.AddRange(v);
            }
        }

    }

    [Serializable]
    public class ServerProcess
    {
        public ServerProcess(int id, string path, string pname)
        {
            Id = id;
            Path = System.IO.Path.GetFullPath(path);
            Name = pname;
        }

        public string Name { get; set; }
        public int Id { get; set; }
        public string Path { get; set; }

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
