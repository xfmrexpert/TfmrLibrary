using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MeshLib;

namespace TfmrLib.FEM
{
    /// <summary>
    /// Results loaded from an FEM solve (e.g. MFEM-ElectroMag). Field data are keyed
    /// by Gmsh node/element IDs (1-based, as they appear in the .msh file) so they
    /// can be matched directly against <see cref="MeshLib.GmshFile"/> output.
    /// </summary>
    public class FEMSolution
    {
        /// <summary>
        /// Path of the .msh file the results were loaded from.
        /// </summary>
        public string? ResultsFile { get; init; }

        /// <summary>
        /// Mesh (typically the refined export mesh produced by the solver).
        /// </summary>
        public GmshFile? Mesh { get; init; }

        /// <summary>
        /// Continuous nodal scalar fields (e.g. "V"), keyed by view name in StringTags[0].
        /// </summary>
        public Dictionary<string, Dictionary<int, double>> NodalScalars { get; init; }
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Per-element nodal fields (e.g. "E", "|E|"), preserving discontinuities across
        /// material interfaces. Indexed as [viewName][elementId] = component-major array
        /// of length numComponents * numNodes.
        /// </summary>
        public Dictionary<string, Dictionary<int, ElementNodeField>> ElementNodalFields { get; init; }
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Per-element scalar/vector fields, indexed as [viewName][elementId] = values.
        /// </summary>
        public Dictionary<string, Dictionary<int, double[]>> ElementFields { get; init; }
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Physical (material / boundary) tag → human-readable name. Populated from the
        /// .msh file's $PhysicalNames section when present, and may be augmented by
        /// callers using the regions / BC names known at build time.
        /// </summary>
        public Dictionary<int, string> PhysicalNames { get; init; } = new();

        public static FEMSolution Load(string mshResultsPath)
        {
            if (!File.Exists(mshResultsPath))
                throw new FileNotFoundException("FEM results file not found.", mshResultsPath);

            var gmsh = GmshFile.Parse(mshResultsPath);

            var nodal = new Dictionary<string, Dictionary<int, double>>(StringComparer.OrdinalIgnoreCase);
            foreach (var view in gmsh.NodeData)
            {
                var name = view.StringTags.FirstOrDefault() ?? $"view{nodal.Count}";
                var dict = new Dictionary<int, double>(view.Data.Count);
                foreach (var (nodeId, value) in view.Data)
                    dict[nodeId] = value;
                nodal[name] = dict;
            }

            var elemNodal = new Dictionary<string, Dictionary<int, ElementNodeField>>(StringComparer.OrdinalIgnoreCase);
            foreach (var view in gmsh.ElementNodeData)
            {
                var name = view.StringTags.FirstOrDefault() ?? $"view{elemNodal.Count}";
                int numComponents = view.IntegerTags.Count > 1 ? view.IntegerTags[1] : 1;
                var dict = new Dictionary<int, ElementNodeField>(view.Data.Count);
                foreach (var (elementId, numNodes, values) in view.Data)
                    dict[elementId] = new ElementNodeField(numComponents, numNodes, values);
                elemNodal[name] = dict;
            }

            var elem = new Dictionary<string, Dictionary<int, double[]>>(StringComparer.OrdinalIgnoreCase);
            foreach (var view in gmsh.ElementData)
            {
                var name = view.StringTags.FirstOrDefault() ?? $"view{elem.Count}";
                var dict = new Dictionary<int, double[]>(view.Data.Count);
                foreach (var (elementId, values) in view.Data)
                    dict[elementId] = values;
                elem[name] = dict;
            }

            return new FEMSolution
            {
                ResultsFile = mshResultsPath,
                Mesh = gmsh,
                NodalScalars = nodal,
                ElementNodalFields = elemNodal,
                ElementFields = elem,
                PhysicalNames = new Dictionary<int, string>(gmsh.PhysicalNames),
            };
        }

        public bool TryGetPotential(out Dictionary<int, double> potential)
        {
            foreach (var key in new[] { "V", "Phi", "Potential" })
            {
                if (NodalScalars.TryGetValue(key, out var v))
                {
                    potential = v;
                    return true;
                }
            }
            potential = default!;
            return false;
        }
    }

    /// <summary>
    /// One element's worth of per-node field samples. <see cref="Values"/> is component-major:
    /// for node i and component c, the value is Values[i * NumComponents + c].
    /// </summary>
    public readonly record struct ElementNodeField(int NumComponents, int NumNodes, double[] Values)
    {
        public double Get(int localNode, int component) => Values[localNode * NumComponents + component];
    }
}
