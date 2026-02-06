using System.Collections.Generic;
using TfmrLib;
using Xunit;
using GeometryLib;

namespace TfmrLib.Tests
{
    public class TapWindingTests
    {
        [Fact]
        public void WindingSegment_WithTaps_ShouldCreateMultipleBranches()
        {
            var transformer = new Transformer();
            var winding = new Winding { Label = "TappedWinding" };
            transformer.Windings.Add(winding);

            var segment = new WindingSegment 
            { 
                Label = "TappedSegment",
                Geometry = new DiscWindingGeometry { NumTurns = 100 } // Need geometry for turns
            };
            winding.Segments.Add(segment);

            // Add taps
            segment.Taps.Add(new TapDefinition { Label = "T1", TurnNumber = 25 });
            segment.Taps.Add(new TapDefinition { Label = "T2", TurnNumber = 75 });
            // Out of order to test sorting
            segment.Taps.Add(new TapDefinition { Label = "T_Mid", TurnNumber = 50 });

            transformer.Initialize();

            // Assertions
            Assert.Equal(4, segment.Branches.Count); // 0-25, 25-50, 50-75, 75-100

            // Check connectivity
            // Branch 0 end should be Branch 1 start
            Assert.Equal(segment.Branches[0].EndNode, segment.Branches[1].StartNode);
            Assert.Equal(segment.Branches[1].EndNode, segment.Branches[2].StartNode);

            // Check turns
            Assert.Equal(25, segment.Branches[0].NumTurns);
            Assert.Equal(0, segment.Branches[0].StartTurnIndex);

            Assert.Equal(25, segment.Branches[1].NumTurns); // 25 to 50
            Assert.Equal(25, segment.Branches[1].StartTurnIndex);
            
            Assert.Equal(25, segment.Branches[2].NumTurns); // 50 to 75
            
            Assert.Equal(25, segment.Branches[3].NumTurns); // 75 to 100
        }

        [Fact]
        public void Terminal_ShouldConnectToTap()
        {
            var transformer = new Transformer();
            var winding = new Winding { Label = "TapWdg" };
            transformer.Windings.Add(winding);

            var segment = new WindingSegment 
            { 
                Label = "Main",
                Geometry = new DiscWindingGeometry { NumTurns = 100 }
            };
            segment.Taps.Add(new TapDefinition { Label = "TestTap", TurnNumber = 40 });
            winding.Segments.Add(segment);

            var terminal = new Terminal 
            { 
                Label = "H2", 
                ConnectToSegmentLabel = "Main", 
                ConnectToTapLabel = "TestTap" 
            };
            winding.Terminals.Add(terminal);

            transformer.Initialize();

            // The terminal should be connected to the node between branch 0 (0-40) and branch 1 (40-100)
            Assert.NotNull(terminal.InternalNode);
            Assert.Equal(segment.Branches[0].EndNode, terminal.InternalNode);
            Assert.Equal(segment.Branches[1].StartNode, terminal.InternalNode);
        }
    }
}
