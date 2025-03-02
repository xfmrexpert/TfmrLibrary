using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TfmrLib
{
    public static class Conversions
    {
        public static double in_to_m(double x_in)
        {
            return x_in * 25.4 / 1000;
        }
    }
}
