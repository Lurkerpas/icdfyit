using FluentAssertions;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Persistence;
using Xunit;

namespace IcdFyIt.Core.Tests.Persistence;

public class XmlPersistenceTests
{
    private readonly XmlPersistence _sut = new();

    [Fact(Skip = "Not yet implemented")]
    public void SaveThenLoad_ProducesEquivalentModel()
    {
        var model = new DataModel();
        model.DataTypes.Add(new DataType { Name = "MyType", BaseType = BaseType.SignedInteger });
        var path = Path.Combine(Path.GetTempPath(), $"icdfyit_test_{Guid.NewGuid():N}.xml");

        try
        {
            _sut.Save(model, path);
            var loaded = _sut.Load(path);

            loaded.DataTypes.Should().HaveCount(1);
            loaded.DataTypes[0].Name.Should().Be("MyType");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact(Skip = "Not yet implemented")]
    public void Load_NewerSchemaVersion_Throws()
    {
        // Arrange: write a file with a schema version higher than CurrentSchemaVersion
        var path = Path.Combine(Path.GetTempPath(), $"icdfyit_test_{Guid.NewGuid():N}.xml");
        var xml = $"<DataModel SchemaVersion=\"{XmlPersistence.CurrentSchemaVersion + 1}\" />";
        File.WriteAllText(path, xml);

        try
        {
            _sut.Invoking(s => s.Load(path))
                .Should().Throw<NotSupportedException>();
        }
        finally
        {
            File.Delete(path);
        }
    }
}
