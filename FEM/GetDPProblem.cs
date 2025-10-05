using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TfmrLib.FEM
{
    public class GetDPProblem : FEMProblem
    {
        public string Filename { get; set; } = "case.pro"; // default
        public string MeshFile { get; set; } = "case.msh"; // default
        public string? GetDPPath { get; set; }  // configurable
        public int Order { get; set; } = 1;
        public bool ShowInTerminal { get; set; } = false;

        private string FindGetDPExecutable()
        {
            if (!string.IsNullOrEmpty(GetDPPath) && File.Exists(GetDPPath))
                return GetDPPath;

            var envPath = Environment.GetEnvironmentVariable("GETDP_PATH");
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                return envPath;

            string[] relativePaths = { "./bin/getdp", "../bin/getdp", "../../../bin/getdp" };
            foreach (var rel in relativePaths)
                if (File.Exists(rel)) return Path.GetFullPath(rel);

            // PATH search
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    var candidate = Path.Combine(dir, "getdp");
                    if (File.Exists(candidate)) return candidate;
                }
            }

            string[] systemPaths = { "/usr/bin/getdp", "/usr/local/bin/getdp" };
            foreach (var sys in systemPaths)
                if (File.Exists(sys)) return sys;

            throw new FileNotFoundException("getdp executable not found. Set GetDPPath property or GETDP_PATH environment variable.");
        }

        private string? FindTerminal()
        {
            string[] terminals = { "cosmic-term", "gnome-terminal", "xterm", "konsole" };
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    foreach (var term in terminals)
                    {
                        var candidate = Path.Combine(dir, term);
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }
            return null;
        }

        protected virtual void WriteGetDPFile()
        {
            Console.WriteLine($"Writing GetDP file to {Filename}");
            // Check if directory exists, create if not
            var dir = Path.GetDirectoryName(Filename);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var f = File.CreateText(Filename);

            f.WriteLine($"FE_Order = {Order};");

            f.WriteLine("Group{");
            foreach (var region in Regions)
            {
                f.WriteLine($"  {region.Name} = Region[{{{string.Join(", ", region.Tags)}}}];");
            }
            foreach (var bc in BoundaryConditions)
            {
                f.WriteLine($"  {bc.Name} = Region[{{{string.Join(", ", bc.Tags)}}}];");
            }
            // Material groups (aggregate tags of all regions sharing the material)
            foreach (var mat in Materials)
            {
                var tags = Regions
                           .Where(r => r.Material == mat)
                           .SelectMany(r => r.Tags)
                           .Distinct()
                           .ToList();
                if (tags.Count > 0)
                    f.WriteLine($"  {mat.Name} = Region[{{{string.Join(", ", tags)}}}];");
            }
            f.WriteLine($"  ProblemDomain = Region[{{{string.Join(", ", Regions.SelectMany(r => r.Tags).Distinct())}}}];");
            f.WriteLine("}");


            // Collect all property names
            var allProps =
                Materials.SelectMany(m => m.Properties.Keys)
                         .Concat(Regions.SelectMany(r => r.Properties.Keys))
                         .Distinct()
                         .ToList();

            if (allProps.Count() > 0)
            {
                f.WriteLine("Function{");
                foreach (var prop in allProps)
                {

                    // Material-level assignment
                    foreach (var mat in Materials)
                        if (mat.Properties.TryGetValue(prop, out var valMat))
                            f.WriteLine($"  {prop}[{mat.Name}] = {valMat:R};");

                    // Region-specific properties override material properties
                    foreach (var region in Regions)
                        if (region.Properties.TryGetValue(prop, out var valProp))
                            f.WriteLine($"  {prop}[{region.Name}] = {valProp:R};");
                }
                f.WriteLine("}");
            }
            f.Close();
        }

        public override void Solve()
        {
            string mygetdp = FindGetDPExecutable();
            Console.WriteLine($"Using getdp at: {mygetdp}");

            WriteGetDPFile();

            string args = $"{Filename} -msh {MeshFile} -solve Magnetodynamics2D_av -pos dyn -v 5";

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
                            "echo; echo 'getdp exited with code '$code'. Press Enter to close...'; read\"";
                    }
                    else if (term.Contains("xterm"))
                    {
                        p.StartInfo.FileName = term;
                        p.StartInfo.Arguments =
                            $"-e bash -lc \"'{mygetdp}' {args}; code=$?; echo $code > '{exitFile}'; " +
                            "echo; echo 'getdp exited with code '$code'. Press Enter to close...'; read\"";
                    }
                    else // konsole
                    {
                        // --noclose keeps window by default, but we still add a read for consistency
                        p.StartInfo.FileName = term;
                        p.StartInfo.Arguments =
                            $"--noclose -e bash -lc \"'{mygetdp}' {args}; code=$?; echo $code > '{exitFile}'; " +
                            "echo; echo 'getdp exited with code '$code'. Press Enter to close...'; read\"";
                    }
                    p.StartInfo.UseShellExecute = false;
                    Console.WriteLine("Launching getdp in terminal:");
                    Console.WriteLine($"{mygetdp} {args}");
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
                        throw new Exception($"getdp exited with code {exitCode}");
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
                        Console.WriteLine("getdp killed (timeout).");
                        lock (returnCodeLock) return_code = -1;
                        timer.Stop();
                    }
                };

                Console.WriteLine("Running (background): " + mygetdp + " " + args);
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
                    throw new Exception($"Failed to run getdp (exit {return_code})");
            }
        }
    }

}