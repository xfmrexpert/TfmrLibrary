using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GeometryLib;

namespace TfmrLib.TransientModels
{
    public class TransientModelGeometryFactory
    {
        private Geometry _geometry = new();
        public Geometry Geometry
        {
            get => _geometry;
        }

        private TagManager _tagManager = new();

        public TagManager TagManager
        {
            get => _tagManager;
        }

        public void BuildGeometry(Transformer tfmr)
        {
            _tagManager.ClearTags();

            var outer_bdry = CreateGeometry(tfmr.Core);

            List<GeomLineLoop> conductorins_bdrys = new List<GeomLineLoop>();

            foreach (Winding wdg in tfmr.Windings)
            {
                conductorins_bdrys.AddRange(this.CreateGeometry(wdg));
            }

            var interior_surface = _geometry.AddSurface(outer_bdry, conductorins_bdrys.ToArray());
            _tagManager.TagEntityByString(interior_surface, "InteriorDomain");
        }

        public GeomLineLoop[] CreateGeometry(Winding wdg)
        {
            var rtnLoop = new List<GeomLineLoop>();
            foreach (var seg in wdg.Segments)
            {
                if (seg.Geometry != null)
                {
                    rtnLoop.AddRange(this.CreateGeometry(seg.Geometry));
                }
            }
            return rtnLoop.ToArray();
        }

        public GeomLineLoop[] CreateGeometry(SegmentGeometry seg) => seg switch
        {
            DiscSegmentGeometry discSeg => CreateGeometry(discSeg),
            HelicalSegmentGeometry helSeg => CreateGeometry(helSeg),
            LayerSegmentGeometry laySeg => CreateGeometry(laySeg),
            null => throw new ArgumentNullException(nameof(seg)),
            _ => throw new NotSupportedException(seg.GetType().Name)
        };

        public GeomLineLoop[] CreateGeometry(DiscSegmentGeometry seg)
        {
            bool include_ins = true;
            // It appears as though I established the z_offset to half of the winding height.  Why?  No idea.
            double z_offset = seg.ParentTransformer.Core.WindowHeight_mm / 2;

            // Note: The number of total turns in a winding may differ from the number of turns in an axisymmetric section
            // because of partial turns (ie. the disc cross-overs may not be axially aligned).

            // Setup conductor and insulation boundaries
            // Loop through conductors in logical order and tag them using the turn map
            // TODO: This should be on DiscSegmentGeometry, no?
            int totalConductors = seg.NumDiscs * seg.TurnsPerDisc * seg.NumParallelConductors;

            if (seg.ConductorLocations.Count != totalConductors)
                throw new InvalidOperationException($"Conductor location count {seg.ConductorLocations.Count} does not match expected number of conductor positions {totalConductors}.");

            // Setup conductor and insulation boundaries
            var conductorins_bdrys = new GeomLineLoop[totalConductors];

            for (int conductorIndex = 0; conductorIndex < totalConductors; conductorIndex++)
            {
                var logical = seg.GetElectricalLocation(conductorIndex);
                var location = seg.ConductorLocations[conductorIndex];
                var locationKey = new LocationKey(seg.ParentWinding.Id, seg.ParentSegment.Id, logical.Turn, logical.Strand);

                var (conductorBoundary, insulationBoundary) = seg.ConductorType.CreateGeometry(ref _geometry, location.RadialPosition_mm, location.AxialPosition_mm - z_offset);

                _tagManager.TagEntityByLocation(conductorBoundary, locationKey, TagType.ConductorBoundary);
                _tagManager.TagEntityByLocation(insulationBoundary, locationKey, TagType.InsulationBoundary);

                if (include_ins)
                {
                    var insulation_surface = _geometry.AddSurface(insulationBoundary, conductorBoundary);
                    _tagManager.TagEntityByLocation(insulation_surface, locationKey, TagType.InsulationSurface);
                    conductorins_bdrys[conductorIndex] = insulationBoundary;
                }
                else
                {
                    conductorins_bdrys[conductorIndex] = conductorBoundary;
                }

                // 2D region that is conductor interior
                var conductor_surface = _geometry.AddSurface(conductorBoundary);
                _tagManager.TagEntityByLocation(conductor_surface, locationKey, TagType.ConductorSurface);
            }

            // Return outer boundary of conductor representations to allow "holes" in the remainder of the geometry
            return conductorins_bdrys;
        }

