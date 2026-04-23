using FluentAssertions;
using IcdFyIt.Core.Model;
using Xunit;

namespace IcdFyIt.Core.Tests.Model;

public class DataModelTests
{
    [Fact]
    public void NewDataModel_HasEmptyCollections()
    {
        var model = new DataModel();

        model.DataTypes.Should().BeEmpty();
        model.Parameters.Should().BeEmpty();
        model.PacketTypes.Should().BeEmpty();
    }

    [Fact]
    public void DataType_AssignedGuid_OnCreation()
    {
        var dt = new SignedIntegerType();

        dt.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Parameter_AssignedGuid_OnCreation()
    {
        var p = new Parameter();

        p.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void PacketType_AssignedGuid_OnCreation()
    {
        var pt = new PacketType();

        pt.Id.Should().NotBe(Guid.Empty);
    }
}
