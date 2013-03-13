using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;

namespace CoreExtensions
{
    public static class ProcessExtensions
    {
        public static Func<string,string> GetInterpreter = (file) => null;

        public static void Write(this Process proc, string msg)
        {
            proc.StandardInput.WriteLine(msg);
        }

        public static void Run(
            this Process proc,
            string command,
            string arguments,
            bool visible,
            string workingDir)
        {
            prepareInterpreter(ref command, ref arguments);
            prepareProcess(proc, command, arguments, visible, workingDir);
            proc.Start();
			proc.WaitForExit();
        }

        public static void Spawn(
            this Process proc,
            string command,
            string arguments,
            bool visible,
            string workingDir)
        {
            prepareInterpreter(ref command, ref arguments);
            prepareProcess(proc, command, arguments, visible, workingDir);
            proc.Start();
        }

        public static void Query(
            this Process proc,
            string command,
            string arguments,
            bool visible,
            string workingDir,
            Action<bool, string> onRecievedLine)
        {
            var process = proc;
            var retries = 0;
            var exitCode = 255;
            while (exitCode == 255 && retries < 5) {
                exitCode = query(process, command, arguments, visible, workingDir, onRecievedLine);
                retries++;
                // Seems to happen on linux when a file is beeing executed while being modified (locked)
                if (exitCode == 255) {
                    process = new Process();
                    Thread.Sleep(100);
                }
            }
        }

        private static int query(
            this Process proc,
            string command,
            string arguments,
            bool visible,
            string workingDir,
			Action<bool, string> onRecievedLine)
        {
            string tempFile = null;
            if (!prepareInterpreter(ref command, ref arguments)) {
                if (Environment.OSVersion.Platform != PlatformID.Unix &&
                    Environment.OSVersion.Platform != PlatformID.MacOSX)
                {
                    if (Path.GetExtension(command).ToLower() != ".exe") {
                        arguments = getBatchArguments(command, arguments, ref tempFile);
                        command = "cmd.exe";
                    }
                }
            }
			
			var exit = false;
            prepareProcess(proc, command, arguments, visible, workingDir);
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardInput = true;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.StartInfo.RedirectStandardError = true;

            DataReceivedEventHandler onOutputLine = 
                (s, data) => {
                    if (data.Data == null)
                        exit = true;
                    else
                        onRecievedLine(false, data.Data);
                };
            DataReceivedEventHandler onErrorLine = 
                (s, data) => {
                    if (data.Data == null)
                        exit = true;
                    else
                        onRecievedLine(true, data.Data);
                };

			proc.OutputDataReceived += onOutputLine;
            proc.ErrorDataReceived += onErrorLine;
            if (proc.Start())
            {
				proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
            }
            proc.OutputDataReceived -= onOutputLine;
            proc.ErrorDataReceived -= onErrorLine;
            if (tempFile != null && File.Exists(tempFile))
                File.Delete(tempFile);
            return proc.ExitCode;
        }

        private static bool prepareInterpreter(ref string command, ref string arguments) {
            var interpreter = GetInterpreter(command);
            if (interpreter != null) {
                command = interpreter;
                arguments = "\"" + command + "\" " + arguments;
                return true;
            }
            return false;
        }

        private static string getBatchArguments(string command, string arguments, ref string tempFile) {
            var illagalChars = new[] {"&", "<", ">", "(", ")", "@", "^", "|"};
            if (command.Contains(" ") ||
                illagalChars.Any(x => arguments.Contains(x))) {
                // Windows freaks when getting the | character
                // Have it run a temporary bat file with command as contents
                tempFile = Path.GetTempFileName() + ".bat";
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
                File.WriteAllText(tempFile, "\"" + command + "\" " + arguments);
                arguments = "/c " + tempFile;
            } else {
                arguments = "/c " + 
                    "^\"" + batchEscape(command) + "^\" " +
                    batchEscape(arguments);
            }
            return arguments;
        }

        private static string batchEscape(string text) {
            foreach (var str in new[] { "^", " ", "&", "(", ")", "[", "]", "{", "}", "=", ";", "!", "'", "+", ",", "`", "~", "\"" })
                text = text.Replace(str, "^" + str);
            return text;
        }
        
        private static void prepareProcess(
            Process proc,
            string command,
            string arguments,
            bool visible,
            string workingDir)
        {
            var info = new ProcessStartInfo(command, arguments);
            info.CreateNoWindow = !visible;
            if (!visible)
                info.WindowStyle = ProcessWindowStyle.Hidden;
            info.WorkingDirectory = workingDir;
            proc.StartInfo = info;
        }                
    }
}
