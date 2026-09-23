using netDxf.Collections;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MathNet.Numerics.LinearAlgebra;

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

    public class MFEMFile
    {
        
    }

    public class MFEMProblem : FEMProblem
    {
        public string Filename { get; set; } = "case.json";

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
        /// File the solver will write results to (HDF5 format only for now). Defaults to
        /// "&lt;MeshFile-without-extension&gt;.results.h5" (the solver writes its output
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

            // StreamWriter creates the file but not its parent directory, so make sure
            // the target folder exists (e.g. a relative "./Results/..." path under the
            // process working directory) before opening it.
            var caseDir = Path.GetDirectoryName(Path.GetFullPath(Filename));
            if (!string.IsNullOrEmpty(caseDir))
                Directory.CreateDirectory(caseDir);

            var resultsFile = ResultsFile;
            if (!Path.IsPathFullyQualified(resultsFile))
            {
                resultsFile = caseDir + "/" + resultsFile;
            }

            // Write out JSON file for the MFEM-ElectroMag solver
            using var stream = new FileStream(Filename, FileMode.Create, FileAccess.Write);
            //using var stream = new StreamWriter(Filename);
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteStartObject("simulation");
                writer.WriteString("physics_type",PhysicsType.ToString().ToLower());
                writer.WriteString("analysis_type", AnalysisType switch { AnalysisType.Field => "field", AnalysisType.CouplingMatrix => "coupling_matrix", _ => AnalysisType.ToString().ToLowerInvariant() });
                writer.WriteString("geometry_type", GeometryType.ToString().ToLower());
                writer.WriteString("mesh", meshPath);
                writer.WriteNumber("order", 2);
                writer.WriteNumber("solver_tolerance", 1e-12);
                writer.WriteNumber("solver_max_iter", 2000);
                writer.WriteNumber("solver_print_level", 1);

                // The "amr" block is emitted only when adaptive refinement is requested,
                // so older solver builds (and the default config) see the exact JSON they
                // saw before. When present, "output_gmsh" gains a trailing comma so the
                // block remains valid JSON.
                bool emitAmr = Amr is { Enabled: true };
                if (emitAmr)
                {
                    var inv = System.Globalization.CultureInfo.InvariantCulture;
                    var amr = Amr!;
                    writer.WriteStartObject("amr");
                    writer.WriteBoolean("enabled", true);
                    writer.WriteNumber("max_iterations", amr.MaxIterations);
                    writer.WriteNumber("max_dofs", amr.MaxDofs);
                    writer.WriteNumber("error_fraction", amr.ErrorFraction);
                    writer.WriteNumber("error_tolerance", amr.ErrorTolerance);
                    writer.WriteBoolean("conforming", amr.Conforming);
                    writer.WriteEndObject(); // end of amr block
                }
                writer.WriteEndObject(); // end of simulation block
                writer.WriteStartObject("output");
                writer.WriteBoolean("export_fields_for_coupling_matrix", false);
                writer.WriteStartObject("hdf5");
                writer.WriteString("file", resultsFile);
                writer.WriteEndObject(); // End of hdf5 block
                writer.WriteEndObject(); // End of output block
                writer.WriteStartArray("entity_groups");
                foreach (var group in EntityGroups)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", group.Name);
                    writer.WriteNumber("dim", group.Dimension);
                    writer.WriteStartArray("attribute_ids");
                    foreach (var id in group.AttributeIds)
                    {
                        writer.WriteNumberValue(id);
                    }
                    writer.WriteEndArray();
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteStartArray("materials");
                foreach (var material in Materials)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name",material.Name);
                    writer.WriteStartObject("properties");
                    foreach (var prop in material.Properties)
                    {
                        writer.WriteNumber(prop.Key, prop.Value);
                    }
                    writer.WriteEndObject(); // end of properties block
                    writer.WriteEndObject(); // end of material
                }
                writer.WriteEndArray();
                writer.WriteStartArray("regions");
                foreach (var region in Regions)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", region.Name);
                    writer.WriteString("entity_group", region.EntityGroupName);
                    writer.WriteString("material", region.Material.Name);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteStartArray("boundary_conditions");
                foreach (var bc in BoundaryConditions)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", bc.Name);
                    writer.WriteString("entity_group", bc.EntityGroupName);
                    if (bc is NeumannBoundaryCondition neumann_bc)
                    {
                        writer.WriteString("type", "neumann");
                        writer.WriteNumber("value", neumann_bc.Flux);
                    }
                    else if (bc is DirichletBoundaryCondition dirichlet_bc)
                    {
                        writer.WriteString("type", "dirichlet");
                        writer.WriteNumber("value", dirichlet_bc.Potential);
                    }
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteStartArray("terminals");
                foreach (var term in Terminals)
                {
                    writer.WriteStartObject();
                    writer.WriteString("name", term.Name);
                    writer.WriteString("quantity", term.ExcitationType.ToString().ToLower());
                    writer.WriteString("entity_group", term.EntityGroup.Name);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
                writer.WriteStartArray("scenarios");
                foreach (var scenario in Scenarios)
                {
                    writer.WriteStartObject(); // start scenario
                    writer.WriteString("name", scenario.Name);
                    if (PhysicsType == PhysicsType.Magnetoquasistatics)
                    {
                        if (scenario.Frequency is FrequencySpec.Scalar scalar)
                        {
                            writer.WriteNumber("frequency", scalar.Value);
                        }
                        else if (scenario.Frequency is FrequencySpec.List list)
                        {
                            writer.WriteStartArray("frequency");
                            foreach (var freq in list.Frequencies)
                            {
                                writer.WriteNumberValue(freq);
                            }
                            writer.WriteEndArray();
                        }
                        else if (scenario.Frequency is FrequencySpec.Sweep sweep)
                        {
                            writer.WriteStartObject("frequency");
                            writer.WriteString("scale", sweep.Scale switch { FrequencyScale.Linear => "linear", FrequencyScale.Log => "log", _ => sweep.Scale.ToString().ToLowerInvariant() });
                            writer.WriteNumber("start", sweep.Start);
                            writer.WriteNumber("stop", sweep.Stop);
                            writer.WriteNumber("points", sweep.Points);
                            writer.WriteEndObject();
                        }
                    }
                    if (scenario.Excitations is not null)
                    {
                        writer.WriteStartArray("excitations");
                        foreach (var exc in scenario.Excitations)
                        {
                            writer.WriteStartObject();
                            writer.WriteString("terminal", exc.TerminalName);
                            writer.WriteNumber("value", exc.Magnitude);
                            writer.WriteEndObject();
                        }
                        writer.WriteEndArray(); // end excitations array
                    }
                    writer.WriteEndObject(); // end scenario
                }
                writer.WriteEndArray(); // end scenarios array
                writer.WriteEndObject(); // end root
            }
        }

        public override void Solve()
        {
            ReportMessage("status", "Solving...");
            string mfem_exe = FindMFEMExecutable();
            ReportMessage("status", $"Using MFEM-ElectroMag at: {mfem_exe}");

            WriteMFEMFile();

            string args = $"{Filename}";

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

            ReportMessage("status", $"Running (background): {mfem_exe} {Filename} --machine-readable");
            process.Start();

            Task stdoutTask = ReadStandardOutputAsync(process.StandardOutput, output, errors);
            Task stderrTask = ReadStandardErrorAsync(process.StandardError, stderr);
            Task exitTask = process.WaitForExitAsync();

            //if (!exitTask.Wait(TimeSpan.FromMinutes(6)))
            //{
            //    if (!process.HasExited)
            //        process.Kill(entireProcessTree: true);
            //    process.WaitForExit();
            //    Task.WhenAll(stdoutTask, stderrTask).GetAwaiter().GetResult();
            //    throw new TimeoutException("MFEM-ElectroMag was terminated after exceeding the six-minute timeout.");
            //}

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
            else
            {
                TryLoadSolution();
            }

        }

        private async Task ReadStandardOutputAsync(
            StreamReader reader,
            StringBuilder output,
            List<string> errors)
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                output.AppendLine(line);

                // Non machine-readable lines (e.g. banners from linked libraries) are still
                // surfaced, but as progress messages so the host controls how they are shown.
                if (!TryParseProgress(line, out MFEMProgressEvent? progress))
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        ReportMessage("diagnostic", line);
                    continue;
                }

                if (progress.Level == "error" && !string.IsNullOrWhiteSpace(progress.Message))
                    errors.Add(progress.Message);

                ProgressChanged?.Invoke(progress);
            }
        }

        private async Task ReadStandardErrorAsync(StreamReader reader, StringBuilder stderr)
        {
            while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
            {
                stderr.AppendLine(line);
                if (!string.IsNullOrWhiteSpace(line))
                    ReportMessage("warning", line);
            }
        }

        private void ReportMessage(string level, string message) =>
            ProgressChanged?.Invoke(new MFEMProgressEvent(
                MFEMProgressEventType.Message,
                Level: level,
                Message: message));

        private bool TryParseProgress(string line, out MFEMProgressEvent? progress)
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
                ReportMessage("warning", $"Malformed MFEM-ElectroMag machine-readable output: {exception.Message}");
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

        public void TryLoadSolution()
        {
            LastLoadError = null;

            if (string.IsNullOrEmpty(ResultsFile))
            {
                LastLoadError = "ResultsFile was not set.";
                ReportMessage("error", LastLoadError);
                return;
            }

            if (!File.Exists(ResultsFile))
            {
                ReportMessage("error", $"Results file '{ResultsFile}' not found.");
                return;
            }

            try
            {
                Results = MFEMResultsReader.Read(ResultsFile);
                ReportMessage("status", $"Read FEM results from {ResultsFile} ");
            }
            catch (Exception ex)
            {
                LastLoadError = $"Failed to load FEM results from '{ResultsFile}': {ex.Message}";
                ReportMessage("error", LastLoadError);
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