        // TODO: This needs to be modified to use ConductorLocations
        public GeomLineLoop[] CreateGeometry(HelicalSegmentGeometry seg)
        {
            bool include_ins = true;

            // Setup conductor and insulation boundaries
            var conductorins_bdrys = new GeomLineLoop[seg.NumMechTurns * seg.NumParallelConductors];

            GeomLineLoop? conductor_bdry;
            GeomLineLoop? insulation_bdry;

            int cond_idx = 0;

            int wdg_direction;
            double z_mid;
            if (seg.ParentSegment.StartLocation == StartNodeLocation.Top)
            {
                z_mid = seg.DistanceAboveBottomYoke_mm + seg.SegmentHeight_mm - seg.ConductorType.TotalHeight_mm / 2; //Turn 0
                wdg_direction = -1; // Winding segment goes from top to bottom
            }
            else
            {
                z_mid = seg.DistanceAboveBottomYoke_mm + seg.ConductorType.TotalHeight_mm / 2; // Turn 0
                wdg_direction = 1; // Winding segment goes from bottom to top
            }

            int numSpacers = seg.NumMechTurns - 1; // Spacers between each turn

            using var spacerEnum = seg.SpacerPattern.GetEnumerator();
            bool hasNextSpacer = false;

            for (int turn = 0; turn < seg.NumMechTurns; turn++)
            {
                double gap = 0.0;
                if (turn > 0)
                {
                    hasNextSpacer = spacerEnum.MoveNext();
                    if (!hasNextSpacer)
                        throw new InvalidOperationException($"Spacer pattern ended prematurely at turn {turn}.");
                    gap = spacerEnum.Current.SpacerHeight_mm;
                }

                if (seg.ParallelOrientation == Orientation.Radial)
                {
                    // Calculate the z-coordinate based on the turn accounting for spacer pattern
                    // Sum the heights of all previous turns and spacers
                    if (turn > 0)
                    {
                        z_mid += wdg_direction * (seg.ConductorType.TotalHeight_mm + gap);
                    }

                    for (int strand = 0; strand < seg.NumParallelConductors; strand++)
                    {
                        var loc = new LocationKey(seg.ParentWinding.Id, seg.ParentSegment.Id, turn, strand);
                        double r_mid = seg.InnerRadius_mm + strand * seg.ConductorType.TotalWidth_mm + seg.ConductorType.TotalWidth_mm / 2;
                        (conductor_bdry, insulation_bdry) = seg.ConductorType.CreateGeometry(ref _geometry, r_mid, z_mid - seg.ParentTransformer.Core.WindowHeight_mm / 2);
                        _tagManager.TagEntityByLocation(conductor_bdry, loc, TagType.ConductorBoundary);
                        if (insulation_bdry != null)
                        {
                            var insulation_surface = _geometry.AddSurface(insulation_bdry, conductor_bdry);
                        }

                        //TODO: The above call to ConductorType.CreateGeometry will create both the conductor and the insulation boundaries
                        // We probably don't want this if we aren't modelling the insulation explicitly
                        if (include_ins)
                        {
                            var insulation_surface = _geometry.AddSurface(insulation_bdry, conductor_bdry);
                            _tagManager.TagEntityByLocation(insulation_surface, loc, TagType.InsulationSurface);
                            conductorins_bdrys[cond_idx] = insulation_bdry;
                        }
                        else
                        {
                            conductorins_bdrys[cond_idx] = conductor_bdry;
                        }

                        var conductor_surface = _geometry.AddSurface(conductor_bdry);
                        _tagManager.TagEntityByLocation(conductor_surface, loc, TagType.ConductorSurface);
                        cond_idx++;
                    }
                }
                else if (seg.ParallelOrientation == Orientation.Axial)
                {
                    for (int strand = 0; strand < seg.NumParallelConductors; strand++)
                    {
                        var loc = new LocationKey(seg.ParentWinding.Id, seg.ParentSegment.Id, turn, strand);

                        // For axial orientation, we need to calculate the z-coordinate based on the turn number
                        // and the number of parallel conductors.
                        if (cond_idx > 0)
                        {
                            z_mid += wdg_direction * (seg.ConductorType.TotalHeight_mm + gap);
                        }

                        // For axial orientation, we need to calculate the radial position based on the turn number
                        // and the number of parallel conductors.
                        double r_mid = seg.InnerRadius_mm + seg.ConductorType.TotalWidth_mm / 2;
                        (conductor_bdry, insulation_bdry) = seg.ConductorType.CreateGeometry(ref _geometry, r_mid, z_mid - seg.ParentTransformer.Core.WindowHeight_mm / 2);
                        _tagManager.TagEntityByLocation(conductor_bdry, loc, TagType.ConductorSurface);
                        if (insulation_bdry != null)
                        {
                            var insulation_surface = _geometry.AddSurface(insulation_bdry, conductor_bdry);
                            _tagManager.TagEntityByLocation(insulation_surface, loc, TagType.InsulationSurface);
                            conductorins_bdrys[cond_idx] = insulation_bdry;
                        }
                        else
                        {
                            conductorins_bdrys[cond_idx] = insulation_bdry;
                        }

                        var conductor_surface = _geometry.AddSurface(conductor_bdry);
                        _tagManager.TagEntityByLocation(conductor_surface, loc, TagType.ConductorSurface);
                        cond_idx++;

                    }
                }
            }

            return conductorins_bdrys;
        }

