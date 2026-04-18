using FluentAssertions;
using IcdFyIt.Core.Model;
using Xunit;

namespace IcdFyIt.Core.Tests.Model;

public class DataModelTests
{
    [Fact(Skip = "Not yet implemented")]
    public void NewDataModel_HasEmptyCollections()
    {
        var model = new DataModel();

        model.DataTypes.Should().BeEmpty();
        model.Parameters.Should().BeEmpty();
        model.PacketTypes.Should().BeEmpty();
    }

    [Fact(Skip = "Not yet implemented")]
    public void DataType_AssignedGuid_OnCreation()
    {
        var dt = new SignedIntegerType();

        dt.Id.Should().NotBe(Guid.Empty);
    }

    [Fact(Skip = "Not yet implemented")]
    public void Parameter_AssignedGuid_OnCreation()
    {
        var p = new Parameter();

        p.Id.Should().NotBe(Guid.Empty);
    }

    [Fact(Skip = "Not yet implemented")]
    public void PacketType_AssignedGuid_OnCreation()
    {
        var pt = new PacketType();

        pt.Id.Should().NotBe(Guid.Empty);
    }
}
