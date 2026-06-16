using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public enum GeometryType
    {
        Planar = 0,
        Axisymmetric = 1
    }

    public enum AnalysisType
    {
        Field = 0,
        CouplingMatrix = 1
    }

    public class FEMProblem
    {
        public GeometryType GeometryType { get; set; } = GeometryType.Planar;
        public AnalysisType AnalysisType { get; set; } = AnalysisType.Field;
        
        public string MeshPath { get; set; }
        public NamedCollection<EntityGroup> EntityGroups { get; set; } = new NamedCollection<EntityGroup>();
        public List<Material> Materials { get; set; } = new List<Material>();
        public List<Region> Regions { get; set; } = new List<Region>();
        public List<Terminal> Terminals { get; set; } = new List<Terminal>(); 
        public List<BoundaryCondition> BoundaryConditions { get; set; } = new List<BoundaryCondition>();
        public List<Scenario> Scenarios { get; set; } = new List<Scenario>();

        /// <summary>
        /// Populated by <see cref="Solve"/> in derived classes once a solution has been
        /// computed and (optionally) loaded back from the solver's output.
        /// </summary>
        public FEMSolution? Solution { get; protected set; }

        public virtual void Solve()
        {
            throw new NotImplementedException("Solve method not implemented for base FEMProblem class");
        }
    }
}
