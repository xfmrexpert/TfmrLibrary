using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public enum MFEMProgressEventType
    {
        Operation,
        Message
    }

    public sealed record MFEMProgressEvent(
        MFEMProgressEventType EventType,
        string? Name = null,
        string? State = null,
        double? ElapsedSeconds = null,
        string? Level = null,
        string? Message = null);

    public class MFEMProblem : FEMProblem
    {
        public string Filename { get; set; } = "case.json";
        public bool ShowInTerminal { get; set; } = false;

        public event Action<MFEMProgressEvent>? ProgressChanged;

        /// <summary>
        /// Optional adaptive mesh refinement (AMR) configuration. When non-null and
        /// <see cref="AmrSettings.Enabled"/> is true, an <c>"amr"</c> block is written
        /// into the <c>"simulation"</c> section of the solver's case.json so the
        /// MFEM-ElectroMag solver runs its estimate→mark→refine→re-solve loop and writes
        /// the final refined mesh + fields back through the usual results.msh contract.
        /// Null (the default) reproduces the previous single-solve behaviour exactly.
        /// </summary>
        public AmrSettings? Amr { get; set; }

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

                // The "amr" block is emitted only when adaptive refinement is requested,
                // so older solver builds (and the default config) see the exact JSON they
                // saw before. When present, "output_gmsh" gains a trailing comma so the
                // block remains valid JSON.
                bool emitAmr = Amr is { Enabled: true };
                stream.WriteLine($"\t\t\"output_gmsh\": true{(emitAmr ? "," : "")}");
                if (emitAmr)
                {
                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                    var amr = Amr!;
                    stream.WriteLine("\t\t\"amr\": {");
                    stream.WriteLine("\t\t\t\"enabled\": true,");
                    stream.WriteLine($"\t\t\t\"max_iterations\": {amr.MaxIterations.ToString(inv)},");
                    stream.WriteLine($"\t\t\t\"max_dofs\": {amr.MaxDofs.ToString(inv)},");
                    stream.WriteLine($"\t\t\t\"error_fraction\": {amr.ErrorFraction.ToString("R", inv)},");
                    stream.WriteLine($"\t\t\t\"error_tolerance\": {amr.ErrorTolerance.ToString("R", inv)},");
                    stream.WriteLine($"\t\t\t\"conforming\": {(amr.Conforming ? "true" : "false")}");
                    stream.WriteLine("\t\t}");
                }
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

            using var process = new Process();
            process.StartInfo.FileName = mfem_exe;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.ArgumentList.Add(Filename);
            process.StartInfo.ArgumentList.Add("--machine-readable");

            var output = new StringBuilder();
            var errors = new List<string>();
            var stderr = new StringBuilder();

            Console.WriteLine($"Running (background): {mfem_exe} {Filename} --machine-readable");
            process.Start();

            Task stdoutTask = ReadStandardOutputAsync(process.StandardOutput, output, errors);
            Task stderrTask = ReadStandardErrorAsync(process.StandardError, stderr);
            Task exitTask = process.WaitForExitAsync();

            if (!exitTask.Wait(TimeSpan.FromMinutes(6)))
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
                process.WaitForExit();
                Task.WhenAll(stdoutTask, stderrTask).GetAwaiter().GetResult();
                throw new TimeoutException("MFEM-ElectroMag was terminated after exceeding the six-minute timeout.");
            }

            Task.WhenAll(stdoutTask, stderrTask).GetAwaiter().GetResult();

            if (process.ExitCode != 0)
            {
                string detail = errors.Count > 0
                    ? string.Join(Environment.NewLine, errors)
                    : stderr.ToString().TrimEnd();

                if (detail.Length == 0)
                    detail = output.ToString().TrimEnd();

                const int maxTail = 4000;
                if (detail.Length > maxTail)
                    detail = "...(truncated)..." + Environment.NewLine + detail[^maxTail..];

                string message = $"Failed to run MFEM-ElectroMag (exit {process.ExitCode}).";
                if (detail.Length > 0)
                    message += $"{Environment.NewLine}{detail}";
                throw new Exception(message);
            }

            TryLoadSolution();
        }

        private async Task ReadStandardOutputAsync(
            StreamReader reader,
            StringBuilder output,
            List<string> errors)
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                Console.WriteLine(line);
                output.AppendLine(line);

                if (!TryParseProgress(line, out MFEMProgressEvent? progress))
                    continue;

                if (progress.Level == "error" && !string.IsNullOrWhiteSpace(progress.Message))
                    errors.Add(progress.Message);

                ProgressChanged?.Invoke(progress);
            }
        }

        private static async Task ReadStandardErrorAsync(StreamReader reader, StringBuilder stderr)
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                Console.WriteLine(line);
                stderr.AppendLine(line);
            }
        }

        private static bool TryParseProgress(string line, out MFEMProgressEvent? progress)
        {
            progress = null;

            try
            {
                using JsonDocument document = JsonDocument.Parse(line);
                JsonElement root = document.RootElement;
                if (!root.TryGetProperty("event", out JsonElement eventElement) ||
                    eventElement.ValueKind != JsonValueKind.String)
                    return false;

                switch (eventElement.GetString())
                {
                    case "operation":
                        if (!TryGetString(root, "name", out string? name) ||
                            !TryGetString(root, "state", out string? state) ||
                            state is not ("started" or "completed" or "failed"))
                            return false;

                        double? elapsedSeconds = null;
                        if (root.TryGetProperty("elapsed_seconds", out JsonElement elapsedElement) &&
                            elapsedElement.ValueKind == JsonValueKind.Number &&
                            elapsedElement.TryGetDouble(out double elapsed))
                            elapsedSeconds = elapsed;

                        progress = new MFEMProgressEvent(
                            MFEMProgressEventType.Operation,
                            Name: name,
                            State: state,
                            ElapsedSeconds: elapsedSeconds);
                        return true;

                    case "message":
                        if (!TryGetString(root, "level", out string? level) ||
                            !TryGetString(root, "message", out string? message) ||
                            level is not ("status" or "solver" or "diagnostic" or "warning" or "error"))
                            return false;

                        progress = new MFEMProgressEvent(
                            MFEMProgressEventType.Message,
                            Level: level,
                            Message: message);
                        return true;

                    default:
                        return false;
                }
            }
            catch (JsonException exception)
            {
                Console.WriteLine($"Malformed MFEM-ElectroMag machine-readable output: {exception.Message}");
                return false;
            }
        }

        private static bool TryGetString(JsonElement element, string propertyName, out string? value)
        {
            value = null;
            if (!element.TryGetProperty(propertyName, out JsonElement property) ||
                property.ValueKind != JsonValueKind.String)
                return false;

            value = property.GetString();
            return value != null;
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

    /// <summary>
    /// Configuration for the MFEM-ElectroMag solver's adaptive mesh refinement (AMR)
    /// loop. The solver estimates a per-element error (Zienkiewicz–Zhu on the recovered
    /// E-field), marks the worst elements, performs a <b>conforming</b> triangular
    /// refinement (no hanging nodes), and re-solves until a stopping criterion is met.
    /// Conforming refinement keeps the returned mesh compatible with the existing
    /// results consumers (triangle locator, P1 field sampler, mesh renderer) without
    /// any changes on the C# side.
    /// </summary>
    public sealed class AmrSettings
    {
        /// <summary>Master switch. When false, no <c>"amr"</c> block is emitted and the
        /// solver performs its usual single solve on the supplied mesh.</summary>
        public bool Enabled { get; set; } = false;

        /// <summary>Maximum number of refine→re-solve iterations.</summary>
        public int MaxIterations { get; set; } = 5;

        /// <summary>Stop once the global degrees of freedom exceed this budget (safety
        /// cap against runaway refinement). Non-positive disables the cap.</summary>
        public long MaxDofs { get; set; } = 2_000_000;

        /// <summary>Fraction of the total error used by the bulk (Dörfler) marking
        /// strategy: mark the smallest set of elements whose summed error reaches this
        /// fraction of the total. Range (0, 1]; smaller refines more conservatively.</summary>
        public double ErrorFraction { get; set; } = 0.7;

        /// <summary>Optional absolute stopping tolerance on the global estimated error.
        /// Non-positive means "ignore" (rely on iteration / DOF caps instead).</summary>
        public double ErrorTolerance { get; set; } = 0.0;

        /// <summary>Require conforming (hanging-node-free) refinement. Must remain true
        /// for the current C# results pipeline; exposed so the contract is explicit.</summary>
        public bool Conforming { get; set; } = true;
    }
}