namespace TfmrLib.FEM
{
    public class Material
    {
        public string Name { get; }
        // Scalar properties used in GetDP (add as needed)
        public Dictionary<string, double> Properties { get; set; } = new();

        public Material(string name) => Name = name;

        public Material Set(string prop, double value)
        {
            Properties[prop] = value;
            return this;
        }

        public bool TryGet(string prop, out double val) => Properties.TryGetValue(prop, out val);
    }
}