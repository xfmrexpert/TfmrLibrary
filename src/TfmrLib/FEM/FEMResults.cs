using System.Numerics;
using MathNet.Numerics.LinearAlgebra;
using MeshLib;

namespace TfmrLib.FEM
{
    public class FEMResults
    {
        public PhysicsType PhysicsType {get; set;}
        public GeometryType GeometryType {get; set;}
        public AnalysisType AnalysisType {get; set;}

        public Mesh Mesh {get; set;}

        public List<ScenarioField> ScenarioFields {get; set;} = new();

        public CouplingResults? Coupling { get; init; }
    }

    public class CouplingResults
    {
        public IReadOnlyList<string> TerminalNames { get; init; } = [];

        public Matrix<double>? CapacitanceMatrix { get; init; }
        public Matrix<double>? StaticInductanceMatrix { get; init; }

        public IReadOnlyList<MQSCouplingSample> Samples { get; init; } = [];
    }

    public sealed record MQSCouplingSample(
        double FrequencyHz,
        Matrix<double> InductanceMatrix,
        Matrix<double> ResistanceMatrix);

    public class ScenarioField
    {
        public string ScenarioId {get; set;}
        public string ScenarioName {get; set;}
        public double? FrequencyHz {get; set;}

    }

}