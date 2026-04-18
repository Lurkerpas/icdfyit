namespace IcdFyIt.Core.Model;

/// <summary> Size descriptor for an Array Data Type (ICD-DAT-91). </summary>
public class ArraySizeDescriptor
{
    public Endianness Endianness { get; set; }
    public int BitSize { get; set; }
    public NumericRange Range { get; set; } = new();
}
