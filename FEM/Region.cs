using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib.FEM
{
    public class Region
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public List<int> Tags { get; set; }
        public List<object> Properties { get; set; }
    }
}
