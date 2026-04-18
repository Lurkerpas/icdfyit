namespace IcdFyIt.Core.Model;

/// <summary> One field in a Structure Data Type. </summary>
public class StructureField
{
    public string Name { get; set; } = string.Empty;

    /// <summary> Nullable: referent may be deleted (ICD-FUN-51). </summary>
    public DataType? DataType { get; set; }
}
