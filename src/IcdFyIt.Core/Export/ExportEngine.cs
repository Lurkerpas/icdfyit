using IcdFyIt.Core.Model;

namespace IcdFyIt.Core.Export;

/// <summary>
/// Drives code generation by invoking Mako templates via Python.NET (ICD-FUN-70 to ICD-FUN-72).
/// Python.NET (pythonnet) is initialised lazily on first use (ICD-DES §4.4).
/// </summary>
public class ExportEngine
{
#pragma warning disable CS0169
    private bool _pythonInitialised;
#pragma warning restore CS0169

    /// <summary>
    /// Renders all templates in <paramref name="templateSet"/> against <paramref name="model"/>
    /// and writes output files to <paramref name="outputFolder"/>.
    /// </summary>
    public void Export(DataModel model, TemplateSet templateSet, string outputFolder) =>
        throw new NotImplementedException();

    /// <summary>Initialises the Python.NET runtime if not already done.</summary>
    private void EnsurePythonInitialised() => throw new NotImplementedException();
}