        public GeomLineLoop[] CreateGeometry(LayerSegmentGeometry seg)
        {
            var wdg_rect = _geometry.AddRectangle(seg.InnerRadius_mm + seg.SegmentRadialBuild_mm / 2, seg.SegmentHeight_mm / 2 + seg.DistanceAboveBottomYoke_mm - seg.ParentTransformer.Core.WindowHeight_mm / 2, seg.SegmentHeight_mm, seg.SegmentRadialBuild_mm);
            var rtn_loops = new GeomLineLoop[1];
            rtn_loops[0] = wdg_rect;
            return rtn_loops;
        }

        public GeomLineLoop CreateGeometry(ICore iCore) => iCore switch
        {
            Core core => CreateGeometry(core),
            CoreFacsimile core_facsimile => CreateGeometry(core_facsimile),
            null => throw new ArgumentNullException(nameof(iCore)),
            _ => throw new NotSupportedException(iCore.GetType().Name)
        };

        public GeomLineLoop CreateGeometry(Core core)
        {
            var LL = _geometry.AddPoint(core.CoreLegRadius_mm / 1000, -core.WindowHeight_mm / 1000 / 2, 0.01);
            var UL = _geometry.AddPoint(core.CoreLegRadius_mm / 1000, core.WindowHeight_mm / 1000 / 2, 0.01);
            var UR = _geometry.AddPoint((core.CoreLegRadius_mm + core.WindowWidth_mm) / 1000, core.WindowHeight_mm / 1000 / 2, 0.01);
            var LR = _geometry.AddPoint((core.CoreLegRadius_mm + core.WindowWidth_mm) / 1000, -core.WindowHeight_mm / 1000 / 2, 0.01);
            var core_leg = _geometry.AddLine(LL, UL);
            _tagManager.TagEntityByString(core_leg, "CoreLeg");
            var top_yoke = _geometry.AddLine(UL, UR);
            _tagManager.TagEntityByString(top_yoke, "TopYoke");
            var right_edge = _geometry.AddLine(UR, LR);
            _tagManager.TagEntityByString(right_edge, "RightEdge");
            var bottom_yoke = _geometry.AddLine(LR, LL);
            _tagManager.TagEntityByString(bottom_yoke, "BottomYoke");
            var outer_bdry = _geometry.AddLineLoop(new[] { core_leg, top_yoke, right_edge, bottom_yoke });
            return outer_bdry;
        }

