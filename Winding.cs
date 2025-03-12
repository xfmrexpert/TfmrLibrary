﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TDAP;
using LinAlg = MathNet.Numerics.LinearAlgebra;

namespace TfmrLib
{
    

    public class Winding
    {
        public double eps_paper; //3.5;
        public double rho_c; //ohm-m;

        public double dist_wdg_tank_right;
        public double dist_wdg_tank_top;
        public double dist_wdg_tank_bottom;
        
        public double r_inner;
        public double t_cond;
        public double h_cond;
        public double t_ins;
        public double h_spacer;
        public double r_cond_corner;
        public int num_discs;
        public int turns_per_disc;
        
        public Complex Rs;
        public Complex Rl;

        //public int phyAir;
        //public int phyExtBdry;
        //public int phyAxis;
        //public int phyInf;
        public int[] phyTurnsCondBdry;
        public int[] phyTurnsCond;
        public int[] phyTurnsIns;

        public int num_turns
        {
            get
            {
                return num_discs * turns_per_disc;
            }
        }

        public Winding()
        {
            dist_wdg_tank_right = 4;
            dist_wdg_tank_top = 2;
            dist_wdg_tank_bottom = 2;
            r_inner = Conversions.in_to_m(15.25);
            t_cond = Conversions.in_to_m(0.085);
            h_cond = Conversions.in_to_m(0.3);
            t_ins = Conversions.in_to_m(0.018);
            h_spacer = Conversions.in_to_m(0.188);
            r_cond_corner = Conversions.in_to_m(0.032);
            num_discs = 14;
            turns_per_disc = 20;
            
            eps_paper = 2.2; //3.5;
            rho_c = Constants.rho_copper; //ohm-m;
            Rs = Complex.Zero;
            Rl = Complex.Zero;
        }

        public double StrandArea()
        {
            return t_cond * h_cond - ((4-Math.PI) * r_cond_corner * r_cond_corner);
        }

        public (double r, double z) GetTurnMidpoint(int n)
        {
            double r, z;
            int disc = (int)Math.Floor((double)n / (double)turns_per_disc);
            int turn = n % turns_per_disc;
            //Console.WriteLine($"turn: {n} disc: {disc} turn in disc: {turn}");

            if (disc % 2 == 0)
            {
                //out to in
                r = r_inner + (turns_per_disc - turn) * (t_cond + 2 * t_ins) - (t_cond / 2 + t_ins);
            }
            else
            {
                //in to out
                r = r_inner + turn * (t_cond + 2 * t_ins) + (t_cond / 2 + t_ins);
            }
            z = dist_wdg_tank_bottom + num_discs * (h_cond + 2 * t_ins) + (num_discs - 1) * h_spacer - (h_cond / 2 + t_ins) - disc * (h_cond + 2 * t_ins + h_spacer);
            //Console.WriteLine($"turn: {n} disc: {disc} turn in disc: {turn} r: {r} z:{z}");
            return (r, z);
        }

        public GeomLineLoop[] GenerateGeometry(ref Geometry geometry)
        {
            bool include_ins = true;

            double z_offset = (num_discs * (h_cond + 2 * t_ins) + (num_discs - 1) * h_spacer + dist_wdg_tank_bottom + dist_wdg_tank_top) / 2;

            // Setup conductor and insulation boundaries
            var conductorins_bdrys = new GeomLineLoop[num_discs * turns_per_disc];

            phyTurnsCond = new int[num_turns];
            phyTurnsCondBdry = new int[num_turns];
            if (include_ins)
            {
                phyTurnsIns = new int[num_turns];
            }

            for (int i = 0; i < num_turns; i++)
            {
                (double r, double z) = GetTurnMidpoint(i);
                z = z - z_offset;
                var conductor_bdry = geometry.AddRoundedRectangle(r, z, h_cond, t_cond, r_cond_corner, 0.0004);
                
                phyTurnsCondBdry[i] = conductor_bdry.AttribID = geometry.NextLineTag;

                if (include_ins)
                {
                    var insulation_bdry = geometry.AddRoundedRectangle(r, z, h_cond + 2 * t_ins, t_cond + 2 * t_ins, r_cond_corner + t_ins, 0.003);
                    var insulation_surface = geometry.AddSurface(insulation_bdry, conductor_bdry);
                    insulation_surface.AttribID = phyTurnsIns[i] = geometry.NextSurfaceTag;
                    conductorins_bdrys[i] = insulation_bdry;
                }
                else
                {
                    conductorins_bdrys[i] = conductor_bdry;
                }
                
                var conductor_surface = geometry.AddSurface(conductor_bdry);
                conductor_surface.AttribID = phyTurnsCond[i] = geometry.NextSurfaceTag;
            }

            return conductorins_bdrys;
        }

        public virtual LinAlg.Matrix<double> Calc_Lmatrix(double f = 60) { return null; }

        public virtual LinAlg.Matrix<double> Calc_Cmatrix() { return null; }

        public LinAlg.Vector<double> Calc_TurnRadii()
        {
            LinAlg.Vector<double> r_t = LinAlg.Vector<double>.Build.Dense(num_turns);
            for (int disc = 0; disc < num_discs; disc++)
            {
                for (int turn = 0; turn < turns_per_disc; turn++)
                {
                    int n = disc * turns_per_disc + turn;
                    r_t[n] = GetTurnMidpoint(n).r;
                }
            }
            return r_t;
        }

        public LinAlg.Matrix<double> Calc_Rmatrix(double f = 60)
        {
            var R = LinAlg.Matrix<double>.Build.Dense(num_turns, num_turns);

            for (int t = 1; t < num_turns; t++)
            {
                R[t, t] = R_c();
            }

            double sigma_c = 1 / rho_c;
            Complex eta = Complex.Sqrt(2d * Math.PI * f * Constants.mu_0 * sigma_c * Complex.ImaginaryOne) * t_cond / 2d;
            double R_skin = (1 / (sigma_c * h_cond * t_cond) * eta * Complex.Cosh(eta) / Complex.Sinh(eta)).Real;
            //Console.WriteLine($"R_skin: {R_skin}");
            R_skin = 0;
            var R_f = R + LinAlg.Matrix<double>.Build.DenseIdentity(num_turns, num_turns) * R_skin;
            return R_f;
        }

        public double R_c()
        {
            return rho_c / (StrandArea());
        }
    }
}
