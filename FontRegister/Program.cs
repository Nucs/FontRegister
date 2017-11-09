using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace FontRegAuto {
    internal class Program {
        private static readonly bool is64BitProcess = IntPtr.Size == 8;
        private static readonly bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();
        public static DirectoryInfo ExecutingDir => new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "")) ?? "/");

        public static int Main(string[] args) {
            Console.Title = "FontRegister By Eli Belash / Nucs 2017";
            bool haltonend = false;
            List<string> fonts = new List<string>();
            void ProcessPath(string path) {
                try {
                    Console.WriteLine($"Processing \"{path}\"");
                    FileAttributes attr = File.GetAttributes(path);
                    if (attr.HasFlag(FileAttributes.Directory)) {
                        fonts.AddRange(GetFiles(new DirectoryInfo(path), "*.fon", "*.ttf", "*.ttc", "*.otf").Select(f => f.FullName));
                    } else {
                        var ex = Path.GetExtension(path);
                        if (!ex.EndsWith(".fon", true, CultureInfo.InvariantCulture) && !ex.EndsWith(".ttc", true, CultureInfo.InvariantCulture) && !ex.EndsWith(".ttf", true, CultureInfo.InvariantCulture) && !ex.EndsWith(".otf", true, CultureInfo.InvariantCulture))
                            return;
                        fonts.Add(path);
                    }
                } catch (IOException e) {
                    Console.WriteLine($"Failed on \"{path}\" because \"{e.Message}\"");
                }
            }

            if (args != null && args.Length == 1 && (args[0].StartsWith("/h") || args[0].Contains("-h"))) {
                PrintInfo();
                Exit(false);
            } else if (args != null && args.Length == 1 && args[0].StartsWith("--")) {
                if (args[0] == "--clear" || args[0] == "--cleanup") {
                    RunCleanup();
                     return 0;
                } else {
                    Console.WriteLine("Unknown command: " + args[0]);
                    PrintInfo();
                    Exit(false);
                }
            } else if (args != null && args.Length >= 1) {
                Console.WriteLine("Accepted arguments:");
                var cargs = args.Distinct().Where(a => a.StartsWith("--") == false).Where(s => string.IsNullOrEmpty(s.Trim())).ToArray();
                foreach (var path in cargs)
                    Console.WriteLine(path);

                foreach (var path in args.Where(path=>path.StartsWith("-")==false))
                    ProcessPath(path);

                fonts = fonts.Distinct().ToList();

                if (fonts.Count == 0) {
                    Console.WriteLine("No fonts were found.");
                    Exit();
                }
                Console.WriteLine();
            } else {
                haltonend = true;
                Console.WriteLine("Enter the search directories path [enter for working directory] [n/next to continue]");
                while (true) {
                    Console.Write(">");
                    var path = Console.ReadLine();
                    if (string.IsNullOrEmpty(path)) {
                        path = Directory.GetCurrentDirectory();
                        if (Directory.Exists(path) == false)
                            path = ExecutingDir.FullName;
                    }
                    if (path == "n" || path == "next") {
                        break;
                    }

                    ProcessPath(path);
                }

                Console.WriteLine();
                Console.WriteLine("Searching...");

                Console.WriteLine();
                if (fonts.Count == 0) {
                    Console.WriteLine("No fonts were found.");
                    Exit();
                }
                fonts = fonts.Distinct().ToList();
                Console.WriteLine($"About to attempt to install {fonts.Count} fonts...");
                AskContinue();
                Console.WriteLine();
            }

            DirectoryInfo cache = null;
            try {
                cache = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), $"cache_" + Path.ChangeExtension(Path.GetRandomFileName(), null)));
                if (!IsDirectoryWritable(cache))
                    throw new Exception($"Couldn't write to generated temporary directory. Test has failed, fallbacking.\n{cache.FullName}"); //fallback
            } catch (Exception e) {
                try {
                    //once you have the path you get the directory with:
                    cache = Directory.CreateDirectory(Path.Combine(ExecutingDir.FullName, "cache"));
                    if (!IsDirectoryWritable(cache))
                        throw new Exception($"Couldn't write to the generated temporary directory. Test has failed, failed.\n{cache.FullName}"); //fallback
                } catch (Exception ee) {
                    Console.WriteLine($"Failed creating temporary directory at\n{cache?.FullName ?? ""}\n{e}\n{ee}");
                    Exit();
                    return 1;
                }
            }

            Console.WriteLine("Cache Directory: " + cache.FullName);
            Console.WriteLine();
            Console.WriteLine("Copying...");

            foreach (var font in fonts)
                try {
                    var dest = Path.Combine(cache.FullName, font.Contains("\\") || font.Contains("/") ? Path.GetFileName(font) : font);
                    if (File.Exists(dest))
                        continue;
                    File.Copy(font, dest, false);
                    MarkForDeletion(dest);
                } catch (IOException) { }
            //export fontreg.exe
            var installer = Path.Combine(cache.FullName, "FontReg.exe");
            EmbeddedResource.ExportZipResource(cache, is64BitOperatingSystem ? "x64.zip" : "x86.zip", Assembly.GetEntryAssembly());
            MarkForDeletion(installer);

            var info = new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = $"/C FontReg.exe /move",
                WorkingDirectory = Path.GetDirectoryName(installer),
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Console.WriteLine($"Installing {fonts.Count} fonts......");
            var p = Process.Start(info);
            p.WaitForExit();

            File.Delete(installer);
            Directory.Delete(cache.FullName, true);

            Console.WriteLine();
            Console.WriteLine("Exit code: " + (p.ExitCode == 0 ? "Succesfull" : "Errornous"));

            Exit(haltonend);
            return p.ExitCode;
        }

        private static void AskContinue() {
            if (!Ask("Continue..?"))
                Exit();
        }

        private static void PrintInfo() {
            Console.WriteLine("Commands:");
            Console.WriteLine("--cleanup / --clear:");
            Console.WriteLine("\tWill remove any stale font registrations in the registry.");
            Console.WriteLine("\tWill repair any missing font registrations for fonts located in the C:\\Windows\\Fonts directory(this step\n\twill be skipped for .fon fonts if FontReg cannot determine which\n\tfonts should have \"hidden\" registrations).");
            Console.WriteLine("\"c:/path1\" \"c:/font.ttf\" ... \"./relativedir/\" ");
            Console.WriteLine("\tWill add or replace a font from given path/folder.");
            Console.WriteLine("\tNote: Folders are deep-searched recusively.");
        }

        private static void RunCleanup() {
            DirectoryInfo cache = null;

            try {
                //once you have the path you get the directory with:
                cache = new DirectoryInfo(Path.Combine(ExecutingDir.FullName, "cache"));
                if (!cache.Exists)
                    cache.Create();
            } catch (Exception e) {
                Console.WriteLine($"Failed creating temporary directory at\n{cache?.FullName ?? "%current_exe%/cache"}\n{e}");
                Exit();
                return;
            }

            Console.WriteLine("Cache Directory: " + cache);

            var installer = Path.Combine(cache.FullName, "FontReg.exe");
            EmbeddedResource.ExportZipResource(cache, is64BitOperatingSystem ? "x64.zip" : "x86", Assembly.GetEntryAssembly());
            Console.WriteLine("Performing font registry cleanup...");

            var info = new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = $"/C FontReg.exe",
                WorkingDirectory = Path.GetDirectoryName(installer),
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            var p = Process.Start(info);
            p.WaitForExit();

            File.Delete(installer);
            Directory.Delete(cache.FullName, true);

            Console.WriteLine();
            Console.WriteLine("Exit code: " + (p.ExitCode == 0 ? "Succesfull" : "Errornous"));

            Exit(false);
        }

        public static IEnumerable<FileInfo> GetFiles(DirectoryInfo dir, params string[] patterns) {
            return patterns?.SelectMany(pattern => dir.EnumerateFiles(pattern, SearchOption.AllDirectories)) ?? dir.EnumerateFiles("*.*", SearchOption.AllDirectories);
        }

        private static bool Ask(string question) {
            Console.Write($"{question} [y/n]: ");
            var cr = Console.ReadLine();
            if (cr == "n")
                return false;
            if (cr != "y")
                return Ask(question);
            return true;
        }

        private static void Exit(bool halt = true) {
            if (halt) {
                Console.WriteLine();
                Console.Write("Press Enter to exit...");
                Console.ReadLine();
            }
            Environment.Exit(0);
        }

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        public static bool InternalCheckIsWow64() {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
                using (var p = Process.GetCurrentProcess()) {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal))
                        return false;
                    return retVal;
                }
            return false;
        }

        /// <summary>
        ///     Quick reliable way to check writing permissions.
        /// </summary>
        internal static bool IsDirectoryWritable(DirectoryInfo dirPath) {
            if (dirPath == null) return false;
            try {
                using (var fs = File.Create(Path.Combine(dirPath.FullName, Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose)) {
                    fs.WriteByte(1);
                }
                return true;
            } catch {
                return false;
            }
        }

        [Flags]
        private enum MoveFileFlags {
            None = 0,
            ReplaceExisting = 1,
            CopyAllowed = 2,
            DelayUntilReboot = 4,
            WriteThrough = 8,
            CreateHardlink = 16,
            FailIfNotTrackable = 32,
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, MoveFileFlags dwFlags);

        /// <summary>
        ///     Marks a file for deletion on restart.<br></br>
        ///     Note: it requires administrator permission and is used incase it is possible.
        /// </summary>
        public static bool MarkForDeletion(string path) {
            try {
                return MoveFileEx(path, null, MoveFileFlags.DelayUntilReboot);
            } catch {
                return false;
            }
        }
    }
}