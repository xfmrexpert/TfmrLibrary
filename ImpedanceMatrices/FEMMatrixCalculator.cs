using GeometryLib;
using MathNet.Numerics.Data.Text;
using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TfmrLib.FEM;
using MeshLib;
using LinAlg = MathNet.Numerics.LinearAlgebra;
using Vector_d = MathNet.Numerics.LinearAlgebra.Vector<double>;

namespace TfmrLib
{
    public class FEMMatrixCalculator : IRLCMatrixCalculator
    {
        private Mesh mesh;

        private void GenerateMesh(Transformer tfmr)
        {
            var geometry = tfmr.GenerateGeometry();
            //GmshFile gmshFile = new GmshFile("case.geo");
            ///gmshFile.CreateFromGeometry(geometry);
            double meshscale = 1.0;
            //mesh = gmshFile.GenerateMesh(meshscale, 2);
        }

        public Matrix<double> Calc_Lmatrix(Transformer tfmr, double freq)
        {
            int order = 2; // Order of the finite element method

            GenerateMesh(tfmr);

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

            var oil = new Material("Oil")
            {
                Properties = new Dictionary<string, double> {
                { "mu_r", 1.0 },
                { "epsr", tfmr.eps_oil },
                { "loss_tan", tfmr.ins_loss_factor } }
            };

            var paper = new Material("Paper")
            {
                Properties = new Dictionary<string, double> {
                { "mu_r", 1.0 },
                { "epsr", tfmr.Windings[0].eps_paper },
                { "loss_tan", tfmr.ins_loss_factor } }
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
            fem.Regions.Add(new Region() { Name = "InteriorDomain", Tags = new List<int>() { tfmr.TagManager.GetTagByString("InteriorDomain") }, Material = oil });
            fem.BoundaryConditions.Add(new BoundaryCondition() { Name = "Dirichlet", Tags = new List<int>() { tfmr.TagManager.GetTagByString("CoreLeg"), tfmr.TagManager.GetTagByString("TopYoke"), tfmr.TagManager.GetTagByString("BottomYoke"), tfmr.TagManager.GetTagByString("RightEdge") } });
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
                                var regionIns = new Region() { Name = $"Wdg{wdgNum}Turn{localTurn}Std{localStrand}Ins", Tags = new List<int>() { tfmr.TagManager.GetTagByLocation(locKey, TagType.InsulationSurface) }, Material = paper };
                                var regionCond = new Region() { Name = $"Wdg{wdgNum}Turn{localTurn}Std{localStrand}Cond", Tags = new List<int>() { tfmr.TagManager.GetTagByLocation(locKey, TagType.ConductorSurface) }, Material = copper };
                                fem.Regions.Add(regionIns);
                                fem.Regions.Add(regionCond);
                                if (globalTurn == excitedTurn && localStrand == excitedStrand)
                                {
                                    fem.Excitations.Add(new Excitation() { Region = regionCond, Value = 1.0 });
                                }
                                else
                                {
                                    fem.Excitations.Add(new Excitation() { Region = regionCond, Value = 0.0 });
                                }
                            }
                        }
                    }
                }
            }

            fem.Solve();

            var resultFile = File.OpenText("out.txt");
            string? line = resultFile.ReadLine() ?? throw new Exception("Failed to read line from result file.");
            var L_array = Array.ConvertAll(line.Split().Skip(1).Where((value, index) => index % 2 == 1).ToArray(), double.Parse);

            var L = Vector_d.Build.Dense(L_array);
            resultFile.Close();

            return L;

            // Use the older method with getdp executable directly for now
            //return CalcInductanceOld(tfmr, excitedTurn, freq, order);
            return null;
        }

        // private Vector_d CalcInductanceOld(Transformer tfmr, int posTurn, double freq, int order = 1)
        // {
        //     Console.WriteLine($"Frequency: {freq.ToString("0.##E0")} Turn: {posTurn}");

        //     string model_prefix = $"./Results/{posTurn}/";
        //     WriteGetDPInductanceFile(tfmr, posTurn, freq, order, model_prefix);

        //     string mygetdp = onelab_dir + "getdp.exe";

        //     string model = model_prefix + "case";
        //     string model_msh = "case.msh";
        //     string model_pro = model + ".pro";

        //     int return_code = -999;
        //     object returnCodeLock = new object();

        //     while (return_code < 0)
        //     {
        //         var sb = new StringBuilder();
        //         Process p = new Process();

        //         p.StartInfo.FileName = mygetdp;
        //         p.StartInfo.Arguments = model_pro + " -msh " + model_msh + $" -setstring modelPath {model_prefix} -solve Magnetodynamics2D_av -pos dyn -v 5";
        //         p.StartInfo.CreateNoWindow = true;

        //         // redirect the output
        //         p.StartInfo.RedirectStandardOutput = true;
        //         p.StartInfo.RedirectStandardError = true;

        //         // hookup the eventhandlers to capture the data that is received
        //         p.OutputDataReceived += (sender, args) => sb.AppendLine(args.Data);
        //         p.ErrorDataReceived += (sender, args) => sb.AppendLine(args.Data);

        //         // direct start
        //         p.StartInfo.UseShellExecute = false;

        //         // Start process watchdog timer
        //         var timer = new System.Timers.Timer(60000); // 60 seconds
        //         timer.Elapsed += (sender, e) =>
        //         {
        //             if (!p.HasExited)
        //             {
        //                 p.Kill();
        //                 Console.WriteLine("Process killed due to timeout.");
        //                 timer.Stop();
        //                 lock (returnCodeLock)
        //                 {
        //                     return_code = -1; // Set return code to indicate timeout
        //                 }
        //                 // Try again
        //             }
        //         };

        //         p.Start();

        //         timer.Start();

        //         // start our event pumps
        //         p.BeginOutputReadLine();
        //         p.BeginErrorReadLine();

        //         // until we are done
        //         p.WaitForExit();

        //         string output = sb.ToString();
        //         return_code = p.ExitCode;
        //         if (return_code > 0)
        //         {
        //             Console.Write(output);
        //         }
        //         timer.Stop(); // Stop the timer if process exits normally
        //         timer.Dispose();
        //         p.Close();

        //     }

        //     if (return_code != 0)
        //     {
        //         throw new Exception($"Failed to run getdp in CalcInductance for turn {posTurn}");
        //     }

        //     var resultFile = File.OpenText(model_prefix + "out.txt");
        //     string? line = resultFile.ReadLine() ?? throw new Exception("Failed to read line from result file.");
        //     var L_array = Array.ConvertAll(line.Split().Skip(1).Where((value, index) => index % 2 == 1).ToArray(), double.Parse);

        //     var L = Vector_d.Build.Dense(L_array);
        //     resultFile.Close();

        //     return L;
        // }

        // private void WriteGetDPInductanceFile(Transformer tfmr, int turnNum, double freq, int order, string model_prefix)
        // {
        //     int total_turns = 0;
        //     foreach (Winding wdg in tfmr.Windings)
        //     {
        //         total_turns += wdg.num_turns;
        //     }

        //     // Find the winding and turn number for the given global turn number
        //     int wdgNum = 0;
        //     int localTurn = turnNum;
        //     for (int i = 0; i < tfmr.Windings.Count; i++)
        //     {
        //         var wdg = tfmr.Windings[i];
        //         if (localTurn < wdg.num_turns)
        //         {
        //             wdgNum = i;
        //             break;
        //         }
        //         localTurn -= wdg.num_turns;
        //     }

        //     Directory.CreateDirectory(Directory.GetCurrentDirectory() + model_prefix);

        //     var f = File.CreateText(model_prefix + "case.pro");

        //     f.WriteLine($"FE_Order = {order};");

        //     f.WriteLine("Group{");

        //     bool firstTurn;

        //     f.Write($"Air = Region[{{{tfmr.phyAir}, ");
        //     firstTurn = true;
        //     for (int windingIndex = 0; windingIndex < tfmr.Windings.Count; windingIndex++)
        //     {
        //         var wdg = tfmr.Windings[windingIndex];
        //         for (int turnIndex = 0; turnIndex < wdg.num_turns; turnIndex++)
        //         {
        //             if (!firstTurn)
        //             {
        //                 f.Write(", ");
        //             }
        //             else
        //             {
        //                 firstTurn = false;
        //             }
        //             f.Write($"{wdg.phyTurnsIns[turnIndex]}");
        //         }
        //     }
        //     f.WriteLine("}];");

        //     for (int windingIndex = 0; windingIndex < tfmr.Windings.Count; windingIndex++)
        //     {
        //         var wdg = tfmr.Windings[windingIndex];
        //         for (int turnIndex = 0; turnIndex < wdg.num_turns; turnIndex++)
        //         {
        //             f.WriteLine($"Turn{turnIndex} = Region[{wdg.phyTurnsCond[turnIndex]}];");
        //         }
        //     }

        //     f.WriteLine($"TurnPos = Region[{tfmr.Windings[wdgNum].phyTurnsCond[turnNum]}];");
        //     f.Write("TurnZero = Region[{");
        //     firstTurn = true;
        //     int globalTurnIndex = 0;
        //     for (int windingIndex = 0; windingIndex < tfmr.Windings.Count; windingIndex++)
        //     {
        //         var wdg = tfmr.Windings[windingIndex];
        //         for (int turnIndex = 0; turnIndex < wdg.num_turns; turnIndex++, globalTurnIndex++)
        //         {
        //             if (globalTurnIndex != turnNum)
        //             {
        //                 if (!firstTurn)
        //                 {
        //                     f.Write(", ");
        //                 }
        //                 else
        //                 {
        //                     firstTurn = false;
        //                 }
        //                 f.Write($"{wdg.phyTurnsCond[turnIndex]}");
        //             }
        //         }
        //     }
        //     f.WriteLine("}];");

        //     f.WriteLine($"Axis = Region[{tfmr.phyAxis}];");
        //     f.WriteLine($"Surface_Inf = Region[{tfmr.phyInf}];");

        //     f.Write("Turns = Region[{");
        //     for (int i = 0; i < total_turns; i++)
        //     {
        //         f.Write($"Turn{i}");
        //         if (i < (total_turns - 1))
        //         {
        //             f.Write(", ");
        //         }
        //         else
        //         {
        //             f.Write("}];\n");
        //         }
        //     }
        //     f.WriteLine("Vol_Mag += Region[{Air, Turns, Surface_Inf}];");
        //     f.WriteLine("Vol_C_Mag = Region[{Turns}];");
        //     f.WriteLine("}");
        //     f.WriteLine($"Freq={freq};");
        //     f.WriteLine("Include \"../../GetDP_Files/L_s_inf.pro\";");
        //     f.Close();
        // }

        public Matrix<double> Calc_Cmatrix(Transformer tfmr)
        {
            GenerateMesh(tfmr);

            int total_conductors = 0;
            foreach (Winding wdg in tfmr.Windings)
            {
                total_conductors += wdg.NumConductors;
            }

            Matrix<double> C_getdp = Matrix<double>.Build.Dense(total_conductors, total_conductors);

            
            int globalTurn = 0;
            int globalConductor = 0;
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
                                var row = CalcCapacitance(tfmr, globalTurn, order: 1);

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

        private Vector_d CalcCapacitance(Transformer tfmr, int turn, int order = 1)
        {
            
            return null;
        }

        // private Vector_d CalcCapacitance(Transformer tfmr, int posTurn, int order = 1)
        // {
        //     string dir = posTurn.ToString();

        //     int total_turns = 0;
        //     foreach (Winding wdg in tfmr.Windings)
        //     {
        //         total_turns += wdg.num_turns;
        //     }

        //     // Find the winding and turn number for the given global turn number
        //     int wdgNum = 0;
        //     int localTurn = posTurn;
        //     for (int i = 0; i < tfmr.Windings.Count; i++)
        //     {
        //         var wdg = tfmr.Windings[i];
        //         if (localTurn < wdg.num_turns)
        //         {
        //             wdgNum = i;
        //             break;
        //         }
        //         localTurn -= wdg.num_turns;
        //     }

        //     string model_prefix = $"./Results/{dir}/";
        //     Directory.CreateDirectory(Directory.GetCurrentDirectory() + $"/Results/{dir}");

        //     var f = File.CreateText($"Results/{dir}/case.pro");
        //     f.WriteLine($"FE_Order = {order};");
        //     f.WriteLine("Group{");
        //     f.WriteLine($"Air = Region[{tfmr.phyAir}];");
        //     int globalTurnIndex = 0;
        //     for (int windingIndex = 0; windingIndex < tfmr.Windings.Count; windingIndex++)
        //     {
        //         var wdg = tfmr.Windings[windingIndex];
        //         for (int turnIndex = 0; turnIndex < wdg.num_turns; turnIndex++)
        //         {
        //             f.WriteLine($"Turn{globalTurnIndex} = Region[{wdg.phyTurnsCondBdry[turnIndex]}];");
        //             globalTurnIndex++;
        //         }
        //     }

        //     f.Write("TurnIns = Region[{");
        //     bool firstTurn = true;
        //     for (int windingIndex = 0; windingIndex < tfmr.Windings.Count; windingIndex++)
        //     {
        //         var wdg = tfmr.Windings[windingIndex];
        //         for (int turnIndex = 0; turnIndex < wdg.num_turns; turnIndex++)
        //         {
        //             if (!firstTurn)
        //             {
        //                 f.Write(", ");
        //             }
        //             else
        //             {
        //                 firstTurn = false;
        //             }
        //             f.Write($"{wdg.phyTurnsIns[turnIndex]}");
        //         }
        //     }

        //     f.Write("}];\n");

        //     //f.WriteLine($"Ground = Region[{tfmr.phyCore}];")
        //     f.WriteLine($"Axis = Region[{tfmr.phyAxis}];");
        //     f.WriteLine($"Surface_Inf = Region[{tfmr.phyInf}];");
        //     f.WriteLine("Vol_Ele = Region[{Air, TurnIns}];");
        //     f.Write("Sur_C_Ele = Region[{");

        //     for (int i = 0; i < total_turns; i++)
        //     {
        //         f.Write($"Turn{i}");
        //         if (i < (total_turns - 1))
        //         {
        //             f.Write(", ");
        //         }
        //         else
        //         {
        //             f.Write("}];\n");
        //         }
        //     }
        //     if (tfmr.r_core == 0)
        //     {
        //         f.WriteLine($"Sur_Neu_Ele = Region[{tfmr.phyAxis}];");
        //     }
        //     f.WriteLine("}");

        //     //TODO: Fix for case where posTurn is last turn
        //     firstTurn = true;
        //     string otherTurns = "";
        //     globalTurnIndex = 0;
        //     for (int windingIndex = 0; windingIndex < tfmr.Windings.Count; windingIndex++)
        //     {
        //         var wdg = tfmr.Windings[windingIndex];
        //         for (int turnIndex = 0; turnIndex < wdg.num_turns; turnIndex++)
        //         {
        //             if (globalTurnIndex != posTurn)
        //             {
        //                 if (!firstTurn)
        //                 {
        //                     otherTurns += ", ";
        //                 }
        //                 else
        //                 {
        //                     firstTurn = false;
        //                 }
        //                 otherTurns += $"Turn{globalTurnIndex}";
        //             }
        //             globalTurnIndex++;
        //         }
        //     }

        //     f.WriteLine($@"
        //     Flag_Axi = 1;

        //     Include ""../../GetDP_Files/Lib_Materials.pro"";

        //     Function {{
        //         {(tfmr.r_core == 0 ? "dn[Region[Axis]] = 0;" : "")} 
        //         epsr[Region[{{Air}}]] = {tfmr.eps_oil};
        //         epsr[Region[{{TurnIns}}]] = {tfmr.Windings[0].eps_paper};
        //     }}

        //     Constraint {{
        //         {{ Name ElectricScalarPotential; Type Assign;
        //             Case {{
        //                 {{ Region Region[Surface_Inf]; Value 0; }}
        //                 {(tfmr.r_core > 0 ? "{ Region Region[Axis]; Value 0;}" : "")}
        //             }}
        //         }}
        //     }}
        //     Constraint {{                             
        //         {{ Name GlobalElectricPotential; Type Assign;
        //             Case {{
        //                 {{ Region Region[Turn{posTurn}]; Value 1.0; }}
        //                 {{ Region Region[{{{otherTurns}}}]; Value 0; }}
        //             }}
        //         }}
        //     }}
        //     Constraint {{ {{ Name GlobalElectricCharge; Case {{ }} }} }}

        //     Include ""../../GetDP_Files/Lib_Electrostatics_v.pro"";
        //     ");

        //     f.Close();

        //     string mygetdp = onelab_dir + "getdp.exe";

        //     string model = model_prefix + "case";
        //     string model_msh = "case.msh";
        //     string model_pro = model + ".pro";

        //     var sb = new StringBuilder();
        //     Process p = new Process();

        //     p.StartInfo.FileName = mygetdp;
        //     p.StartInfo.Arguments = model_pro + " -msh " + model_msh + $" -setstring modelPath Results/{dir} -solve Electrostatics_v -pos Electrostatics_v -v 5";
        //     p.StartInfo.CreateNoWindow = true;

        //     // redirect the output
        //     p.StartInfo.RedirectStandardOutput = true;
        //     p.StartInfo.RedirectStandardError = true;

        //     // hookup the eventhandlers to capture the data that is received
        //     p.OutputDataReceived += (sender, args) => sb.AppendLine(args.Data);
        //     p.ErrorDataReceived += (sender, args) => sb.AppendLine(args.Data);

        //     // direct start
        //     p.StartInfo.UseShellExecute = false;

        //     p.Start();

        //     // start our event pumps
        //     p.BeginOutputReadLine();
        //     p.BeginErrorReadLine();

        //     // until we are done
        //     p.WaitForExit();

        //     string output = sb.ToString();

        //     int return_code = p.ExitCode;
        //     if (return_code != 0)
        //     {
        //         throw new Exception($"Failed to run getdp in CalcCapacitance for turn {posTurn}");
        //     }

        //     var resultFile = File.OpenText(model_prefix + "res/q.txt");
        //     string line = resultFile.ReadLine();
        //     var C_array = Array.ConvertAll(line.Split().Skip(2).ToArray(), Double.Parse);
        //     var C = MathNet.Numerics.LinearAlgebra.Vector<double>.Build.Dense(C_array);
        //     resultFile.Close();
        //     return C;
        // }

        public Matrix<double> Calc_Rmatrix(Transformer tfmr, double f = 60)
        {
            throw new NotImplementedException();
        }
    }
}
