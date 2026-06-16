using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;

namespace TfmrLib.FEM
{
    public class MFEMProblem : FEMProblem
    {
        public string Filename { get; set; } = "case.json";
        public bool ShowInTerminal { get; set; } = false;

        /// <summary>
        /// Path the solver will write results to (Gmsh MSH 2.2 ASCII, with $NodeData /
        /// $ElementNodeData / $ElementData views). Defaults to
        /// "&lt;MeshFile-without-extension&gt;.results.msh" (the solver writes its output
        /// next to the input mesh, not next to the case JSON).
        /// </summary>
        public string? ResultsFile { get; set; }

        /// <summary>
        /// Last error reported while loading the solver's output (or null on success).
        /// Useful for surfacing the reason no <see cref="FEMProblem.Solution"/> was set
        /// after <see cref="Solve"/> returns.
        /// </summary>
        public string? LastLoadError { get; private set; }

        private string FindMFEMExecutable()
        {
            // Allow developer override (e.g. point at the CMake build output).
            var fromEnv = Environment.GetEnvironmentVariable("MFEM_ELECTROMAG_EXE");
            if (!string.IsNullOrWhiteSpace(fromEnv) && File.Exists(fromEnv))
                return fromEnv;

            return "mfem-electromag";
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

        private void WriteMFEMFile()
        {
            // The solver resolves a relative "mesh" path relative to the case.json's own
            // directory, which is not necessarily where the mesh lives (e.g. a build-once /
            // solve-many flow writes one mesh but a per-scenario case.json in a subfolder).
            // Emit an absolute path so it resolves regardless of the JSON's location, and
            // use forward slashes so the string needs no backslash escaping in JSON.
            var meshPath = MeshPath;
            if (!string.IsNullOrEmpty(meshPath))
                meshPath = Path.GetFullPath(meshPath).Replace('\\', '/');

            // Write out JSON file for the MFEM-ElectroMag solver
            using (var stream = new StreamWriter(Filename))
            {
                stream.WriteLine("{");
                stream.WriteLine("\t\"simulation\": {");
                stream.WriteLine("\t\t\"physics_type\": \"electrostatics\",");
                stream.WriteLine($"\t\t\"mesh\": \"{meshPath}\",");
                stream.WriteLine("\t\t\"order\": 2,");
                stream.WriteLine($"\t\t\"geometry_type\": \"{GeometryType.ToString().ToLower()}\",");
                stream.WriteLine("\t\t\"solver_tolerance\": 1e-12,");
                stream.WriteLine("\t\t\"solver_max_iter\": 2000,");
                stream.WriteLine("\t\t\"solver_print_level\": 1,");
                stream.WriteLine("\t\t\"output_gmsh\": true");
                stream.WriteLine("\t},");
                stream.WriteLine("\t\"entity_groups\": [");
                foreach (var group in EntityGroups)
                {
                    stream.WriteLine("\t{");
                    stream.WriteLine($"\t\t\"name\": \"{group.Name}\",");
                    stream.WriteLine($"\t\t\"dim\": {group.Dimension},");
                    stream.WriteLine($"\t\t\"attribute_ids\": [{string.Join(',', group.AttributeIds)}]");
                    if (group != EntityGroups[^1])
                    {
                        stream.WriteLine("\t},");
                    }
                    else
                    {
                        stream.WriteLine("\t}");
                    }
                }
                stream.WriteLine("\t],");
                stream.WriteLine("\t\"materials\": [");
                foreach (var material in Materials)
                {
                    stream.WriteLine("\t{");
                    stream.WriteLine($"\t\t\"name\": \"{material.Name}\",");
                    stream.WriteLine("\t\t\"properties\": {");
                    foreach (var prop in material.Properties)
                    {
                        stream.WriteLine($"\t\t\t\"{prop.Key}\": \t{prop.Value}");
                    }
                    stream.WriteLine("\t\t}");
                    if (material != Materials.Last())
                    {
                        stream.WriteLine("\t},");
                    }
                    else
                    {
                        stream.WriteLine("\t}");
                    }
                }
                stream.WriteLine("\t],");
                stream.WriteLine("\t\"regions\": [");
                foreach (var region in Regions)
                {
                    stream.WriteLine("\t{");
                    stream.WriteLine($"\t\t\"name\": \"{region.Name}\",");
                    stream.WriteLine($"\t\t\"entity_group\": \"{region.EntityGroupName}\",");
                    stream.WriteLine($"\t\t\"material\": {Materials.IndexOf(region.Material)+1}");
                    if (region != Regions.Last())
                    {
                        stream.WriteLine("\t},");
                    }
                    else
                    {
                        stream.WriteLine("\t}");
                    }
                }
                stream.WriteLine("\t],");
                stream.WriteLine("\t\"boundaries\": [");
                foreach (var bc in BoundaryConditions)
                {
                    stream.WriteLine("\t{");
                    stream.WriteLine($"\t\t\"name\": \"{bc.Name}\",");
                    stream.WriteLine($"\t\t\"entity_group\": \"{bc.EntityGroupName}\",");
                    if (bc is NeumannBoundaryCondition neumann_bc)
                    {
                        stream.WriteLine($"\t\t\"type\": \"Neumann\",");
                        stream.WriteLine($"\t\t\"value\": {neumann_bc.Flux}");
                    }
                    else if (bc is DirichletBoundaryCondition dirichlet_bc)
                    {
                        stream.WriteLine($"\t\t\"type\": \"Dirichlet\",");
                        stream.WriteLine($"\t\t\"value\": {dirichlet_bc.Potential}");
                    }
                    if (bc != BoundaryConditions.Last())
                    {
                        stream.WriteLine("\t},");
                    }
                    else
                    {
                        stream.WriteLine("\t}");
                    }
                }
                stream.WriteLine("\t],");
                stream.WriteLine("\t\"terminals\": [");
                foreach (var term in Terminals)
                {
                    stream.WriteLine("\t{");
                    stream.WriteLine($"\t\t\"name\": \"{term.Name}\",");
                    stream.WriteLine($"\t\t\"entity_group\": \"{term.EntityGroup.Name}\"");
                    
                    if (term != Terminals.Last())
                    {
                        stream.WriteLine("\t},");
                    }
                    else
                    {
                        stream.WriteLine("\t}");
                    }
                }
                stream.WriteLine("\t],");
                stream.WriteLine("\t\"scenarios\": [");
                foreach (var scenario in Scenarios)
                {
                    stream.WriteLine("\t{");
                    stream.WriteLine($"\t\t\"name\": \"{scenario.Name}\",");
                    stream.WriteLine($"\t\t\"excitations\": [");
                    foreach (var exc in scenario.Excitations)
                    {
                        stream.WriteLine("\t\t{");
                        stream.WriteLine($"\t\t\t\"terminal\": \"{exc.Terminal.Name}\",");
                        stream.WriteLine($"\t\t\t\"value\": {exc.Value}");
                        if (exc != scenario.Excitations.Last())
                        {
                            stream.WriteLine("\t\t},");
                        }
                        else
                        {
                            stream.WriteLine("\t\t}");
                        }
                    }
                    stream.WriteLine("\t\t]");
                    if (scenario != Scenarios.Last())
                    {
                        stream.WriteLine("\t},");
                    }
                    else
                    {
                        stream.WriteLine("\t}");
                    }
                }
                stream.WriteLine("\t]");
                stream.WriteLine("}");
            }
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
                            $"--wait -- bash -lc \"'{mfem_exe}' {args}; code=$?; echo $code > '{exitFile}'; " +
                            "echo; echo 'MFEM-ElectroMag exited with code '$code'. Press Enter to close...'; read\"";
                    }
                    else if (term.Contains("xterm"))
                    {
                        p.StartInfo.FileName = term;
                        p.StartInfo.Arguments =
                            $"-e bash -lc \"'{mfem_exe}' {args}; code=$?; echo $code > '{exitFile}'; " +
                            "echo; echo 'MFEM-ElectroMag exited with code '$code'. Press Enter to close...'; read\"";
                    }
                    else // konsole
                    {
                        // --noclose keeps window by default, but we still add a read for consistency
                        p.StartInfo.FileName = term;
                        p.StartInfo.Arguments =
                            $"--noclose -e bash -lc \"'{mfem_exe}' {args}; code=$?; echo $code > '{exitFile}'; " +
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
                TryLoadSolution();
                return;
            }

            // Non-terminal (background) mode with simple timeout + live output to console
            int return_code = -999;
            object returnCodeLock = new();
            while (return_code < 0)
            {
                var sb = new StringBuilder();
                using var p = new Process();
                p.StartInfo.FileName = mfem_exe;
                p.StartInfo.Arguments = args;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                p.OutputDataReceived += (s, a) => { if (a.Data != null) { Console.WriteLine(a.Data); sb.AppendLine(a.Data); } };
                p.ErrorDataReceived += (s, a) => { if (a.Data != null) { Console.WriteLine(a.Data); sb.AppendLine(a.Data); } };

                var timer = new System.Timers.Timer(360000);
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

            TryLoadSolution();
        }

        private void TryLoadSolution()
        {
            LastLoadError = null;

            if (string.IsNullOrEmpty(ResultsFile))
            {
                LastLoadError = "ResultsFile path was not set.";
                Console.WriteLine(LastLoadError);
                return;
            }

            if (!File.Exists(ResultsFile))
            {
                // Try a few common alternates next to the mesh file before giving up.
                var meshDir = !string.IsNullOrEmpty(MeshPath)
                    ? Path.GetDirectoryName(MeshPath)
                    : Path.GetDirectoryName(ResultsFile);
                string? found = null;
                if (!string.IsNullOrEmpty(meshDir) && Directory.Exists(meshDir))
                {
                    foreach (var pattern in new[] { "*.results.msh", "results*.msh", "*solution*.msh" })
                    {
                        var hits = Directory.GetFiles(meshDir, pattern);
                        if (hits.Length > 0)
                        {
                            // Prefer the newest file.
                            Array.Sort(hits, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
                            found = hits[0];
                            break;
                        }
                    }
                }

                if (found == null)
                {
                    LastLoadError = $"MFEM-ElectroMag did not produce results file '{ResultsFile}'.";
                    Console.WriteLine(LastLoadError);
                    return;
                }

                Console.WriteLine($"Results file '{ResultsFile}' not found; using '{found}' instead.");
                ResultsFile = found;
            }

            try
            {
                Solution = FEMSolution.Load(ResultsFile);
                Console.WriteLine($"Loaded FEM solution from {ResultsFile} " +
                    $"(nodal views: {Solution.NodalScalars.Count}, " +
                    $"element-nodal views: {Solution.ElementNodalFields.Count}, " +
                    $"element views: {Solution.ElementFields.Count}).");
            }
            catch (Exception ex)
            {
                LastLoadError = $"Failed to load FEM results from '{ResultsFile}': {ex.Message}";
                Console.WriteLine(LastLoadError);
            }
        }

    }
}