namespace IcdFyIt.Core.Model;

/// <summary> Endianness and bit size common to all scalar Data Types (ICD-DAT-101, ICD-DAT-110, ICD-DAT-120). </summary>
public class ScalarProperties
{
    public Endianness Endianness { get; set; }
    public int BitSize { get; set; }
}
