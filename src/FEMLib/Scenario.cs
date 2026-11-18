using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FEMLib
{
    public class Scenario
    {
        public string Name { get; init; }
        public List<Excitation> Excitations { get; set; }
        public FrequencySpec Frequency { get; set; }
    }
}
