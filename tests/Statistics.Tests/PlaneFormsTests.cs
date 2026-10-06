using PropStruct.Particle;
using Xunit;

namespace PropStruct.Statistics.Tests;

/// <summary>
/// The forms the per-cycle plane's products take and the schedule of its unrolled sums, against
/// <c>cases/statistics/plane_forms.json</c>: every Original form of <see cref="CyclePlaneOrder"/> is
/// compared, bit for bit, with the value the script read off the <c>order</c> text of a site of
/// <c>CyclePlane.listing.generated.txt</c> that uses it, and every pass decision of
/// <see cref="UnrolledSchedule"/> with the script's rule, at trip counts below, at and above its block
/// size. The Binary64 forms are held by the bit snapshots, which no case of this file loosens.
/// </summary>
public class PlaneFormsTests
{
    public static IEnumerable<object[]> FormCases() =>
        FormulaCaseFiles.ReadPlaneFormsCases().Where(c => c.Form != "schedule").Select(c => new object[] { c.Name });

    public static IEnumerable<object[]> ScheduleCases() =>
        FormulaCaseFiles.ReadPlaneFormsCases().Where(c => c.Form == "schedule").Select(c => new object[] { c.Name });

    [Theory]
    [MemberData(nameof(FormCases))]
    public void OriginalFormMatchesTheTableText(string caseName)
    {
        var c = FormulaCaseFiles.FindPlaneFormsCase(caseName);
        var order = new CyclePlaneOrder(PrecisionKind.Original);
        for (var draw = 0; draw < c.Operands.Count; draw++)
        {
            var x = c.Operands[draw][0];
            var di = c.Operands[draw][1];
            var index = c.Operands[draw][2];
            var m = c.Operands[draw][3];
            var centre = (index - 0.5) * di;
            var actual = c.Form switch
            {
                "linear" => order.Linear(x, di, index - 0.5),
                "quadratic" => order.Quadratic(x, di * (index - 0.5)),
                "fourth" => order.Fourth(centre),
                "cube" => order.Cube(centre),
                "square" => order.Square(order.Fourth(centre) - m),
                _ => throw new InvalidOperationException($"unknown form {c.Form}"),
            };
            Assert.Equal(BitConverter.DoubleToInt64Bits(c.ExpectedOriginal[draw]), BitConverter.DoubleToInt64Bits(actual));
        }
    }

    [Theory]
    [MemberData(nameof(ScheduleCases))]
    public void ScheduleMatchesTheRule(string caseName)
    {
        var c = FormulaCaseFiles.FindPlaneFormsCase(caseName);
        for (var trips = 1; trips <= c.RoundsAfter.Count; trips++)
        {
            var schedule = new UnrolledSchedule(trips, c.Unroll);
            for (var pass = 1; pass <= trips; pass++)
            {
                Assert.Equal(c.RoundsAfter[trips - 1][pass - 1], schedule.RoundsAfter(pass));
                Assert.Equal(c.ReloadsBefore[trips - 1][pass - 1], schedule.ReloadsBefore(pass));
            }
        }
    }
}
