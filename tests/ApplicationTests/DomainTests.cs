using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using System.ComponentModel;
using System.Reflection;
using Xunit;

namespace ApplicationTests;

public class DomainTests
{
    [Fact]
    public void Trade_Record_Preserves_Values_And_Properties()
    {
        var trade = new Trade(123.45m, "Public", "CLI001");

        trade.Value.Should().Be(123.45m);
        trade.ClientSector.Should().Be("Public");
        trade.ClientId.Should().Be("CLI001");
    }

    [Fact]
    public void Trade_Record_ValueEquality_Works()
    {
        var a = new Trade(100m, "Public", "A");
        var b = new Trade(100m, "Public", "A");

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void RiskCategory_Enum_Has_Description_Attributes()
    {
        foreach (var value in Enum.GetValues<RiskCategory>())
        {
            var member = typeof(RiskCategory).GetMember(value.ToString()).FirstOrDefault();
            var attr = member?.GetCustomAttribute<DescriptionAttribute>();
            attr.Should().NotBeNull($"enum value {value} should have a DescriptionAttribute");
            attr!.Description.Should().NotBeNullOrWhiteSpace();
        }
    }
}