using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace TfmrLib.FEM
{
    public class GetDPProblem : FEMProblem
    {
        public string Filename { get; set; } = "case.pro"; // default
        public string MeshFile { get; set; } = "case.msh"; // default
        public string? GetDPPath { get; set; }  // configurable
        public int Order { get; set; } = 1;
        public bool ShowInTerminal { get; set; } = false;

        private string FindGetDPExecutable()
        {
            if (!string.IsNullOrEmpty(GetDPPath) && File.Exists(GetDPPath))
                return GetDPPath;

            var envPath = Environment.GetEnvironmentVariable("GETDP_PATH");
            if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
                return envPath;

            string[] relativePaths = { "./bin/getdp", "../bin/getdp", "../../../bin/getdp" };
            foreach (var rel in relativePaths)
                if (File.Exists(rel)) return Path.GetFullPath(rel);

            // PATH search
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    var candidate = Path.Combine(dir, "getdp");
                    if (File.Exists(candidate)) return candidate;
                }
            }

            string[] systemPaths = { "/usr/bin/getdp", "/usr/local/bin/getdp" };
            foreach (var sys in systemPaths)
                if (File.Exists(sys)) return sys;

            throw new FileNotFoundException("getdp executable not found. Set GetDPPath property or GETDP_PATH environment variable.");
        }

        private string? FindTerminal()
        {
            string[] terminals = { "cosmic-term", "gnome-terminal", "xterm", "konsole" };
            var pathEnv = Environment.GetEnvironmentVariable("PATH");
            if (pathEnv != null)
            {
                foreach (var dir in pathEnv.Split(Path.PathSeparator))
                {
                    foreach (var term in terminals)
                    {
                        var candidate = Path.Combine(dir, term);
                        if (File.Exists(candidate)) return candidate;
                    }
                }
            }
            return null;
        }

        protected virtual void WriteGetDPFile()
        {
            Console.WriteLine($"Writing GetDP file to {Filename}");
            // Check if directory exists, create if not
            var dir = Path.GetDirectoryName(Filename);
            if (dir != null && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            var f = File.CreateText(Filename);

            f.WriteLine($"FE_Order = {Order};");

            f.WriteLine("Group{");
            foreach (var region in Regions)
            {
                f.WriteLine($"  {region.Name} = Region[{{{string.Join(", ", region.Tags)}}}];");
            }
            foreach (var bc in BoundaryConditions)
            {
                f.WriteLine($"  {bc.Name} = Region[{{{string.Join(", ", bc.Tags)}}}];");
            }
            // Material groups (aggregate tags of all regions sharing the material)
            foreach (var mat in Materials)
            {
                var tags = Regions
                           .Where(r => r.Material == mat)
                           .SelectMany(r => r.Tags)
                           .Distinct()
                           .ToList();
                if (tags.Count > 0)
                    f.WriteLine($"  {mat.Name} = Region[{{{string.Join(", ", tags)}}}];");
            }
            f.WriteLine($"  ProblemDomain = Region[{{{string.Join(", ", Regions.SelectMany(r => r.Tags).Distinct())}}}];");
            f.WriteLine("   Vol_Mag += Region[ProblemDomain];");
            // Define conducting regions (those with a conductivity property)
            var conductingRegions = Regions
                .Where(r => r.TryGetProperty("sigma", out double sigma) && sigma > 0)
                .ToList();
            if (conductingRegions.Count == 0)
                throw new Exception("No conducting regions defined (regions with a 'sigma' property > 0)");
            f.WriteLine($"  Vol_C_Mag = Region[{{{string.Join(", ", conductingRegions.SelectMany(r => r.Tags).Distinct())}}}];");
            f.WriteLine($"  ConductorList = Region[{{{string.Join(", ", conductingRegions.Select(r => r.Name))}}}];");
            f.WriteLine("}");

            // Collect all property names
            var allProps =
                Materials.SelectMany(m => m.Properties.Keys)
                         .Concat(Regions.SelectMany(r => r.Properties.Keys))
                         .Distinct()
                         .ToList();

            if (allProps.Count() > 0)
            {
                f.WriteLine("Function{");
                foreach (var prop in allProps)
                {

                    // Material-level assignment
                    foreach (var mat in Materials)
                        if (mat.Properties.TryGetValue(prop, out var valMat))
                            f.WriteLine($"  {prop}[{mat.Name}] = {valMat:R};");

                    // Region-specific properties override material properties
                    foreach (var region in Regions)
                        if (region.Properties.TryGetValue(prop, out var valProp))
                            f.WriteLine($"  {prop}[{region.Name}] = {valProp:R};");
                }
                f.WriteLine("}");
            }
            f.Close();
        }

        public override void Solve()
        {
            string mygetdp = FindGetDPExecutable();
            Console.WriteLine($"Using getdp at: {mygetdp}");

            WriteGetDPFile();

            string args = $"{Filename} -msh {MeshFile} -solve Magnetodynamics2D_av -pos dyn -v 5";
            
            if (ShowInTerminal)
            {
                var term = FindTerminal() ?? throw new Exception("No terminal emulator found (gnome-terminal/xterm/konsole).");
                var exitFile = Path.GetTempFileName();
                try
                {
                    using var p = new Process();
                    if (term.Contains("gnome-terminal"))
                    {
                        // Keep window open: capture exit code, then wait for Enter.
                        // --wait lets our process block until the bash command (including read) finishes.
                        p.StartInfo.FileName = term;
                        p.StartInfo.Arguments =
                            $"--wait -- bash -lc \"'{mygetdp}' {args}; code=$?; echo $code > '{exitFile}'; " +
                            "echo; echo 'getdp exited with code '$code'. Press Enter to close...'; read\"";
                    }
                    else if (term.Contains("xterm"))
                    {
                        p.StartInfo.FileName = term;
                        p.StartInfo.Arguments =
                            $"-e bash -lc \"'{mygetdp}' {args}; code=$?; echo $code > '{exitFile}'; " +
                            "echo; echo 'getdp exited with code '$code'. Press Enter to close...'; read\"";
                    }
                    else // konsole
                    {
                        // --noclose keeps window by default, but we still add a read for consistency
                        p.StartInfo.FileName = term;
                        p.StartInfo.Arguments =
                            $"--noclose -e bash -lc \"'{mygetdp}' {args}; code=$?; echo $code > '{exitFile}'; " +
                            "echo; echo 'getdp exited with code '$code'. Press Enter to close...'; read\"";
                    }
                    p.StartInfo.UseShellExecute = false;
                    Console.WriteLine("Launching getdp in terminal:");
                    Console.WriteLine($"{mygetdp} {args}");
                    p.Start();
                    p.WaitForExit();

                    int exitCode = 0;
                    if (File.Exists(exitFile))
                    {
                        var txt = File.ReadAllText(exitFile).Trim();
                        if (!int.TryParse(txt, out exitCode))
                            Console.WriteLine("Warning: could not parse exit code text: " + txt);
                    }
                    if (exitCode != 0)
                        throw new Exception($"getdp exited with code {exitCode}");
                }
                finally
                {
                    try { if (File.Exists(exitFile)) File.Delete(exitFile); } catch { }
                }
                return;
            }

            // Non-terminal (background) mode with simple timeout + live output to console
            int return_code = -999;
            object returnCodeLock = new();
            while (return_code < 0)
            {
                var sb = new StringBuilder();
                using var p = new Process();
                p.StartInfo.FileName = mygetdp;
                p.StartInfo.Arguments = args;
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;

                p.OutputDataReceived += (s, a) => { if (a.Data != null) { Console.WriteLine(a.Data); sb.AppendLine(a.Data); } };
                p.ErrorDataReceived += (s, a) => { if (a.Data != null) { Console.WriteLine(a.Data); sb.AppendLine(a.Data); } };

                var timer = new System.Timers.Timer(60000);
                timer.Elapsed += (s, e) =>
                {
                    if (!p.HasExited)
                    {
                        try { p.Kill(true); } catch { }
                        Console.WriteLine("getdp killed (timeout).");
                        lock (returnCodeLock) return_code = -1;
                        timer.Stop();
                    }
                };

                Console.WriteLine("Running (background): " + mygetdp + " " + args);
                p.Start();
                timer.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
                timer.Stop();
                timer.Dispose();

                return_code = p.ExitCode;
                if (return_code > 0)
                    Console.WriteLine(sb.ToString());
                if (return_code != 0 && return_code != -1)
                    throw new Exception($"Failed to run getdp (exit {return_code})");
            }
        }
    }

    public class GetDPAxiMagProblem : GetDPProblem
    {
        // Need a way to identify conducting regions generically, based on region properties, I suppose
        // I suppose we could just assume any region with a conductivity property is a conductor?

        protected override void WriteGetDPFile()
        {
            base.WriteGetDPFile();
            Console.WriteLine($"Appending Magnetodynamics2D_av definitions to {Filename}");
            var f = File.AppendText(Filename);
            f.WriteLine();
            f.WriteLine("// Additional definitions for Magnetodynamics2D_av");
            f.WriteLine("// Auto-generated by TfmrLib");
            f.WriteLine();
            f.WriteLine("Freq = 60; // Hz");
            f.WriteLine("Group {");
            f.WriteLine("  DefineGroup[");
            f.WriteLine("    // The full magnetic domain:");
            f.WriteLine("    Vol_Mag,");
            f.WriteLine("    // Subsets of Vol_Mag:");
            f.WriteLine("    Vol_C_Mag // massive conductors");
            f.WriteLine("  ];");
            f.WriteLine("}");

            f.WriteLine("Function {");
            f.WriteLine("  Mu0 = 4*Pi*1e-7;");
            f.WriteLine("  mu[All] = Mu0 * mu_r[];");
            f.WriteLine("  nu[] = 1/mu[];");
            f.WriteLine("  CoefGeo[] = 1.0; //2*Pi;");
            f.WriteLine("}");

            f.WriteLine("Constraint {");
            f.WriteLine("  { Name MagneticVectorPotential_2D;");
            f.WriteLine("    Case { { Region Axis; Value 0; }");
            f.WriteLine("           { Region Dirichlet; Value 0; } }");
            f.WriteLine("  }");
            // f.WriteLine("  If(FE_Order == 2)");
            // f.WriteLine("    { Name MagneticVectorPotential_2D_0;");
            // f.WriteLine("      Case { { Region Dirichlet; Value 0; } }");
            // f.WriteLine("    }");
            // f.WriteLine("  EndIf");
            f.WriteLine("  { Name Current_2D;");
            f.WriteLine("    Case {");
            f.WriteLine("      // Amplitude of the phasor is set to \"Current\"");
            foreach (var exc in Excitations)
            {
                f.WriteLine($"      {{ Region {exc.Region.Name}; Value {exc.Value}; }}");
            }
            f.WriteLine("    }");
            f.WriteLine("  }");
            f.WriteLine("}");

            f.WriteLine("Group{");
            f.WriteLine("  // all volumes + surfaces on which integrals will be computed");
            f.WriteLine("  Dom_Mag = Region[ {Vol_Mag} ];");
            f.WriteLine("  DomainDummy = Region[ 12345 ] ; // Dummy region number for postpro with functions");
            f.WriteLine("}");

            f.WriteLine("Jacobian {");
            f.WriteLine("  { Name Vol;");
            f.WriteLine("       Case {");
            f.WriteLine("           { Region All; Jacobian VolAxiSqu; }");
            f.WriteLine("       }");
            f.WriteLine("   }");
            f.WriteLine("   { Name Sur;");
            f.WriteLine("       Case {");
            f.WriteLine("           { Region All; Jacobian SurAxi; }");
            f.WriteLine("       }");
            f.WriteLine("   }");
            f.WriteLine("}");

            f.WriteLine("Integration {");
            f.WriteLine("   { Name Gauss_v;");
            f.WriteLine("     Case {");
            f.WriteLine("       { Type Gauss;");
            f.WriteLine("         Case {");
            f.WriteLine("           { GeoElement Point; NumberOfPoints 1; }");
            f.WriteLine("           { GeoElement Line; NumberOfPoints 5; }");
            f.WriteLine("           { GeoElement Triangle; NumberOfPoints 7; }");
            f.WriteLine("           { GeoElement Quadrangle; NumberOfPoints 4; }");
            f.WriteLine("           { GeoElement Tetrahedron; NumberOfPoints 15; }");
            f.WriteLine("           { GeoElement Hexahedron; NumberOfPoints 14; }");
            f.WriteLine("           { GeoElement Prism; NumberOfPoints 21; }");
            f.WriteLine("         }");
            f.WriteLine("       }");
            f.WriteLine("     }");
            f.WriteLine("   }");
            f.WriteLine("}");

            // The following FunctionSpace means we need to define regions for Dom_Mag and Vol_Mag
            // as well as (if used) boundary conditions for MagneticVectorPotential_2D and MagneticVectorPotential_2D_0
            f.WriteLine("FunctionSpace {");
            f.WriteLine("  { Name Hcurl_a_2D; Type Form1P;");
            f.WriteLine("    BasisFunction {");
            f.WriteLine("      { Name s_n; NameOfCoef a_n; Function BF_PerpendicularEdge;");
            f.WriteLine("        Support Dom_Mag; Entity NodesOf[All]; }");
            f.WriteLine("      If(FE_Order == 2)");
            f.WriteLine("        { Name s_e; NameOfCoef a_e; Function BF_PerpendicularEdge_2E;");
            f.WriteLine("          Support Vol_Mag; Entity EdgesOf[All]; }");
            f.WriteLine("      EndIf");
            f.WriteLine("    }");
            f.WriteLine("    Constraint {");
            f.WriteLine("      { NameOfCoef a_n;");
            f.WriteLine("        EntityType NodesOf; NameOfConstraint MagneticVectorPotential_2D; }");
            f.WriteLine("      If(FE_Order == 2)");
            f.WriteLine("        { NameOfCoef a_e;");
            f.WriteLine("          EntityType EdgesOf; NameOfConstraint MagneticVectorPotential_2D_0; }");
            f.WriteLine("      EndIf");
            f.WriteLine("    }");
            f.WriteLine("  }");
            f.WriteLine("}");

            f.WriteLine("FunctionSpace {");
            f.WriteLine("  // Gradient of Electric scalar potential (2D)");
            f.WriteLine("  { Name Hregion_u_2D; Type Form1P; // same as for \\vec{a}");
            f.WriteLine("    BasisFunction {");
            f.WriteLine("      { Name sr; NameOfCoef ur; Function BF_RegionZ;");
            f.WriteLine("        // constant vector (over the region) with nonzero z-component only");
            f.WriteLine("        Support Region[{Vol_C_Mag}];");
            f.WriteLine("        Entity Region[{Vol_C_Mag}]; }");
            f.WriteLine("    }");
            f.WriteLine("    GlobalQuantity {");
            f.WriteLine("      { Name U; Type AliasOf; NameOfCoef ur; }");
            f.WriteLine("      { Name I; Type AssociatedWith; NameOfCoef ur; }");
            f.WriteLine("    }");
            f.WriteLine("    Constraint {");
            f.WriteLine("      { NameOfCoef I;");
            f.WriteLine("        EntityType Region; NameOfConstraint Current_2D; }");
            f.WriteLine("    }");
            f.WriteLine("  }");
            f.WriteLine("}");

            // For Formulation, we need regions Vol_Mag and Vol_C_Mag
            // Vol_Mag is the magnetic field domain, Vol_C_Mag is the conducting domain
            // We also need to define the coefficients nu and sigma in the regions appropriately
            f.WriteLine("Formulation {");
            f.WriteLine("  { Name Magnetodynamics2D_av; Type FemEquation;");
            f.WriteLine("    Quantity {");
            f.WriteLine("      { Name a; Type Local; NameOfSpace Hcurl_a_2D; }");
            f.WriteLine("      { Name ur; Type Local; NameOfSpace Hregion_u_2D; }");
            f.WriteLine("      { Name I; Type Global; NameOfSpace Hregion_u_2D [I]; }");
            f.WriteLine("      { Name U; Type Global; NameOfSpace Hregion_u_2D [U]; }");
            f.WriteLine("    }");
            f.WriteLine("    Equation {");
            f.WriteLine("      Integral { [ nu[] * Dof{d a} , {d a} ];");
            f.WriteLine("        In Vol_Mag; Jacobian Vol; Integration Gauss_v; }");
            f.WriteLine("      // Electric field e = -Dt[{a}]-{ur},");
            f.WriteLine("      // with {ur} = Grad v constant in each region of Vol_C_Mag");
            f.WriteLine("      Integral { DtDof [ sigma[] * Dof{a} , {a} ];");
            f.WriteLine("        In Vol_C_Mag; Jacobian Vol; Integration Gauss_v; }");
            f.WriteLine("      Integral { [ sigma[] * Dof{ur} , {a} ];");
            f.WriteLine("        In Vol_C_Mag; Jacobian Vol; Integration Gauss_v; }");

            f.WriteLine("      // When {ur} act as a test function, one obtains the circuits relations,");
            f.WriteLine("      // relating the voltage and the current of each region in Vol_C_Mag");
            f.WriteLine("      Integral { DtDof [ sigma[] * Dof{a} , {ur} ];");
            f.WriteLine("        In Vol_C_Mag; Jacobian Vol; Integration Gauss_v; }");
            f.WriteLine("      Integral { [ sigma[] * Dof{ur}, {ur} ];");
            f.WriteLine("        In Vol_C_Mag; Jacobian Vol; Integration Gauss_v; }");
            f.WriteLine("      GlobalTerm { [ Dof{I} , {U} ]; In Vol_C_Mag; }");

            f.WriteLine("      // Attention: CoefGeo[.] = 2*Pi for Axial symmetry");
            f.WriteLine("    }");
            f.WriteLine("  }");
            f.WriteLine("}");


            f.WriteLine("Resolution {");
            f.WriteLine("  { Name Magnetodynamics2D_av;");
            f.WriteLine("    System {");
            f.WriteLine("      { Name A; NameOfFormulation Magnetodynamics2D_av;");
            f.WriteLine("        Type ComplexValue; Frequency Freq;");
            f.WriteLine("      }");
            f.WriteLine("    }");
            f.WriteLine("    Operation {");
            //f.WriteLine("      CreateDirectory[resPath];");
            f.WriteLine("      InitSolution[A];");
            f.WriteLine("      Generate[A];");
            f.WriteLine("      SetFrequency[A, Freq];");
            f.WriteLine("      Solve[A];");
            f.WriteLine("      SaveSolution[A];");
            f.WriteLine("    }");
            f.WriteLine("  }");
            f.WriteLine("}");

            // Same PostProcessing for both static and dynamic formulations (both refer to
            // the same FunctionSpace from which the solution is obtained)
            f.WriteLine("PostProcessing {");
            f.WriteLine("  { Name Magnetodynamics2D_av; NameOfFormulation Magnetodynamics2D_av;");
            f.WriteLine("    PostQuantity {");
            f.WriteLine("      // In 2D, a is a vector with only a z-component: (0,0,az)");
            f.WriteLine("      { Name a; Value {");
            f.WriteLine("          Term { [ {a} ]; In Vol_Mag; Jacobian Vol; }");
            f.WriteLine("        }");
            f.WriteLine("      }");
            f.WriteLine("      // The equilines of az are field lines (giving the magnetic field direction)");
            f.WriteLine("      { Name az; Value {");
            f.WriteLine("          Term { [ CompZ[{a}] ]; In Vol_Mag; Jacobian Vol; }");
            f.WriteLine("        }");
            f.WriteLine("      }");
            f.WriteLine("      { Name Inductance_from_Flux; Value { Term { Type Global; [ $Flux ]; In DomainDummy; } } }");
            f.WriteLine("      { Name L; ");
            f.WriteLine("        Value { ");
            f.WriteLine("          Term { [ -Im [ {U}/(2*Pi*Freq)]]; In ConductorList; }");
            f.WriteLine("        }");
            f.WriteLine("      }");
            f.WriteLine("    }");
            f.WriteLine("  }");
            f.WriteLine("}");

            f.WriteLine("PostOperation {");
            f.WriteLine("  { Name dyn; NameOfPostProcessing Magnetodynamics2D_av;");
            f.WriteLine("    Operation {");
            f.WriteLine("      Print[ az, OnElementsOf Vol_Mag, File \"az.pos\" ];");
            f.WriteLine("      Print[ L, OnRegion ConductorList, File \"out.txt\",");
            f.WriteLine("      Format Table, SendToServer \"Output/Global/Inductance [H]\" ];");
            f.WriteLine("      Print[ Inductance_from_Flux [ConductorList], OnRegion ConductorList, File \"out2.txt\",");
            f.WriteLine("      Format Table, SendToServer \"Output/Global/Inductance2 [H]\" ];");
            //f.WriteLine("      Print[ JouleLosses, OnRegion Vol_C_Mag, File \"out3.txt\", Format Table];");
            f.WriteLine("    }");
            f.WriteLine("  }");
            f.WriteLine("}");

            f.Close();
        }

    }
}