        public GeomLineLoop CreateGeometry(CoreFacsimile core)
        {
            var window_LL = _geometry.AddPoint(core.OuterRadius_mm / 1000.0, -core.WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var window_UL = _geometry.AddPoint(core.OuterRadius_mm / 1000.0, core.WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var window_UR = _geometry.AddPoint((core.OuterRadius_mm + core.WindowWidth_mm) / 1000.0, core.WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var window_LR = _geometry.AddPoint((core.OuterRadius_mm + core.WindowWidth_mm) / 1000.0, -core.WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var core_LL = _geometry.AddPoint((core.OuterRadius_mm - core.Thickness_mm) / 1000.0, -core.WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var core_UL = _geometry.AddPoint((core.OuterRadius_mm - core.Thickness_mm) / 1000.0, core.WindowHeight_mm / 1000.0 / 2.0, 0.01);
            var core_UR = window_UL;
            var core_LR = window_LL;
            // var axis_lower = _geometry.AddPoint(0.0, -core.WindowHeight_mm / 1000.0 / 2.0, 0.01);
            // var axis_upper = _geometry.AddPoint(0.0, core.WindowHeight_mm / 1000.0 / 2.0, 0.01);
            // var axis = _geometry.AddLine(axis_lower, axis_upper);
            // _tagManager.TagEntityByString(axis, "Axis");
            // var air_top = _geometry.AddLine(axis_upper, core_UL);
            // var air_bottom = _geometry.AddLine(core_LL, axis_lower);
            var core_outer = _geometry.AddLine(core_LR, core_UR);
            _tagManager.TagEntityByString(core_outer, "CoreLeg_Outer");
            var core_top = _geometry.AddLine(core_UL, core_UR);
            _tagManager.TagEntityByString(core_top, "CoreLeg_Top");
            var core_inner = _geometry.AddLine(core_LL, core_UL);
            _tagManager.TagEntityByString(core_inner, "CoreLeg_Inner");
            var core_bottom = _geometry.AddLine(core_LR, core_LL);
            _tagManager.TagEntityByString(core_top, "CoreLeg_Bottom");
            var top_yoke = _geometry.AddLine(window_UL, window_UR);
            _tagManager.TagEntityByString(top_yoke, "TopYoke");
            var right_edge = _geometry.AddLine(window_UR, window_LR);
            _tagManager.TagEntityByString(right_edge, "RightEdge");
            var bottom_yoke = _geometry.AddLine(window_LR, window_LL);
            _tagManager.TagEntityByString(bottom_yoke, "BottomYoke");
            var window_bdry = _geometry.AddLineLoop(new[] { core_outer, top_yoke, right_edge, bottom_yoke });
            var core_bdry = _geometry.AddLineLoop(new[] { core_outer, core_bottom, core_inner, core_top });
            _tagManager.TagEntityByString(core_bdry, "CoreFacsimileBdry");
            //var air_bdry = _geometry.AddLineLoop(new[] { axis, air_top, core_inner, air_bottom });
            // var air_surface = _geometry.AddSurface(air_bdry, core_bdry);
            // _tagManager.TagEntityByString(air_surface, "AirCore");
            var core_surface = _geometry.AddSurface(core_bdry);
            _tagManager.TagEntityByString(core_surface, "CoreFacsimile");
            var outer_bdry = _geometry.AddLineLoop(new[] { core_top, top_yoke, right_edge, bottom_yoke, core_bottom, core_inner });
            return window_bdry;
        }

        public (GeomLineLoop, GeomLineLoop) CreateGeometry(RectConductor cond, double r_mid, double z_mid)
        {
            var conductor_bdry = _geometry.AddRoundedRectangle(
                r_mid / 1000,
                z_mid / 1000,
                cond.BareHeight_mm / 1000,
                cond.BareWidth_mm / 1000,
                cond.CornerRadius_mm / 1000,
                0.0004
            );
            var insulation_bdry = _geometry.AddRoundedRectangle(
                r_mid / 1000,
                z_mid / 1000,
                (cond.BareHeight_mm + 2 * cond.InsulationThickness_mm) / 1000,
                (cond.BareWidth_mm + 2 * cond.InsulationThickness_mm) / 1000,
                (cond.CornerRadius_mm + cond.InsulationThickness_mm) / 1000,
                0.0004
            );
            return (conductor_bdry, insulation_bdry);
        }
    }
}