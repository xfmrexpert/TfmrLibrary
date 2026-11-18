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
using netDxf.Collections;

namespace FEMLib
{
    public class NoumenaProblem : FEMProblem
    {
        public string Filename { get; set; } = "case.json";

        public string? ResultsFile { get; set; }

        public FEMResults Results { get; private set; }

        private string FindNoumenaExecutable()
        {
            return "noumena";
        }

        private void WriteNoumenaFile()
        {
            var meshPath = MeshPath;

            var caseDir = Path.GetDirectoryName(Path.GetFullPath(Filename));
            if (!string.IsNullOrEmpty(caseDir))
                Directory.CreateDirectory(caseDir);

            var resultsFile = ResultsFile;
            if (!Path.IsPathFullyQualified(resultsFile))
            {
                resultsFile = caseDir + "/" + resultsFile;
            }

            // Write out JSON file for the Noumena solver
            using var stream = new FileStream(Filename, FileMode.Create, FileAccess.Write);
            //using var stream = new StreamWriter(Filename);
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteStartObject("simulation");
                writer.WriteString("physics_type", PhysicsType.ToString().ToLower());
                writer.WriteString("analysis_type", AnalysisType switch { AnalysisType.Field => "field", AnalysisType.CouplingMatrix => "coupling_matrix", _ => AnalysisType.ToString().ToLowerInvariant() });
                writer.WriteString("geometry_type", GeometryType.ToString().ToLower());
                writer.WriteString("mesh", meshPath);
                writer.WriteNumber("order", 2);
                writer.WriteNumber("solver_tolerance", 1e-12);
                writer.WriteNumber("solver_max_iter", 2000);
                writer.WriteNumber("solver_print_level", 1);

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
                    writer.WriteString("name", material.Name);
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
            WriteNoumenaFile();

            string args = $"{Filename}";

            using var process = new Process();
            process.StartInfo.FileName = "noumena";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.ArgumentList.Add(Filename);

            var output = new StringBuilder();
            var errors = new List<string>();
            var stderr = new StringBuilder();

            process.Start();

            Task exitTask = process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new Exception("No beuno. Noumena shit the bed.");
            }
            else
            {
                TryLoadSolution();
            }

        }

        public void TryLoadSolution()
        {
            if (string.IsNullOrEmpty(ResultsFile))
            {
                throw new Exception("ResultsFile was not set");
            }

            if (!File.Exists(ResultsFile))
            {
                throw new Exception("ResultsFile doesn't exist");
            }

            try
            {
                Results = NoumenaResultsReader.Read(ResultsFile);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to load FEM results from '{ResultsFile}': {ex.Message}");
            }
        }

    }
}