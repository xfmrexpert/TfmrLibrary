using System.Numerics;
using PureHDF;
using MathNet.Numerics.LinearAlgebra;
using System.Runtime.CompilerServices;

namespace TfmrLib.FEM
{
    public class MFEMResultsReader
    {
        public static FEMResults Read(string fileName)
        {
            using var file = H5File.OpenRead(fileName);

            string physics_type = file.Attribute("physics_type").Read<string>();

            if (!Enum.TryParse<PhysicsType>(physics_type, ignoreCase: true, out var physicsType)
                || !Enum.IsDefined(physicsType))
            {
                throw new InvalidDataException($"Unsupported physics_type: '{physics_type}'.");
            }

            string analysis_type = file.Attribute("analysis_type").Read<string>();

            var analysisType = analysis_type switch
            {
                "field" => AnalysisType.Field,
                "coupling_matrix" => AnalysisType.CouplingMatrix,
                _ => throw new InvalidDataException(
                    $"Unsupported analysis_type: '{analysis_type}'.")
            };

            string geometry_type = file.Attribute("geometry_type").Read<string>();

            if (!Enum.TryParse<GeometryType>(geometry_type, ignoreCase: true, out var geometryType)
                || !Enum.IsDefined(geometryType))
            {
                throw new InvalidDataException($"Unsupported geometry_type: '{geometry_type}'.");
            }

            var coupling = file.Group("coupling");

            if (analysisType == AnalysisType.CouplingMatrix)
            {
                var frequencies = coupling.Dataset("frequency_hz").Read<double[]>();
                var terminalNames = coupling.Dataset("terminal_names").Read<string[]>();

                var inductanceDataset = coupling.Dataset("Inductance/values");
                var inductanceValues = inductanceDataset.Read<double[]>();

                var resistanceDataset = coupling.Dataset("Resistance/values");
                var resistanceValues = resistanceDataset.Read<double[]>();

                int n = terminalNames.Length;

                if (physicsType == PhysicsType.Magnetoquasistatics)
                {
                    var samples = new List<MQSCouplingSample>();
                    for (int i = 0; i < frequencies.Length; ++i)
                    {
                        var couplingSample = new MQSCouplingSample(FrequencyHz: frequencies[i],
                                                                    InductanceMatrix: Matrix<double>.Build.Dense(n, n),
                                                                    ResistanceMatrix: Matrix<double>.Build.Dense(n, n));
                        for (int r = 0; r < n; ++r)
                        {
                            for (int c = 0; c < n; ++c)
                            {
                                int offset = (i * n + r) * n + c;
                                couplingSample.InductanceMatrix[r, c] = inductanceValues[offset];
                                couplingSample.ResistanceMatrix[r, c] = resistanceValues[offset];
                            }
                        }
                        samples.Add(couplingSample);
                        //matrices.Add(frequencies[i], (inductance, resistance));
                    }
                    var couplingResults = new CouplingResults { TerminalNames = terminalNames.ToList(), Samples = samples.AsReadOnly() };
                    return new FEMResults { PhysicsType = physicsType, AnalysisType = analysisType, GeometryType = geometryType, Coupling = couplingResults };
                }
                else if (physicsType == PhysicsType.Electrostatics)
                {
                    // Read capacitance matrix
                }
                else
                {
                    // Must be magnetostatic, so read static InductanceMatrix

                }
            }

            return null;
        }
    }
}