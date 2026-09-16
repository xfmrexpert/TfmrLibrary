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
using System.Numerics;
using System.ComponentModel.DataAnnotations;

namespace TfmrLib
{
    public class FEMMatrixCalculator : IRLCMatrixCalculator
    {
        private Mesh mesh;
        private string meshFile;

        /// <summary>
        /// Raised for each progress event reported by the FEM solver, so hosts (CLI/UI) can
        /// render them without the solver writing directly to the console.
        /// </summary>
        public event Action<MFEMProgressEvent>? ProgressChanged;

        private List<(double, Matrix<double>)> R_matrices = new(); 

        private void GenerateMesh(Transformer tfmr, int meshorder = 2)
        {
            var meshGen = new MeshGenerator();
            var geometry = tfmr.GenerateGeometry();
            double meshscale = 10.0;
            meshGen.AddGeometry(geometry);
            var geoFile = "case.geo";
            meshFile = "case.msh";
            mesh = meshGen.GenerateMesh(geoFile, meshscale, meshorder);
        }

        // WARNING: The following two methods are thrown in for completeness, but DO NOT use if you 
        // want a range of frequencies. Using the FrequencySpec to specify the range will
        // yield much better performance.
        public Matrix<double> Calc_Lmatrix(Transformer tfmr, double freq)
        {
            return Calc_Lmatrix(tfmr, new FrequencySpec.Scalar(freq)).First().Item2;
        }

        public Matrix<double> Calc_Rmatrix(Transformer tfmr, double freq)
        {
            return Calc_Rmatrix(tfmr, new FrequencySpec.Scalar(freq)).First().Item2;
        }

        public List<(double, Matrix<double>)> Calc_Lmatrix(Transformer tfmr, FrequencySpec freq)
        {
            int order = 2;
            GenerateMesh(tfmr, order);
            var fem = new MFEMProblem();
            fem.ProgressChanged += e => ProgressChanged?.Invoke(e);
            fem.AnalysisType = AnalysisType.CouplingMatrix;
            fem.PhysicsType = PhysicsType.Magnetoquasistatics;
            fem.GeometryType = GeometryType.Axisymmetric;
            fem.MeshPath = meshFile;
            fem.Filename = $"./Lmatrix.json";
            fem.ResultsFile = $"Lmatrix.h5";

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
                                var groupIns = new EntityGroup() { Name = $"Wdg{wdgNum}Seg{segNum}Turn{localTurn}Std{localStrand}Ins", Dimension = 2, AttributeIds = new List<int>() { tfmr.TagManager.GetTagByLocation(locKey, TagType.InsulationSurface) } };
                                var regionIns = new Region() { Name = $"Wdg{wdgNum}Seg{segNum}Turn{localTurn}Std{localStrand}Ins", EntityGroupName = groupIns.Name, Material = paper };
                                var groupCond = new EntityGroup() { Name = $"Wdg{wdgNum}Seg{segNum}Turn{localTurn}Std{localStrand}Cond", Dimension = 2, AttributeIds = new List<int>() { tfmr.TagManager.GetTagByLocation(locKey, TagType.ConductorSurface) } };
                                var regionCond = new Region() { Name = $"Wdg{wdgNum}Seg{segNum}Turn{localTurn}Std{localStrand}Cond", EntityGroupName = groupCond.Name, Material = copper };
                                fem.EntityGroups.Add(groupIns);
                                fem.EntityGroups.Add(groupCond);
                                fem.Regions.Add(regionIns);
                                fem.Regions.Add(regionCond);
                                fem.Terminals.Add(new FEM.Terminal() { Name = $"Wdg{wdgNum}Seg{segNum}Turn{localTurn}Std{localStrand}Cond", EntityGroup = groupCond, ExcitationType = Quantity.Current });
                            }
                        }
                    }
                }
            }
            fem.Scenarios.Add(new Scenario { Name = "LMatrix", Frequency = freq });

            fem.Solve();

            var L_matrices = new List<(double, Matrix<double>)>();

            var results = fem.Results;

            // Read the L matrices from the output file and return them as a list of tuples (frequency, L matrix)
            foreach (var sample in results.Coupling.Samples)
            {
                L_matrices.Add((sample.FrequencyHz, sample.InductanceMatrix));
            }

            R_matrices = new List<(double, Matrix<double>)>();

            // Read the R matrices from the output file
            foreach (var sample in results.Coupling.Samples)
            {
                R_matrices.Add((sample.FrequencyHz, sample.ResistanceMatrix));
            }
            
            return L_matrices;
        }

        public Matrix<double> Calc_Cmatrix(Transformer tfmr)
        {
            int order = 2;
            GenerateMesh(tfmr, order);
            var fem = new MFEMProblem();
            fem.ProgressChanged += e => ProgressChanged?.Invoke(e);
            fem.AnalysisType = AnalysisType.CouplingMatrix;
            fem.PhysicsType = PhysicsType.Electrostatics;
            fem.GeometryType = GeometryType.Axisymmetric;
            fem.MeshPath = meshFile;
            fem.Filename = $"./Cmatrix.json";
            fem.ResultsFile = $"CMatrix.h5";

            var oil = new Material("Oil")
            {
                Properties = new Dictionary<string, double> {
                { "epsilon_r", 1.0 } }
            };

            var paper = new Material("Paper")
            {
                Properties = new Dictionary<string, double> {
                { "epsilon_r", 2.2 } }
            };

            fem.Materials.Add(oil);
            fem.Materials.Add(paper);
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
                                var groupIns = new EntityGroup() { Name = $"Wdg{wdgNum}Seg{segNum}Turn{localTurn}Std{localStrand}Ins", Dimension = 2, AttributeIds = new List<int>() { tfmr.TagManager.GetTagByLocation(locKey, TagType.InsulationSurface) } };
                                var regionIns = new Region() { Name = $"Wdg{wdgNum}Seg{segNum}Turn{localTurn}Std{localStrand}Ins", EntityGroupName = groupIns.Name, Material = paper };
                                var groupCond = new EntityGroup() { Name = $"Wdg{wdgNum}Seg{segNum}Turn{localTurn}Std{localStrand}Cond", Dimension = 1, AttributeIds = new List<int>() { tfmr.TagManager.GetTagByLocation(locKey, TagType.ConductorSurface) } };
                                //var regionCond = new Region() { Name = $"Wdg{wdgNum}Seg{segNum}Turn{localTurn}Std{localStrand}Cond", EntityGroupName = groupCond.Name, Material = copper };
                                fem.EntityGroups.Add(groupIns);
                                fem.EntityGroups.Add(groupCond);
                                fem.Regions.Add(regionIns);
                                //fem.Regions.Add(regionCond);
                                fem.Terminals.Add(new FEM.Terminal() { Name = $"Wdg{wdgNum}Seg{segNum}Turn{localTurn}Std{localStrand}Cond", EntityGroup = groupCond, ExcitationType = Quantity.Voltage });
                            }
                        }
                    }
                }
            }
            fem.Scenarios.Add(new Scenario { Name = "CMatrix" });

            fem.Solve();

            var C_matrix = fem.Results.Coupling.CapacitanceMatrix;

            return C_matrix;
        }

        public List<(double, Matrix<double>)> Calc_Rmatrix(Transformer tfmr, FEM.FrequencySpec freq)
        {
            if (R_matrices.Count == 0)
            {
                Calc_Lmatrix(tfmr, freq);
            }
            return R_matrices;
        }
    }
}
