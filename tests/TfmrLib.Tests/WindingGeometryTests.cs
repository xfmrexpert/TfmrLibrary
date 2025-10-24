namespace TfmrLib.Tests;

public class WindingGeometryTests
{
    [Fact]
    public void ConductorDCResTest()
    {
        var cdr = new RectConductor();
        cdr.StrandHeight_mm = Conversions.in_to_mm(0.3);
        cdr.StrandWidth_mm = Conversions.in_to_mm(0.085);
        cdr.CornerRadius_mm = Conversions.in_to_mm(0.032);
        cdr.InsulationThickness_mm = Conversions.in_to_mm(0.018);
        Console.WriteLine($"Conductor Resistance: {cdr.DCResistance}");
    }

}
