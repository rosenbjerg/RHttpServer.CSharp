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
    class Builder
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
                                if (args.Length > 2)
                                {
                                    var oname = args[2];
                                    if (!oname.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) oname += ".exe";
                                    PrepareAndCompile(args[1], oname);
                                }
                                else
                                {
                                    PrepareAndCompile(args[1], "Server.exe");
                                }
                            }
                            else
                            {
                                Console.WriteLine("The directory containing source files could not be found ('"+ args[1]+"');");
                            }
                            break;
                        case "start":
                            if (File.Exists(args[1]) && args[1].EndsWith(".exe"))
                            {
                                StartServer(args[1]);
                            }
                            else
                            {
                                Console.WriteLine("The server executable file could not be found ('" + args[1] + "')");
                            }
                            break;
                        case "stop":
                            LoadRunningProcessList();
                            if (args[1].All(char.IsDigit))
                            {
                                var p = RunningProcesses.FirstOrDefault(pp => pp.Id == int.Parse(args[1]));
                                if (p != null)
                                {
                                    KillProcess(p);
                                    SaveRunningProcessList();
                                }
                            }
                            else if (File.Exists(args[1]))
                            {
                                var p =
                                    RunningProcesses.FirstOrDefault(
                                        pp => Path.GetFullPath(pp.Path) == Path.GetFullPath(args[1]));
                                if (p != null)
                                {
                                    KillProcess(p);
                                    SaveRunningProcessList();
                                }
                            }
                            else
                            {
                                Console.WriteLine(args[1] + " could not be found");
                            }
                            break;
                        default:
                            ShowHelp();
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
                else ShowHelp();
            }
	        else ShowHelp();
        }

        private static void ShowHelp()
        {
            Console.WriteLine("RHttpServerBuilder v. " + Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("\nSupported functionality:\n");
            Console.WriteLine("build SOURCEDIR\n" +
                              "Reads packages.config (if provided) in SOURCEDIR and downloads all missing dependencies." +
                              "Then compiles all .cs files in SOURCEDIR." +
                              "The resulting executable will be named Server.exe\n" +
                              "You can also place the required .dll libraries in a folder called ServerLibs in the working directory, " +
                              "if you do not want to use packages.config\n\n");

            Console.WriteLine("build SOURCEDIR OUTPUTNAME\n" +
                              "Same as above, but the resulting executable will be named based on OUTPUTNAME\n\n");

            Console.WriteLine("start SERVEREXEC\n" +
                              "Starts the server executable SERVEREXEC in a windowless process\n\n");

            Console.WriteLine("stop SERVEREXEC\n" +
                              "Stops the server process started from SERVEREXEC, if started using this tool\n\n");
            
            Console.WriteLine("stop SERVERPID\n" +
                              "Stops the server process started with the process id SERVERPID, if started using this tool\n\n");

            Console.WriteLine("stopall\n" +
                              "Stops all running windowless server processes started with this tool\n\n");

        }

        private static void UnpackBuilderDeps()
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            var assembly = Assembly.GetExecutingAssembly();
            if (!File.Exists(Path.Combine(path, "NuGet.Core.dll")))
            {
                using (Stream stream = assembly.GetManifestResourceStream("Builder.RHttpServer.BuilderDeps.NuGet.Core.dll"))
                {
                    FileStream fileStream = File.Create(Path.Combine(path, "NuGet.Core.dll"), (int)stream.Length);
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }
            if (!File.Exists(Path.Combine(path, "Microsoft.Web.XmlTransform.dll")))
            {
                using (Stream stream = assembly.GetManifestResourceStream("Builder.RHttpServer.BuilderDeps.Microsoft.Web.XmlTransform.dll"))
                {
                    FileStream fileStream = File.Create(Path.Combine(path, "Microsoft.Web.XmlTransform.dll"), (int)stream.Length);
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }
            if (!File.Exists(Path.Combine(path, "Server.config")))
            {
                using (Stream stream = assembly.GetManifestResourceStream("Builder.RHttpServer.BuilderDeps.Server.config"))
                {
                    FileStream fileStream = File.Create(Path.Combine(path, "Server.config"), (int)stream.Length);
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Close();
                    fileStream.Dispose();
                }
            }
        }

        private static void PrepareAndCompile(string inputDir, string outputname)
        {
            var topFolder = Environment.CurrentDirectory;
            var folder = inputDir.Replace(".", topFolder);
            var csFiles = Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories).ToArray();
            var packageConfig = Directory.EnumerateFiles(folder, "packages.config", SearchOption.AllDirectories).FirstOrDefault();

            var modulesFolder = Path.Combine(topFolder, "ServerLibs");
            Directory.CreateDirectory(modulesFolder);
            if (!string.IsNullOrWhiteSpace(packageConfig))
            {
                UnpackBuilderDeps();
                RestoreMissingNugets(packageConfig, modulesFolder);
            }
            else
            {
                Console.WriteLine("No packages.config file was found in {0}.\nUnable to download dependencies using nuget.", inputDir);
            }
            CreateSystemLibraries(modulesFolder);
            var dlls = Directory.EnumerateFiles(modulesFolder, "*.dll", SearchOption.AllDirectories).ToList();
            dlls = MakePathsRelativeTo(dlls, topFolder);
            csFiles = MakePathsRelativeTo(csFiles.ToList(), topFolder).ToArray();
            Compile(csFiles, outputname, dlls);
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

            var internalResources = assembly.GetManifestResourceNames().Where(t => t.Contains(".ServerDeps.")).ToArray();
            
            foreach (var res in internalResources)
            {
                var fpath = Path.Combine(modulesFolder, res.Replace("Builder.RHttpServer.ServerDeps.", ""));
                if (File.Exists(fpath)) continue;
                using (Stream stream = assembly.GetManifestResourceStream(res))
                {
                    FileStream fileStream = File.Create(fpath, (int) stream.Length);
                    byte[] bytesInStream = new byte[stream.Length];
                    stream.Read(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Write(bytesInStream, 0, bytesInStream.Length);
                    fileStream.Close();
                }
            }
        }

        private static void RestoreMissingNugets(string packagePath, string dllSavePath)
        {
            var repo = PackageRepositoryFactory.Default.CreateRepository("https://packages.nuget.org/api/v2");

            //Initialize the package manager
            string tempPath = Path.Combine(dllSavePath, "TEMP");
            Directory.CreateDirectory(tempPath);
            var files = ReadPackages(packagePath);
            var existing = Directory.EnumerateFiles(dllSavePath, "*.dll", SearchOption.AllDirectories);
            files = files.Where(f => !existing.Any(t => t.Contains(f.Name))).ToArray();
            PackageManager packageManager = new PackageManager(repo, tempPath);
            if (files.Any()) Console.WriteLine("Missing {0} packages", files.Length);
            for (int i = 0; i < files.Length; i++)
            {
                var package = files[i];
                Console.WriteLine("Downloading package {0}/{1}: {2} {3}", i + 1, files.Length, package.Name, package.Version);
                packageManager.InstallPackage(package.Name, SemanticVersion.Parse(package.Version));
            }
            var all = Directory.EnumerateFiles(tempPath, "*.dll", SearchOption.AllDirectories).Where(f => f.Contains("net45"));
            foreach (var f in all)
            {
                var filename = Path.Combine(dllSavePath, Path.GetFileName(f));
                if (!File.Exists(filename)) File.Move(f, filename);
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
            var parameters = new CompilerParameters
            {
                GenerateInMemory = false,
                TreatWarningsAsErrors = false,
                GenerateExecutable = true,
                CompilerOptions = $"/optimize",
                OutputAssembly = executableName
            };
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
                var conf = executableName + ".config";
                if (!File.Exists(conf))
                {
                    File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Server.config"), conf);
                    File.SetAttributes(conf, FileAttributes.Hidden | FileAttributes.ReadOnly);
                }
                
                Console.WriteLine("Build complete");
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
                Console.WriteLine("{0} started (pid {1})", proc.ProcessName, proc.Id);
            }
        }

        private static void KillProcess(ServerProcess sProc)
        {
            if (Process.GetProcesses().Any(p => p.Id == sProc.Id))
            {
                Process p = Process.GetProcessById(sProc.Id);
                p.Kill();
                Console.WriteLine("{0} (pid {1}) killed ", p.ProcessName, p.Id);
            }
            RunningProcesses.Remove(sProc);
        }

        private static void SaveRunningProcessList()
        {
            var hiddenFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".proclist.bin");
            FileStream stream = File.Create(hiddenFileName);
            var formatter = new BinaryFormatter();
            formatter.Serialize(stream, RunningProcesses);
            stream.Close();
            File.SetAttributes(hiddenFileName,
                FileAttributes.Archive /*|*/
                //FileAttributes.Hidden
            );
        }

        private static void LoadRunningProcessList()
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".proclist.bin");
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
