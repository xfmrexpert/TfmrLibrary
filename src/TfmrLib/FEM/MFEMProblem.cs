using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TfmrLib.FEM
{
    public class MFEMProblem : FEMProblem
    {
        private string FindMFEMExecutable()
        {
            return "mfem-electromag";
        }

        private void WriteMFEMFile()
        {
            
        }

        public override void Solve()
        {
            string mfem_exe = FindMFEMExecutable();
            Console.WriteLine($"Using MFEM-ElectroMag at: {mfem_exe}");

            WriteMFEMFile();

            string args = $"{Filename}";

            if (ShowInTerminal)
            {
                var term = FindTerminal() ?? throw new Exception("No terminal emulator found (gnome-terminal/xterm/konsole).");
                var exitFile = Path.GetTempFileName();
                try
                {
                    using var p = new Process();
                    if (term.Contains("gnome-terminal"))
                    {
                        // Keep window open: capture exit code, then wait for Enter.
                        // --wait lets our process block until the bash command (including read) finishes.
                        p.StartInfo.FileName = term;
                        p.StartInfo.Arguments =
                            $"--wait -- bash -lc \"'{mygetdp}' {args}; code=$?; echo $code > '{exitFile}'; " +
                            "echo; echo 'MFEM-ElectroMag exited with code '$code'. Press Enter to close...'; read\"";
                    }
                    else if (term.Contains("xterm"))
                    {
                        p.StartInfo.FileName = term;
                        p.StartInfo.Arguments =
                            $"-e bash -lc \"'{mygetdp}' {args}; code=$?; echo $code > '{exitFile}'; " +
                            "echo; echo 'MFEM-ElectroMag exited with code '$code'. Press Enter to close...'; read\"";
                    }
                    else // konsole
                    {
                        // --noclose keeps window by default, but we still add a read for consistency
                        p.StartInfo.FileName = term;
                        p.StartInfo.Arguments =
                            $"--noclose -e bash -lc \"'{mygetdp}' {args}; code=$?; echo $code > '{exitFile}'; " +
                            "echo; echo 'MFEM-ElectroMag exited with code '$code'. Press Enter to close...'; read\"";
                    }
                    p.StartInfo.UseShellExecute = false;
                    Console.WriteLine("Launching MFEM-ElectroMag in terminal:");
                    Console.WriteLine($"{mfem_exe} {args}");
                    p.Start();
                    p.WaitForExit();

                    int exitCode = 0;
                    if (File.Exists(exitFile))
                    {
                        var txt = File.ReadAllText(exitFile).Trim();
                        if (!int.TryParse(txt, out exitCode))
                            Console.WriteLine("Warning: could not parse exit code text: " + txt);
                    }
                    if (exitCode != 0)
                        throw new Exception($"MFEM-ElectroMag exited with code {exitCode}");
                }
                finally
                {
                    try { if (File.Exists(exitFile)) File.Delete(exitFile); } catch { }
                }
                return;
            }

            // Non-terminal (background) mode with simple timeout + live output to console
            int return_code = -999;
            object returnCodeLock = new();
            while (return_code < 0)
            {
                var sb = new StringBuilder();
                using var p = new Process();
                p.StartInfo.FileName = mygetdp;
                p.StartInfo.Arguments = args;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                p.OutputDataReceived += (s, a) => { if (a.Data != null) { Console.WriteLine(a.Data); sb.AppendLine(a.Data); } };
                p.ErrorDataReceived += (s, a) => { if (a.Data != null) { Console.WriteLine(a.Data); sb.AppendLine(a.Data); } };

                var timer = new System.Timers.Timer(60000);
                timer.Elapsed += (s, e) =>
                {
                    if (!p.HasExited)
                    {
                        try { p.Kill(true); } catch { }
                        Console.WriteLine("MFEM-ElectroMag killed (timeout).");
                        lock (returnCodeLock) return_code = -1;
                        timer.Stop();
                    }
                };

                Console.WriteLine("Running (background): " + mfem_exe + " " + args);
                p.Start();
                timer.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                timer.Stop();
                timer.Dispose();

                return_code = p.ExitCode;
                if (return_code > 0)
                    Console.WriteLine(sb.ToString());
                if (return_code != 0 && return_code != -1)
                    throw new Exception($"Failed to run MFEM-ElectroMag (exit {return_code})");
            }
        }
        
    }
}