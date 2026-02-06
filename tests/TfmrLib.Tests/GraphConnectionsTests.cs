using System.Collections.Generic;
using TfmrLib;
using Xunit;

namespace TfmrLib.Tests
{
    public class GraphConnectionsTests
    {
        [Fact]
        public void SeriesConnection_ShouldConnectSegments_ByLabel()
        {
            var transformer = new Transformer();
            var winding = new Winding { Label = "HV" };
            transformer.Windings.Add(winding);
            
            var s1 = new WindingSegment { Label = "Top" };
            var s2 = new WindingSegment { Label = "Bottom" };
            
            winding.Segments.Add(s1);
            winding.Segments.Add(s2);
            
            winding.InternalConnections.Add(new SeriesConnection 
            { 
                SegmentLabels = new List<string> { "Top", "Bottom" } 
            });

            // Initialize entire transformer to trigger logic
            transformer.Initialize();

            // Assert: End of s1 should be same node as Start of s2
            Assert.Equal(s1.EndNode, s2.StartNode);
            // Assert: Start of s1 should NOT be connected to End of s2 (no ring)
            Assert.NotEqual(s1.StartNode, s2.EndNode);
        }

        [Fact]
        public void SeriesConnection_Default_ShouldConnectAllInOrder()
        {
            var transformer = new Transformer();
            var winding = new Winding { Label = "HV" };
            transformer.Windings.Add(winding);
            
            var s1 = new WindingSegment { Label = "S1" };
            var s2 = new WindingSegment { Label = "S2" };
            var s3 = new WindingSegment { Label = "S3" };
            
            winding.Segments.Add(s1);
            winding.Segments.Add(s2);
            winding.Segments.Add(s3);
            
            // Empty SeriesConnection implies all segments
            winding.InternalConnections.Add(new SeriesConnection());

            transformer.Initialize();

            Assert.Equal(s1.EndNode, s2.StartNode);
            Assert.Equal(s2.EndNode, s3.StartNode);
        }

        [Fact]
        public void ParallelConnection_ShouldConnectSegments_ByLabel()
        {
            var transformer = new Transformer();
            var winding = new Winding { Label = "LV" };
            transformer.Windings.Add(winding);
            
            var s1 = new WindingSegment { Label = "Left" };
            var s2 = new WindingSegment { Label = "Right" };
            
            winding.Segments.Add(s1);
            winding.Segments.Add(s2);

            winding.InternalConnections.Add(new ParallelConnection 
            { 
                SegmentLabels = new List<string> { "Left", "Right" } 
            });

            transformer.Initialize();

            Assert.Equal(s1.StartNode, s2.StartNode);
            Assert.Equal(s1.EndNode, s2.EndNode);
        }
    }
}
