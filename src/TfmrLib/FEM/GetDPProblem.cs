using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinAlg = MathNet.Numerics.LinearAlgebra;
using Vector_d = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace TfmrLib.FEM
{
    public class GetDPProblem : FEMProblem
    {
        public string Filename { get; set; } = "case.pro"; // default
        public string? GetDPPath { get; set; }  // configurable
        public int Order { get; set; } = 1;
        public bool ShowInTerminal { get; set; } = false;

        protected string formulation;
        protected string postop;

        private string FindGetDPExecutable()
        {
            if (!string.IsNullOrEmpty(GetDPPath) && File.Exists(GetDPPath))
                return GetDPPath;

            var envPath = Environment.GetEnvironmentVariable("GETDP_PATH");
            envPath = string.IsNullOrEmpty(envPath) ? null : envPath.Trim('"');
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                return envPath;

            string[] relativePaths = { "./getdp", "./bin/getdp", "../bin/getdp", "../../../bin/getdp" };
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

        protected virtual void WriteGetDPFile(Scenario sc)
        {
            Console.WriteLine($"Writing GetDP file to {Filename}");
            // Check if directory exists, create if not
            var dir = Path.GetDirectoryName(Filename);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var f = File.CreateText(Filename);

            f.WriteLine($"FE_Order = {Order};");

            f.WriteLine("Group{");
            foreach (var region in Regions)
            {
                var regionGroup = EntityGroups[region.Name];
                f.WriteLine($"  {region.Name} = Region[{{{string.Join(", ", regionGroup.AttributeIds)}}}];");
            }
            foreach (var bc in BoundaryConditions)
            {
                var bcGroup = EntityGroups[bc.Name];
                f.WriteLine($"  {bc.Name} = Region[{{{string.Join(", ", bcGroup.AttributeIds)}}}];");
            }
            // Material groups (aggregate tags of all regions sharing the material)
            foreach (var mat in Materials)
            {
                var entity_groups = Regions
                           .Where(r => r.Material == mat)
                           .Select(r => r.EntityGroupName)
                           .Distinct()
                           .ToList();
                if (entity_groups.Count > 0)
                {
                    var tags = entity_groups.SelectMany(g => EntityGroups[g].AttributeIds);
                    f.WriteLine($"  {mat.Name} = Region[{{{string.Join(", ", tags)}}}];");
                }
            }
            f.WriteLine($"  ProblemDomain = Region[{{{string.Join(", ", Regions.Where(r => r.Material is not null).SelectMany(r => EntityGroups[r.EntityGroupName].AttributeIds).Distinct())}}}];");
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
            foreach (var sc in Scenarios)
            {
                Console.WriteLine($"Solving scenario: {sc.Name}");
                SolveScenario(sc);
            }
        }

        protected void SolveScenario(Scenario sc)
        {
            string mygetdp = FindGetDPExecutable();
            Console.WriteLine($"Using getdp at: {mygetdp}");

            WriteGetDPFile(sc);

            string args = $"{Filename} -msh {MeshPath} -solve {formulation} -pos {postop} -v 5";

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

        public Matrix<double> Calc_Lmatrix_old(Transformer tfmr, double freq)
        {
            int order = 2; // Order of the finite element method

            int total_conductors = 0;
            foreach (Winding wdg in tfmr.Windings)
            {
                total_conductors += wdg.NumConductors;
            }

            Matrix<double> L_getdp = Matrix<double>.Build.Dense(total_conductors, total_conductors);

            int globalTurn = -1;
            int globalConductor = -1;
            foreach (var wdg in tfmr.Windings)
            {
                foreach (var seg in wdg.Segments)
                {
                    if (seg.Geometry != null)
                    {
                        var seg_geom = seg.Geometry;
                        for (int localTurn = 0; localTurn < seg_geom.NumTurns; localTurn++)
                        {
                            globalTurn++;
                            for (int localStrand = 0; localStrand < seg_geom.NumParallelConductors; localStrand++)
                            {
                                globalConductor++;
                                var row = CalcInductance(tfmr, globalTurn, localStrand, freq, order);
                                Console.WriteLine($"L matrix row for turn {globalTurn} strand {localStrand} at {freq.ToString("0.##E0")}Hz calculated.  Adding to row {globalConductor} of L matrix.");
                                Console.WriteLine($"L row: {string.Join(", ", row.ToArray())}");

                                // Take a lock to prevent two threads from writing to the matrix at the same time (just in case)
                                lock (L_getdp)
                                {
                                    L_getdp.SetRow(globalConductor, row);
                                }
                            }
                        }
                    }
                }
            }

            L_getdp = L_getdp.Multiply(2 * Math.PI); // Need to multiply by 2pi to go from Henries per radian to Henries for a complete turn

            Console.Write($"L total at {freq.ToString("0.##E0")}Hz: {(L_getdp * 2 * Math.PI).RowSums().Sum() / 1000.0}mH\n");

            // globalConductor = 0;
            // foreach (var wdg in tfmr.Windings)
            // {
            //     foreach (var seg in wdg.Segments)
            //     {
            //         if (seg.Geometry != null)
            //         {
            //             var seg_geom = seg.Geometry;
            //             for (int localTurn = 0; localTurn < seg_geom.NumTurns; localTurn++, globalTurn++)
            //             {
            //                 for (int localStrand = 0; localStrand < seg_geom.NumParallelConductors; localStrand++)
            //                 {
            //                     globalConductor++;
            //                     (double r, double z) = wdg.GetTurnMidpoint(localTurn);
            //                     L_getdp[globalConductor, t2] = L_getdp[globalConductor, t2] / r;
            //                 }
            //             }
            //         }
            //     }
            // }

            DelimitedWriter.Write($"L_getdp_{freq.ToString("0.00E0")}.csv", L_getdp, ",");
            return L_getdp;
        }

        private Vector_d CalcInductance(Transformer tfmr, int excitedTurn, int excitedStrand, double freq, int order = 1)
        {
            Console.WriteLine($"Frequency: {freq.ToString("0.##E0")} Turn: {excitedTurn}");

            var fem = new GetDPAxiMagProblem();
            fem.Order = order;
            //fem.MeshPath = meshFile;
            fem.Filename = $"./Results/{excitedTurn}/case.pro";

            fem.Frequency = freq;

            var oil = new Material("Oil")
            {
                Properties = new Dictionary<string, double> {
                { "mu_r", 1.0 } }
            };

            var paper = new Material("Paper")
            {
                Properties = new Dictionary<string, double> {
                { "mu_r", 1.0 } }
            };

            var copper = new Material("Copper")
            {
                Properties = new Dictionary<string, double> {
                { "mu_r", 1.0 },
                { "sigma", 5.96e7 } }
            };

            fem.Materials.Add(oil);
            fem.Materials.Add(paper);
            fem.Materials.Add(copper);
            fem.EntityGroups.Add(new EntityGroup() { Name = "InteriorDomain", Dimension = 2, AttributeIds = new List<int>() { tfmr.TagManager.GetTagByString("InteriorDomain") } });
            fem.Regions.Add(new Region() { Name = "InteriorDomain", EntityGroupName = "InteriorDomain", Material = oil });
            fem.EntityGroups.Add(new EntityGroup() { Name = "Axis", Dimension = 1, AttributeIds = new List<int>() { tfmr.TagManager.GetTagByString("CoreLeg") } });
            fem.BoundaryConditions.Add(new DirichletBoundaryCondition() { Name = "Axis", EntityGroupName = "Axis", Potential = 0.0 });
            fem.EntityGroups.Add(new EntityGroup() { Name = "Dirichlet", Dimension = 1, AttributeIds = new List<int>() { /* tfmr.TagManager.GetTagByString("CoreLeg"),  */tfmr.TagManager.GetTagByString("TopYoke"), tfmr.TagManager.GetTagByString("BottomYoke"), tfmr.TagManager.GetTagByString("RightEdge") } });
            fem.BoundaryConditions.Add(new DirichletBoundaryCondition() { Name = "Dirichlet", EntityGroupName = "Dirichlet", Potential = 0.0 });
            int globalTurn = 0;
            for (int wdgNum = 0; wdgNum < tfmr.Windings.Count; wdgNum++)
            {
                var wdg = tfmr.Windings[wdgNum];
                for (int segNum = 0; segNum < wdg.Segments.Count; segNum++)
                {
                    var seg = wdg.Segments[segNum];
                    if (seg.Geometry != null)
                    {
                        var seg_geom = seg.Geometry;
                        for (int localTurn = 0; localTurn < seg_geom.NumTurns; localTurn++, globalTurn++)
                        {
                            for (int localStrand = 0; localStrand < seg_geom.NumParallelConductors; localStrand++)
                            {
                                var locKey = new LocationKey(wdgNum, segNum, localTurn, localStrand);
                                var groupIns = new EntityGroup() { Name = $"Wdg{wdgNum}Turn{localTurn}Std{localStrand}Ins", Dimension = 2, AttributeIds = new List<int>() { tfmr.TagManager.GetTagByLocation(locKey, TagType.InsulationSurface) } };
                                var regionIns = new Region() { Name = $"Wdg{wdgNum}Turn{localTurn}Std{localStrand}Ins", EntityGroupName = groupIns.Name, Material = paper };
                                var groupCond = new EntityGroup() { Name = $"Wdg{wdgNum}Turn{localTurn}Std{localStrand}Cond", Dimension = 2, AttributeIds = new List<int>() { tfmr.TagManager.GetTagByLocation(locKey, TagType.ConductorBoundary) } };
                                var regionCond = new Region() { Name = $"Wdg{wdgNum}Turn{localTurn}Std{localStrand}Cond", EntityGroupName = groupCond.Name, Material = copper };
                                fem.EntityGroups.Add(groupIns);
                                fem.EntityGroups.Add(groupCond);
                                fem.Regions.Add(regionIns);
                                fem.Regions.Add(regionCond);
                                //if (globalTurn == excitedTurn && localStrand == excitedStrand)
                                //{
                                //    fem.Excitations.Add(new Excitation() { Region = regionCond, Value = 1.0 });
                                //}
                                //else
                                //{
                                //    fem.Excitations.Add(new Excitation() { Region = regionCond, Value = 0.0 });
                                //}
                            }
                        }
                    }
                }
            }

            fem.Solve();

            var resultFile = File.OpenText($"./Results/{excitedTurn}/out.txt");
            string? line = resultFile.ReadLine() ?? throw new Exception("Failed to read line from result file.");
            var L_array = Array.ConvertAll(line.Split().Skip(1).Where((value, index) => index % 2 == 1).ToArray(), double.Parse);

            var L = Vector_d.Build.Dense(L_array);
            resultFile.Close();

            return L;
        }

        public Matrix<double> Calc_Cmatrix_old(Transformer tfmr)
        {
            //GenerateMesh(tfmr);

            int total_conductors = 0;
            foreach (Winding wdg in tfmr.Windings)
            {
                total_conductors += wdg.NumConductors;
            }

            Matrix<double> C_getdp = Matrix<double>.Build.Dense(total_conductors, total_conductors);


            int globalTurn = -1;
            int globalConductor = -1;
            foreach (var wdg in tfmr.Windings)
            {
                foreach (var seg in wdg.Segments)
                {
                    if (seg.Geometry != null)
                    {
                        var seg_geom = seg.Geometry;
                        for (int localTurn = 0; localTurn < seg_geom.NumTurns; localTurn++)
                        {
                            globalTurn++;
                            for (int localStrand = 0; localStrand < seg_geom.NumParallelConductors; localStrand++)
                            {
                                globalConductor++;
                                var row = CalcCapacitance(tfmr, globalTurn, localStrand, order: 1);
                                Console.WriteLine($"C matrix row for turn {globalTurn} strand {localStrand} calculated.  Adding to row {globalConductor} of C matrix.");
                                Console.WriteLine($"C row: {string.Join(", ", row.ToArray())}");
                                // Take a lock to prevent two threads from writing to the matrix at the same time (just in case)
                                lock (C_getdp)
                                {
                                    C_getdp.SetRow(globalConductor, row);
                                }
                            }
                        }
                    }
                }
            }

            // globalConductor = 0;
            // foreach (var wdg in tfmr.Windings)
            // {
            //     foreach (var seg in wdg.Segments)
            //     {
            //         if (seg.Geometry != null)
            //         {
            //             var seg_geom = seg.Geometry;
            //             for (int localTurn = 0; localTurn < seg_geom.NumTurns; localTurn++, globalTurn++)
            //             {
            //                 for (int localStrand = 0; localStrand < seg_geom.NumParallelConductors; localStrand++)
            //                 {
            //                     globalConductor++;
            //                     (double r, double z) = wdg.GetTurnMidpoint(localTurn);
            //                     for (int t2 = 0; t2 < total_turns; t2++)
            //                     {
            //                         C_getdp[globalTurn, t2] = C_getdp[globalTurn, t2] / r;
            //                     }
            //                 }
            //             }
            //         }
            //     }
            // }


            DelimitedWriter.Write("C_getdp.csv", C_getdp, ",");
            return C_getdp;
        }

        private Vector_d CalcCapacitance(Transformer tfmr, int excitedTurn, int excitedStrand, int order = 1)
        {

            var fem = new GetDPAxiElecProblem();
            fem.Order = order;
            //fem.MeshPath = meshFile;
            fem.Filename = $"./Results/{excitedTurn}/case.pro";

            var oil = new Material("Oil")
            {
                Properties = new Dictionary<string, double> {
                { "eps_r", 1.0 } }
            };

            var paper = new Material("Paper")
            {
                Properties = new Dictionary<string, double> {
                { "eps_r", 2.0 } }
            };

            // var conductor = new Material("Conductor") // Dummy material for copper conductor area
            // {
            //     Properties = new Dictionary<string, double> {
            //     { "eps_r", 1.0 } }
            // };

            fem.Materials.Add(oil);
            fem.Materials.Add(paper);
            //fem.Materials.Add(conductor);
            fem.Regions.Add(new Region() { Name = "InteriorDomain", EntityGroupName = "InteriorDomain", Material = oil });
            if (tfmr.Core.CoreLegRadius_mm > 0)
            {
                fem.BoundaryConditions.Add(new DirichletBoundaryCondition() { Name = "CoreLeg", EntityGroupName = "CoreLeg", Potential = 0.0 });
            }
            else
            {
                fem.BoundaryConditions.Add(new NeumannBoundaryCondition() { Name = "Axis", EntityGroupName = "Axis", Flux = 0.0 });
            }
            fem.BoundaryConditions.Add(new DirichletBoundaryCondition() { Name = "Dirichlet", EntityGroupName = "Dirichlet", Potential = 0.0 });
            int globalTurn = 0;
            for (int wdgNum = 0; wdgNum < tfmr.Windings.Count; wdgNum++)
            {
                var wdg = tfmr.Windings[wdgNum];
                for (int segNum = 0; segNum < wdg.Segments.Count; segNum++)
                {
                    var seg = wdg.Segments[segNum];
                    if (seg.Geometry != null)
                    {
                        var seg_geom = seg.Geometry;
                        for (int localTurn = 0; localTurn < seg_geom.NumTurns; localTurn++, globalTurn++)
                        {
                            for (int localStrand = 0; localStrand < seg_geom.NumParallelConductors; localStrand++)
                            {
                                var locKey = new LocationKey(wdgNum, segNum, localTurn, localStrand);
                                var groupIns = new EntityGroup() { Name = $"Wdg{wdgNum}Turn{localTurn}Std{localStrand}Ins", Dimension = 2, AttributeIds = new List<int>() { tfmr.TagManager.GetTagByLocation(locKey, TagType.InsulationSurface) } };
                                var regionIns = new Region() { Name = $"Wdg{wdgNum}Turn{localTurn}Std{localStrand}Ins", EntityGroupName = groupIns.Name, Material = paper };
                                var groupCond = new EntityGroup() { Name = $"Wdg{wdgNum}Turn{localTurn}Std{localStrand}Cond", Dimension = 2, AttributeIds = new List<int>() { tfmr.TagManager.GetTagByLocation(locKey, TagType.ConductorBoundary) } };
                                var regionCond = new Region() { Name = $"Wdg{wdgNum}Turn{localTurn}Std{localStrand}Cond", EntityGroupName = groupCond.Name };
                                fem.EntityGroups.Add(groupIns);
                                fem.EntityGroups.Add(groupCond);
                                fem.Regions.Add(regionIns);
                                fem.Regions.Add(regionCond);

                                //if (globalTurn == excitedTurn && localStrand == excitedStrand)
                                //{
                                //    fem.Excitations.Add(new Excitation() { Region = regionCond, Value = 1.0 });
                                //}
                                //else
                                //{
                                //    fem.Excitations.Add(new Excitation() { Region = regionCond, Value = 0.0 });
                                //}
                            }
                        }
                    }
                }
            }

            fem.Solve();

            var resultFile = File.OpenText($"./Results/{excitedTurn}/q.txt");
            string? line = resultFile.ReadLine() ?? throw new Exception("Failed to read line from result file.");
            var C_array = Array.ConvertAll(line.Split().Skip(2).ToArray(), double.Parse);

            var C = Vector_d.Build.Dense(C_array);
            resultFile.Close();

            return C;
        }
    }

}