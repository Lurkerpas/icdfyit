using FluentAssertions;
using IcdFyIt.Core.Model;
using IcdFyIt.Core.Services;
using Xunit;

namespace IcdFyIt.Core.Tests.Services;

public class ModelValidatorTests
{
    private readonly ModelValidator _sut = new();

    [Fact(Skip = "Not yet implemented")]
    public void Validate_EmptyModel_ReturnsNoIssues()
    {
        var model = new DataModel();

        var issues = _sut.Validate(model);

        issues.Should().BeEmpty();
    }

    [Fact(Skip = "Not yet implemented")]
    public void Validate_DuplicateDataTypeName_ReturnsIssue()
    {
        var model = new DataModel();
        model.DataTypes.Add(new SignedIntegerType { Name = "Foo" });
        model.DataTypes.Add(new SignedIntegerType { Name = "Foo" });

        var issues = _sut.Validate(model);

        issues.Should().ContainSingle();
    }

    [Fact(Skip = "Not yet implemented")]
    public void Validate_DuplicateParameterName_ReturnsIssue()
    {
        var model = new DataModel();
        model.Parameters.Add(new Parameter { Name = "Bar" });
        model.Parameters.Add(new Parameter { Name = "Bar" });

        var issues = _sut.Validate(model);

        issues.Should().ContainSingle();
    }
}
