using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LinAlg = MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    public class AnalyticMatrixCalculator : IRLCMatrixCalculator
    {
        //PUL Inductances
        private Matrix<double> Calc_Lmatrix_Wdg(Winding wdg, double f = 60)
        {
            var M = Matrix<double>.Build;

            Matrix<double> L = M.Dense(wdg.NumConductors, wdg.NumConductors);

            foreach (var segment in wdg.Segments)
            {
                if (segment.Geometry == null)
                {
                    continue; // Skip segments without geometry
                }

                var segmentGeometry = segment.Geometry;

                int idx = 0;
                for (int turn = 0; turn < segmentGeometry.NumTurns; turn++)
                {
                    for (int strand = 0; strand < segmentGeometry.NumParallelConductors; strand++)
                    {
                        (double r_i, double z_i) = segmentGeometry.GetConductorPosition(turn, strand);
                        L[idx, idx] = CalcSelfInductance(segmentGeometry.ConductorType.BareHeight_mm, segmentGeometry.ConductorType.BareWidth_mm, r_i, wdg.rho_c, f) / (2 * Math.PI * r_i);
                        for (int j = idx + 1; j < wdg.NumTurns; j++)
                        {
                            for (int s2 = 0; s2 < segmentGeometry.NumParallelConductors; s2++)
                            {
                                (double r_j, double z_j) = segmentGeometry.GetConductorPosition(j, s2);
                                L[idx, j] += CalcMutualInductance_Lyle(r_i, z_i, segmentGeometry.ConductorType.BareHeight_mm, segmentGeometry.ConductorType.BareWidth_mm, r_j, z_j, segmentGeometry.ConductorType.BareHeight_mm, segmentGeometry.ConductorType.BareWidth_mm) / (2 * Math.PI * r_i);
                                L[j, idx] += CalcMutualInductance_Lyle(r_i, z_i, segmentGeometry.ConductorType.BareHeight_mm, segmentGeometry.ConductorType.BareWidth_mm, r_j, z_j, segmentGeometry.ConductorType.BareHeight_mm, segmentGeometry.ConductorType.BareWidth_mm) / (2 * Math.PI * r_j);
                            }
                        }
                        idx++;
                    }
                }
            }

            return L;
        }

        public Matrix<double> Calc_Lmatrix(Transformer tfmr, double f = 60)
        {
            int total_conductors = 0;
            foreach (Winding wdg in tfmr.Windings)
            {
                total_conductors += wdg.NumConductors;
            }

            LinAlg.Matrix<double> L = LinAlg.Matrix<double>.Build.Dense(total_conductors, total_conductors);

            int idx_i = -1;
            foreach (Winding wdg in tfmr.Windings)
            {
                foreach (var segment in wdg.Segments)
                {
                    if (segment.Geometry == null)
                    {
                        continue; // Skip segments without geometry
                    }

                    var segmentGeometry = segment.Geometry;

                    for (int turn = 0; turn < segmentGeometry.NumTurns; turn++)
                    {
                        for (int strand = 0; strand < segmentGeometry.NumParallelConductors; strand++)
                        {
                            idx_i++;
                            (double r_i, double z_i) = segmentGeometry.GetConductorPosition(turn, strand);
                            L[idx_i, idx_i] = CalcSelfInductance(segmentGeometry.ConductorType.BareHeight_mm, segmentGeometry.ConductorType.BareWidth_mm, r_i, segmentGeometry.ConductorType.rho_c, f);// / (2 * Math.PI * r_i);

                            int idx_j = -1;
                            foreach (Winding otherWdg in tfmr.Windings)
                            {
                                foreach (var otherSegment in otherWdg.Segments)
                                {
                                    if (otherSegment.Geometry == null)
                                    {
                                        continue; // Skip segments without geometry
                                    }

                                    var otherSegmentGeometry = otherSegment.Geometry;

                                    for (int j = 0; j < otherSegmentGeometry.NumTurns; j++)
                                    {
                                        for (int s2 = 0; s2 < otherSegmentGeometry.NumParallelConductors; s2++)
                                        {
                                            idx_j++;
                                            if (idx_j <= idx_i) continue; // Only compute upper triangle, matrix is symmetric
                                            (double r_j, double z_j) = otherSegmentGeometry.GetConductorPosition(j, s2);
                                            L[idx_i, idx_j] += CalcMutualInductance_Lyle(r_i, z_i, segmentGeometry.ConductorType.BareHeight_mm, segmentGeometry.ConductorType.BareWidth_mm, r_j, z_j, otherSegmentGeometry.ConductorType.BareHeight_mm, otherSegmentGeometry.ConductorType.BareWidth_mm);// / (2 * Math.PI * r_i);
                                            L[idx_j, idx_i] += CalcMutualInductance_Lyle(r_i, z_i, segmentGeometry.ConductorType.BareHeight_mm, segmentGeometry.ConductorType.BareWidth_mm, r_j, z_j, otherSegmentGeometry.ConductorType.BareHeight_mm, otherSegmentGeometry.ConductorType.BareWidth_mm);// / (2 * Math.PI * r_j);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return L;
        }

        public Matrix<double> Calc_Cmatrix(Transformer tfmr)
        {
            int total_conductors = 0;
            foreach (Winding wdg in tfmr.Windings)
            {
                total_conductors += wdg.NumConductors;
            }

            LinAlg.Matrix<double> C = LinAlg.Matrix<double>.Build.Dense(total_conductors, total_conductors);

            int start = 0;
            foreach (Winding wdg in tfmr.Windings)
            {
                if (wdg.NumTurns > 0)
                {
                    C.SetSubMatrix(start, start, Calc_Cmatrix_Wdg(wdg));
                    start += wdg.NumTurns;
                }
            }
            return C;
        }

        private Matrix<double> Calc_Cmatrix_Wdg(Winding wdg)
        {
            double eps_oil = 1.0;

            var M = Matrix<double>.Build;

            Matrix<double> C = M.Dense(wdg.NumTurns, wdg.NumTurns);

            //TODO: Need to modify to go from out to in and in to out
            // for (int i = 0; i < wdg.num_discs; i++)
            // {
            //     double C_abv;
            //     double C_bel;
            //     int n_abv;
            //     int n_bel;
            //     double dist_to_ground = 10;
            //     double k = 1.0 / 3.0;
            //     //Console.WriteLine($"Disc {i}");
            //     // For now, let's calculate four capacitances for each turn
            //     // If last disc (section), above is prev disc, below is tank
            //     // TODO: How to handle segments above and below
            //     if (i == (wdg.num_discs - 1))
            //     {
            //         C_abv = Constants.eps_0 * eps_oil * wdg.t_cond / (wdg.h_spacer + 2 * wdg.t_ins);
            //         C_abv = Constants.eps_0 * (k / (2 * wdg.t_ins / wdg.eps_paper + (2 * wdg.t_ins + wdg.h_spacer) /  eps_oil) + (1 - k) / (2 * wdg.t_ins / wdg.eps_paper + (2 * wdg.t_ins + wdg.h_spacer) / wdg.eps_paper)) * (wdg.t_cond + 2 * wdg.t_ins);
            //         n_abv = i - 1;
            //         C_bel = Constants.eps_0 * eps_oil * wdg.t_cond / dist_to_ground;
            //         n_bel = -1;
            //         //Console.WriteLine($"Last (bottom) Disc: C_abv={C_abv} C_bel={C_bel}");
            //     }
            //     else if (i == 0) // If first disc, above is tank, below is next disc
            //     {
            //         C_abv = Constants.eps_0 * eps_oil * wdg.t_cond / dist_to_ground;
            //         n_abv = -1;
            //         C_bel = Constants.eps_0 * eps_oil * wdg.t_cond / (wdg.h_spacer + 2 * wdg.t_ins);
            //         C_bel = Constants.eps_0 * (k / (2 * wdg.t_ins / wdg.eps_paper + (2 * wdg.t_ins + wdg.h_spacer) / eps_oil) + (1 - k) / (2 * wdg.t_ins / wdg.eps_paper + (2 * wdg.t_ins + wdg.h_spacer) / wdg.eps_paper)) * (wdg.t_cond + 2 * wdg.t_ins);
            //         n_bel = i + 1;
            //         //Console.WriteLine($"First (top) Disc: C_abv={C_abv} C_bel={C_bel}");
            //     }
            //     else
            //     {
            //         C_abv = C_bel = Constants.eps_0 * eps_oil * wdg.t_cond / (wdg.h_spacer + 2 * wdg.t_ins);
            //         C_abv = C_bel = Constants.eps_0 * (k / (2 * wdg.t_ins / wdg.eps_paper + (2 * wdg.t_ins + wdg.h_spacer) / eps_oil) + (1 - k) / (2 * wdg.t_ins / wdg.eps_paper + (2 * wdg.t_ins + wdg.h_spacer) / wdg.eps_paper)) * (wdg.t_cond + 2 * wdg.t_ins);
            //         n_abv = i - 1;
            //         n_bel = i + 1;
            //         //Console.WriteLine($"Middle Disc: C_abv={C_abv} C_bel={C_bel}");
            //     }

            //     for (int j = 0; j < wdg.turns_per_disc; j++)
            //     {
            //         double C_lt;
            //         double C_rt;

            //         bool out_to_in = (i % 2 == 0);

            //         // If first turn in section, left is inner winding or core, right is next turn
            //         if ((j == 0 && !out_to_in) || (j == (wdg.turns_per_disc - 1) && out_to_in))
            //         {
            //             C_lt = Constants.eps_0 * wdg.eps_paper * wdg.h_cond / dist_to_ground;
            //             C_rt = Constants.eps_0 * wdg.eps_paper * (wdg.h_cond + 2 * wdg.t_ins) / (2 * wdg.t_ins);

            //         }
            //         else if ((j == (wdg.turns_per_disc - 1) && !out_to_in) || (j == 0 && out_to_in)) // If last turn in section, left is previous turn, right is outer winding or tank
            //         {
            //             C_lt = Constants.eps_0 * wdg.eps_paper * (wdg.h_cond + 2 * wdg.t_ins) / (2 * wdg.t_ins);
            //             C_rt = Constants.eps_0 * wdg.eps_paper * wdg.h_cond / dist_to_ground;
            //         }
            //         else
            //         {
            //             C_lt = C_rt = Constants.eps_0 * wdg.eps_paper * (wdg.h_cond + 2 * wdg.t_ins) / (2 * wdg.t_ins);
            //         }

            //         int disc_abv = i - 1;
            //         if (disc_abv < 0)
            //         {
            //             // ground above
            //             n_abv = -1;
            //         }
            //         else
            //         {
            //             n_abv = disc_abv * wdg.turns_per_disc + (wdg.turns_per_disc - j - 1);
            //         }
            //         int disc_bel = i + 1;
            //         if (disc_bel >= wdg.num_discs)
            //         {
            //             // ground below
            //             n_bel = -1;
            //         }
            //         else
            //         {
            //             n_bel = disc_bel * wdg.turns_per_disc + (wdg.turns_per_disc - j - 1);
            //         }

            //         int n_lt;
            //         int n_rt;

            //         if (i % 2 == 0) //out to in
            //         {
            //             n_lt = i * wdg.turns_per_disc + j + 1;
            //             n_rt = i * wdg.turns_per_disc + j - 1;
            //             if (j == 0)
            //             {
            //                 n_rt = -1;
            //             }
            //             if (j >= (wdg.turns_per_disc - 1))
            //             {
            //                 n_lt = -1;
            //             }
            //             //Console.WriteLine("Out to in");
            //         }
            //         else //in to out
            //         {
            //             n_lt = i * wdg.turns_per_disc + j - 1;
            //             n_rt = i * wdg.turns_per_disc + j + 1;
            //             if (j == 0)
            //             {
            //                 n_lt = -1;
            //             }
            //             if (j >= (wdg.turns_per_disc - 1))
            //             {
            //                 n_rt = -1;
            //             }
            //             //Console.WriteLine($"{j}");
            //             //Console.WriteLine("In to out");
            //         }

            //         int n = i * wdg.turns_per_disc + j;
            //         //Console.WriteLine($"n: {n} n_abv: {n_abv} n_bel: {n_bel} n_lt: {n_lt} n_rt: {n_rt}");

            //         // Assemble C_abv, C_bel, C_lt, C_rt into C_seg
            //         C[n, n] = C_abv + C_bel + C_lt + C_rt;
            //         if (n_abv >= 0)
            //         {
            //             C[n, n_abv] = -C_abv;
            //             C[n_abv, n] = -C_abv;
            //         }
            //         if (n_bel >= 0)
            //         {
            //             C[n, n_bel] = -C_bel;
            //             C[n_bel, n] = -C_bel;
            //         }
            //         if (n_lt >= 0)
            //         {
            //             C[n, n_lt] = -C_lt;
            //             C[n_lt, n] = -C_lt;
            //         }
            //         if (n_rt >= 0)
            //         {
            //             C[n, n_rt] = -C_rt;
            //             C[n_rt, n] = -C_rt;
            //         }
            //     }
            // }

            return C;
        }

        public LinAlg.Matrix<double> Calc_Rmatrix(Transformer tfmr, double f = 60)
        {
            int total_conductors = 0;
            foreach (Winding wdg in tfmr.Windings)
            {
                total_conductors += wdg.NumConductors;
            }

            LinAlg.Matrix<double> R = LinAlg.Matrix<double>.Build.Dense(total_conductors, total_conductors);

            int start = 0;
            foreach (Winding wdg in tfmr.Windings)
            {
                if (wdg.NumTurns > 0)
                {
                    //R.SetSubMatrix(start, start, wdg.Calc_Rmatrix(f));
                    start += wdg.NumTurns;
                }
            }
            return R;
        }

        private double CalcSelfInductance(double h_mm, double w_mm, double r_avg_mm, double rho_c, double f)
        {
            var h = h_mm / 1000.0; // Convert to meters
            var w = w_mm / 1000.0; // Convert to meters
            var r_avg = r_avg_mm / 1000.0; // Convert to meters

            double mu_r = 1.0;
            double sigma = 1.0 / rho_c; // Conductivity of copper (S/m)
            double GMD = Math.Exp(0.5 * Math.Log(h * h + w * w) + 2 * w / (3 * h) * Math.Atan(h / w) + 2 * h / (3 * w) * Math.Atan(w / h) - w * w / (12 * h * h) * Math.Log(1 + h * h / (w * w)) - h * h / (12 * w * w) * Math.Log(1 + w * w / (h * h)) - 25 / 12);
            // Internal inductance
            double L_int_low = Constants.mu_0 * mu_r / (8 * Math.PI);  // Low-frequency internal inductance
            double omega = 2 * Math.PI * f;      // Angular frequency
            double delta = Math.Sqrt(2 / (omega * Constants.mu_0 * mu_r * sigma));  // Skin depth
            double L_int = L_int_low * Math.Min(1, delta / (GMD / 2));  // Smooth transition
            double L_s = L_int + Constants.mu_0 * r_avg * (Math.Log(8 * r_avg / GMD) - 2);
            //Console.WriteLine($"r_avg: {r_avg} GMD: {GMD} L_s: {L_s / 1e-9} L_s/l: {L_s / (2 * Math.PI * r_avg) / 1e-9}");
            return L_s;
        }

        private double CalcInductanceCoaxLoops(double r_a_mm, double z_a_mm, double r_b_mm, double z_b_mm)
        {
            double r_a = r_a_mm / 1000.0; // Convert to meters
            double z_a = z_a_mm / 1000.0; // Convert to meters
            double r_b = r_b_mm / 1000.0; // Convert to meters
            double z_b = z_b_mm / 1000.0; // Convert to meters

            if (r_a == 0 || r_b == 0) return 0;

            double d = Math.Abs(z_b - z_a);
            double k = Math.Sqrt(4 * r_a * r_b / ((r_a + r_b) * (r_a + r_b) + d * d));
            double L_ab = 2 * Constants.mu_0 / k * Math.Sqrt(r_a * r_b) * ((1 - k * k / 2) * Elliptic.EllipticK(k) - Elliptic.EllipticE(k));
            return L_ab;
        }

        // r is mean radius of each turn
        // z is height of mid-point
        private (double r_i, double z_i, double r_j, double z_j) CalcLyleLoops(double r, double z, double w, double h)
        {
            double r_i, z_i, r_j, z_j;

            if (h > w)
            {
                // Two rings with r_avg = r_1 and at d +/- beta
                double r_adj = r * (1 + w * w / (24 * r * r));
                double beta = Math.Sqrt(((h * h) - (w * w)) / 12);
                r_i = r_adj;
                z_i = z + beta;
                r_j = r_adj;
                z_j = z - beta;
            }
            else
            {
                // Two rings with r_avg = r_1 and r_1 + 2*delta and at z = d
                double r_adj = r * (1 + h * h / (24 * r * r));
                double delta = Math.Sqrt(((w * w) - (h * h)) / 12);
                r_i = r_adj - delta;
                z_i = z;
                r_j = r_adj + delta;
                z_j = z;
            }

            return (r_i, z_i, r_j, z_j);
        }

        private double CalcMutualInductance_Lyle(double r_a, double z_a, double h_a, double w_a, double r_b, double z_b, double h_b, double w_b)
        {
            var ta = CalcLyleLoops(r_a, z_a, w_a, h_a);
            double r_1 = ta.r_i;
            double z_1 = ta.z_i;
            double r_2 = ta.r_j;
            double z_2 = ta.z_j;

            var tb = CalcLyleLoops(r_b, z_b, w_b, h_b);
            double r_3 = tb.r_i;
            double z_3 = tb.z_i;
            double r_4 = tb.r_j;
            double z_4 = tb.z_j;

            double L_13 = CalcInductanceCoaxLoops(r_1, z_1, r_3, z_3);
            double L_14 = CalcInductanceCoaxLoops(r_1, z_1, r_4, z_4);
            double L_23 = CalcInductanceCoaxLoops(r_2, z_2, r_3, z_3);
            double L_24 = CalcInductanceCoaxLoops(r_2, z_2, r_4, z_4);
            double L_ab = (L_13 + L_14 + L_23 + L_24) / 4;

            return L_ab;

        }

    }
}
