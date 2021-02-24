using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace BuildTool
{
    public static class Utils
    {
        private static readonly Lazy<string> processDir =
            new Lazy<string>(() => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));

        public static string ProcessDir => processDir.Value;

        public static string GeneratedOutputDir => Path.Combine(ProcessDir, "generated_files");

        public static int ExecuteShell(string command, string workingDirectory = null)
        {
            return ExecuteShell(new[] {command}, workingDirectory);
        }
        
        public static int ExecuteShell(IEnumerable<string> commands, string workingDirectory = null)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = Directory.Exists(workingDirectory) ? workingDirectory : GeneratedOutputDir,
            };
            using var process = new Process
            {
                StartInfo = psi
            };
            if (!process.Start())
            {
                return 1;
            }

#if DEBUG
            process.OutputDataReceived += (sender, e) => { Console.WriteLine(e.Data); };
            process.ErrorDataReceived += (sender, e) => { Console.WriteLine(e.Data); };
#endif
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using (var sw = process.StandardInput)
            {
                foreach (var cmd in commands)
                {
                    sw.WriteLine(cmd);
                }
            }
            process.WaitForExit();
            return process.ExitCode;
        }

        public static string PostfixBackslash(string text)
        {
            if (text == null)
            {
                return null;
            }
            if (text == "")
            {
                return "\\";
            }
            return text.TrimEnd('\\') + '\\';
        }
    }
}
