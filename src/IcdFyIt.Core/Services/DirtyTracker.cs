namespace IcdFyIt.Core.Services;

/// <summary>
/// Tracks whether the in-memory model has unsaved changes (ICD-IF-180).
/// </summary>
public class DirtyTracker
{
    /// <summary>True when the model has changes not yet written to disk.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>Marks the model as having unsaved changes.</summary>
    public void MarkDirty() => IsDirty = true;

    /// <summary>Clears the dirty flag (called after a successful save or after New/Open).</summary>
    public void MarkClean() => IsDirty = false;
}
